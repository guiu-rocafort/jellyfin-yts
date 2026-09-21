namespace Jellyfin.Plugin.YtsTorrents.QBittorrent;

public record TorrentInfo(string Hash, string State, double Progress, string ContentPath, string SavePath)
{
    private static readonly string[] CompleteStates =
    {
        "stalledUP", "pausedUP", "stoppedUP", "queuedUP", "uploading", "checkingUP", "forcedUP",
    };

    private static readonly string[] ErrorStates =
    {
        "error", "missingFiles", "unknown",
    };

    public bool IsComplete => System.Array.IndexOf(CompleteStates, State) >= 0;

    public bool IsError => System.Array.IndexOf(ErrorStates, State) >= 0;
}
