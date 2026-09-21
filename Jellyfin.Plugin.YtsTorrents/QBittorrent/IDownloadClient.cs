using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YtsTorrents.QBittorrent;

public interface IDownloadClient
{
    /// <summary>
    /// Attempts a login against the given qBittorrent WebUI, independent of the plugin's saved
    /// configuration -- so the settings page can validate credentials before they're saved. Throws
    /// <see cref="QBittorrentException"/> with a user-facing message on failure.
    /// </summary>
    Task TestConnectionAsync(string url, string username, string password, CancellationToken cancellationToken);

    Task EnsureCategoryAsync(string category, string savePath, CancellationToken cancellationToken);

    Task AddMagnetAsync(string magnet, string category, string savePath, CancellationToken cancellationToken);

    Task<TorrentInfo?> GetByHashAsync(string hash, CancellationToken cancellationToken);

    Task DeleteAsync(string hash, bool deleteFiles, CancellationToken cancellationToken);
}
