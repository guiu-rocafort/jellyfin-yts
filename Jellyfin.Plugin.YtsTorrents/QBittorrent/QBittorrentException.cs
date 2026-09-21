using System;

namespace Jellyfin.Plugin.YtsTorrents.QBittorrent;

public class QBittorrentException : Exception
{
    public QBittorrentException(string message)
        : base(message)
    {
    }

    public QBittorrentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
