using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.YtsTorrents.Configuration;
using Jellyfin.Plugin.YtsTorrents.QBittorrent;
using Jellyfin.Plugin.YtsTorrents.Yts;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YtsTorrents.Downloads;

/// <summary>
/// Orchestrates the search-to-library pipeline: hands a chosen torrent to qBittorrent, then on each
/// scheduled poll checks progress and imports finished downloads into the configured library folder.
/// </summary>
public class DownloadCoordinator
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".m4v", ".ts",
    };

    private static readonly HashSet<string> SubtitleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".srt", ".sub", ".idx", ".ass", ".ssa", ".vtt",
    };

    private readonly IDownloadClient _downloadClient;
    private readonly PendingDownloadStore _store;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<DownloadCoordinator> _logger;
    private readonly ConcurrentDictionary<string, int> _consecutiveFailures = new();

    public DownloadCoordinator(
        IDownloadClient downloadClient,
        PendingDownloadStore store,
        ILibraryManager libraryManager,
        ILogger<DownloadCoordinator> logger)
    {
        _downloadClient = downloadClient;
        _store = store;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public async Task StartDownloadAsync(YtsMovie movie, YtsTorrent torrent, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance!.Configuration;
        var hash = torrent.Hash.ToLowerInvariant();
        var magnet = MagnetBuilder.Build(hash, $"{movie.Title} ({movie.Year})");

        await _downloadClient.EnsureCategoryAsync(config.QbCategory, config.QbStagingSavePath, cancellationToken).ConfigureAwait(false);
        await _downloadClient.AddMagnetAsync(magnet, config.QbCategory, config.QbStagingSavePath, cancellationToken).ConfigureAwait(false);

        _store.Add(new PendingDownload
        {
            Hash = hash,
            MovieTitle = movie.Title,
            Year = movie.Year,
            AddedUtc = DateTime.UtcNow,
            State = PendingState.Downloading,
        });
    }

    /// <summary>Every tracked download, in-progress and finished alike, most recent first.</summary>
    public IReadOnlyCollection<PendingDownload> ListDownloads() =>
        _store.All().OrderByDescending(p => p.AddedUtc).ToList();

    public bool IsDownloaded(string hash) =>
        _store.All().Any(p => p.State == PendingState.Completed && string.Equals(p.Hash, hash, StringComparison.OrdinalIgnoreCase));

    public async Task PollAndImportAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance!.Configuration;

        // Completed items are polled too (not just Downloading) so the UI can show their live
        // seeding status (uploading, stalled, etc.) rather than freezing at whatever qBittorrent
        // reported the moment the download finished. Importing items are included because Jellyfin
        // never runs this task concurrently with itself, so any item still Importing when a poll
        // starts was interrupted mid-import (e.g. by a shutdown) and would otherwise be stuck forever.
        var pendingItems = _store.All()
            .Where(p => p.State is PendingState.Downloading or PendingState.Importing or PendingState.Completed)
            .ToList();
        var total = Math.Max(pendingItems.Count, 1);
        var processed = 0;

        if (pendingItems.Count > 0)
        {
            _logger.LogInformation("Polling {Count} tracked download(s).", pendingItems.Count);
        }

        foreach (var pending in pendingItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var info = await _downloadClient.GetByHashAsync(pending.Hash, cancellationToken).ConfigureAwait(false);
                if (info is null)
                {
                    if (pending.State != PendingState.Completed)
                    {
                        // Could be a brief registration delay right after StartDownloadAsync, or the
                        // torrent was removed from qBittorrent outside the plugin -- warn and give up
                        // after repeated misses rather than sitting in "Downloading" forever with no
                        // trace of why.
                        var misses = _consecutiveFailures.AddOrUpdate(pending.Hash, 1, (_, count) => count + 1);
                        _logger.LogWarning(
                            "qBittorrent has no record of torrent {Hash} for '{Title}' ({Year}) (attempt {Count})",
                            pending.Hash, pending.MovieTitle, pending.Year, misses);
                        if (misses >= 3)
                        {
                            Fail(pending, "qBittorrent no longer has a record of this torrent.");
                        }
                    }

                    // For a Completed item, this is the expected outcome once DeleteFromQbAfterImport
                    // has removed it from qBittorrent -- leave its last known status as-is, silently.
                    continue;
                }

                _consecutiveFailures.TryRemove(pending.Hash, out _);
                pending.Progress = info.Progress;
                pending.QbState = info.State;

                if (pending.State == PendingState.Completed)
                {
                    // Just refreshed its seeding status above, nothing else to do.
                    continue;
                }

                if (pending.State == PendingState.Importing)
                {
                    _logger.LogWarning(
                        "Resuming interrupted import of '{Title}' ({Year})", pending.MovieTitle, pending.Year);
                }

                if (info.IsComplete)
                {
                    await ImportAsync(pending, info, config, cancellationToken).ConfigureAwait(false);
                }
                else if (info.IsError)
                {
                    Fail(pending, $"qBittorrent reported state '{info.State}'.");
                }
            }
            catch (QBittorrentException ex) when (pending.State == PendingState.Completed)
            {
                // Only the seeding-status refresh failed -- the movie is already in the library, so
                // this must not flip it to Failed. Keep the last known status and try again next poll.
                _logger.LogWarning(ex, "qBittorrent status refresh failed for imported torrent {Hash}", pending.Hash);
            }
            catch (QBittorrentException ex)
            {
                var failures = _consecutiveFailures.AddOrUpdate(pending.Hash, 1, (_, count) => count + 1);
                _logger.LogWarning(ex, "qBittorrent check failed for {Hash} (attempt {Count})", pending.Hash, failures);
                if (failures >= 3)
                {
                    Fail(pending, ex.Message);
                }
            }
            finally
            {
                processed++;
                progress.Report(processed * 100d / total);
            }
        }

        // One batch save covers progress/state updates for items that didn't hit ImportAsync/Fail
        // this tick (those already saved themselves) -- cheaper than saving per item above.
        if (pendingItems.Count > 0)
        {
            _store.Save();
        }
    }

    private async Task ImportAsync(PendingDownload pending, TorrentInfo info, PluginConfiguration config, CancellationToken cancellationToken)
    {
        pending.State = PendingState.Importing;
        _store.Save();

        try
        {
            var target = MovieFolderNamer.BuildTargetDir(config.LibraryFolderPath, pending.MovieTitle, pending.Year);
            ImportFiles(info.ContentPath, target, config.ImportMode);
            _libraryManager.QueueLibraryScan();

            if (config.DeleteFromQbAfterImport)
            {
                var deleteFiles = config.ImportMode == ImportMode.Move;
                await _downloadClient.DeleteAsync(pending.Hash, deleteFiles, cancellationToken).ConfigureAwait(false);
            }

            pending.State = PendingState.Completed;
            _store.Save();
        }
        catch (Exception ex)
        {
            Fail(pending, ex.Message);
        }
    }

    private void Fail(PendingDownload pending, string message)
    {
        pending.State = PendingState.Failed;
        pending.LastError = message;
        _store.Save();
        _logger.LogError("Import failed for {Title} ({Year}): {Message}", pending.MovieTitle, pending.Year, message);
    }

    private void ImportFiles(string contentPath, string targetDir, ImportMode mode)
    {
        Directory.CreateDirectory(targetDir);

        var files = Directory.Exists(contentPath)
            ? Directory.GetFiles(contentPath, "*", SearchOption.AllDirectories)
            : new[] { contentPath };

        var videoFiles = files.Where(f => VideoExtensions.Contains(Path.GetExtension(f))).ToList();
        if (videoFiles.Count == 0)
        {
            throw new InvalidOperationException($"No video files found under '{contentPath}'.");
        }

        // Subtitles (e.g. YTS releases commonly ship a Subs/ folder) are flattened into the same
        // target directory as the video by original filename, same as video files -- previously
        // these were silently dropped since only VideoExtensions was ever selected here.
        var subtitleFiles = files.Where(f => SubtitleExtensions.Contains(Path.GetExtension(f))).ToList();

        foreach (var src in videoFiles.Concat(subtitleFiles))
        {
            FileImporter.Place(src, Path.Combine(targetDir, Path.GetFileName(src)), mode, _logger);
        }

        _logger.LogInformation(
            "Imported {VideoCount} video and {SubtitleCount} subtitle file(s) from '{ContentPath}' to '{TargetDir}'",
            videoFiles.Count, subtitleFiles.Count, contentPath, targetDir);
    }
}
