namespace Jellyfin.Plugin.YtsTorrents.Api.Dto;

public class TestConnectionRequestDto
{
    public string QbUrl { get; set; } = string.Empty;

    public string QbUsername { get; set; } = string.Empty;

    public string QbPassword { get; set; } = string.Empty;
}
