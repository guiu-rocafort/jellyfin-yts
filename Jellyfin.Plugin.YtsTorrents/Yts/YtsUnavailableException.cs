using System;

namespace Jellyfin.Plugin.YtsTorrents.Yts;

public class YtsUnavailableException : Exception
{
    public YtsUnavailableException(string message)
        : base(message)
    {
    }

    public YtsUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
