using System.Collections.Generic;

namespace Jellyfin.Plugin.YtsTorrents.Yts;

public record YtsTorrent(
    string Url,
    string Hash,
    string Quality,
    string Type,
    string Size,
    long SizeBytes,
    int Seeds,
    int Peers);

public record YtsMovie(
    long Id,
    string ImdbCode,
    string Title,
    int Year,
    double Rating,
    IReadOnlyList<string> Genres,
    IReadOnlyList<YtsTorrent> Torrents);
