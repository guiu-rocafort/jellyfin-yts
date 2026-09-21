using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YtsTorrents.Downloads;

/// <summary>
/// Persists in-flight downloads to a small JSON file under the plugin's data directory so they survive a
/// Jellyfin restart. On restart, entries just resume being polled -- qBittorrent already has the torrent,
/// no special recovery logic is needed.
/// </summary>
public class PendingDownloadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ILogger<PendingDownloadStore> _logger;
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, PendingDownload> _items = new();
    private readonly object _fileLock = new();

    public PendingDownloadStore(IApplicationPaths applicationPaths, ILogger<PendingDownloadStore> logger)
    {
        _logger = logger;
        var dataDir = Path.Combine(applicationPaths.PluginsPath, "YtsTorrents-data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "pending-downloads.json");
        Load();
    }

    public IReadOnlyCollection<PendingDownload> All() => _items.Values.ToList();

    public void Add(PendingDownload download)
    {
        _items[download.Hash] = download;
        Save();
    }

    public void Remove(string hash)
    {
        _items.TryRemove(hash, out _);
        Save();
    }

    public void Save()
    {
        lock (_fileLock)
        {
            var json = JsonSerializer.Serialize(_items.Values.ToList(), JsonOptions);
            File.WriteAllText(_filePath, json);
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        lock (_fileLock)
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<List<PendingDownload>>(json, JsonOptions) ?? new List<PendingDownload>();
                foreach (var item in list)
                {
                    _items[item.Hash] = item;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse {File}; starting with no pending downloads.", _filePath);
            }
        }
    }
}
