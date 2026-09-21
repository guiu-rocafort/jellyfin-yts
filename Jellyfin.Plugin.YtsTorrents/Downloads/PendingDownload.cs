using System;

namespace Jellyfin.Plugin.YtsTorrents.Downloads;

public enum PendingState
{
    Downloading,
    Importing,
    Failed,
}

public class PendingDownload
{
    public string Hash { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;

    public int Year { get; set; }

    public DateTime AddedUtc { get; set; }

    public PendingState State { get; set; } = PendingState.Downloading;

    public string? LastError { get; set; }
}
