using System.Collections.Generic;

namespace Jellyfin.Plugin.YtsTorrents.Api.Dto;

public record TorrentDto(string Hash, string Quality, string Type, string Size, int Seeds, int Peers);

public record SearchResultDto(long MovieId, string Title, int Year, double Rating, IReadOnlyList<TorrentDto> Torrents);
