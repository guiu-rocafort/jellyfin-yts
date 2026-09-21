using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.YtsTorrents.Configuration;

public enum ImportMode
{
    Hardlink,
    Move,
    Copy,
}

public class PluginConfiguration : BasePluginConfiguration
{
    public string YtsBaseUrl { get; set; } = "https://yts.gg/api/v2";

    public string QbUrl { get; set; } = "http://qbittorrent:8080";

    public string QbUsername { get; set; } = string.Empty;

    public string QbPassword { get; set; } = string.Empty;

    public string QbCategory { get; set; } = "jellyfin-imports";

    public string QbStagingSavePath { get; set; } = string.Empty;

    public string LibraryFolderPath { get; set; } = string.Empty;

    public ImportMode ImportMode { get; set; } = ImportMode.Hardlink;

    public bool DeleteFromQbAfterImport { get; set; }
}
