using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.YtsTorrents.Downloads;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.YtsTorrents.Tasks;

/// <summary>
/// Runs on a short interval (Jellyfin-native scheduled task, adjustable/"Run Now"-able from
/// Dashboard &gt; Scheduled Tasks) to check qBittorrent for finished downloads and import them.
/// A background hosted service was considered, but a scheduled task needs no extra lifecycle
/// management and the ~1 minute granularity is irrelevant since torrent downloads take minutes.
/// </summary>
public class PollDownloadsTask : IScheduledTask
{
    private readonly DownloadCoordinator _coordinator;

    public PollDownloadsTask(DownloadCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public string Name => "Poll YTS Torrent Downloads";

    public string Key => "YtsTorrentsPollDownloads";

    public string Description => "Checks qBittorrent for finished downloads and imports them into the library.";

    public string Category => "Library";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) =>
        _coordinator.PollAndImportAsync(progress, cancellationToken);

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromMinutes(1).Ticks,
            },
        };
    }
}
