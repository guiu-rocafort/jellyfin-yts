namespace Jellyfin.Plugin.YtsTorrents.Api.Dto;

public class DownloadRequestDto
{
    public long MovieId { get; set; }

    public string Hash { get; set; } = string.Empty;
}
