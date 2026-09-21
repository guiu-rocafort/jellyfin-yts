using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.YtsTorrents.Yts;

public static class MagnetBuilder
{
    public static readonly string[] Trackers =
    {
        "udp://tracker.opentrackr.org:1337/announce",
        "udp://tracker.torrent.eu.org:451/announce",
        "udp://tracker.dler.org:6969/announce",
        "udp://open.stealth.si:80/announce",
        "udp://open.demonii.com:1337/announce",
        "udp://open.dstud.io:6969/announce",
        "udp://tracker.srv00.com:6969/announce",
    };

    private static readonly Regex BtihPattern = new("^[a-fA-F0-9]{40}$", RegexOptions.Compiled);

    public static bool IsValidHash(string hash) => hash is not null && BtihPattern.IsMatch(hash);

    public static string Build(string btih, string displayName)
    {
        if (!IsValidHash(btih))
        {
            throw new ArgumentException("Not a valid 40-character BTIH hash.", nameof(btih));
        }

        var trackerParams = string.Concat(Trackers.Select(t => "&tr=" + Uri.EscapeDataString(t)));
        return "magnet:?xt=urn:btih:" + btih.Trim() + "&dn=" + Uri.EscapeDataString(displayName) + trackerParams;
    }
}
