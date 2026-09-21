using System;

namespace Jellyfin.Plugin.YtsTorrents.Downloads;

public enum PendingState
{
    Downloading,
    Importing,
    Failed,

    /// <summary>Imported successfully. Kept in the store (rather than removed) purely so the search
    /// page can tell the user a given torrent hash was already downloaded; excluded from the
    /// "in-progress" downloads list.</summary>
    Completed,
}

public class PendingDownload
{
    public string Hash { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;

    public int Year { get; set; }

    public DateTime AddedUtc { get; set; }

    public PendingState State { get; set; } = PendingState.Downloading;

    public string? LastError { get; set; }

    /// <summary>Fraction complete (0.0-1.0), as last reported by qBittorrent.</summary>
    public double Progress { get; set; }

    /// <summary>Raw qBittorrent torrent state (e.g. "downloading", "stalledDL", "metaDL"), as last polled.</summary>
    public string? QbState { get; set; }
}
