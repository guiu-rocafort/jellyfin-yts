using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YtsTorrents.QBittorrent;

public interface IDownloadClient
{
    Task EnsureCategoryAsync(string category, string savePath, CancellationToken cancellationToken);

    Task AddMagnetAsync(string magnet, string category, string savePath, CancellationToken cancellationToken);

    Task<TorrentInfo?> GetByHashAsync(string hash, CancellationToken cancellationToken);

    Task DeleteAsync(string hash, bool deleteFiles, CancellationToken cancellationToken);
}
