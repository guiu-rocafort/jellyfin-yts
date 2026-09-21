using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YtsTorrents.Yts;

public interface IYtsClient
{
    Task<IReadOnlyList<YtsMovie>> SearchAsync(string query, int limit, CancellationToken cancellationToken);

    Task<YtsMovie?> GetByMovieIdAsync(long movieId, CancellationToken cancellationToken);

    Task<YtsMovie?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken);
}
