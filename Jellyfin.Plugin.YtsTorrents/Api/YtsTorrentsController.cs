using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.YtsTorrents.Api.Dto;
using Jellyfin.Plugin.YtsTorrents.Downloads;
using Jellyfin.Plugin.YtsTorrents.Yts;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YtsTorrents.Api;

[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("Plugins/YtsTorrents")]
public class YtsTorrentsController : ControllerBase
{
    private readonly IYtsClient _ytsClient;
    private readonly DownloadCoordinator _coordinator;
    private readonly ILogger<YtsTorrentsController> _logger;

    public YtsTorrentsController(IYtsClient ytsClient, DownloadCoordinator coordinator, ILogger<YtsTorrentsController> logger)
    {
        _ytsClient = ytsClient;
        _coordinator = coordinator;
        _logger = logger;
    }

    [HttpGet("Search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SearchResultDto[]>> Search([FromQuery] string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchResultDto>();
        }

        try
        {
            var movies = await _ytsClient.SearchAsync(query, 20, cancellationToken).ConfigureAwait(false);
            var results = movies.Select(ToDto).ToArray();
            return Ok(results);
        }
        catch (YtsUnavailableException ex)
        {
            _logger.LogWarning(ex, "YTS search failed for query '{Query}'", query);
            return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
        }
    }

    [HttpPost("Downloads")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult> StartDownload([FromBody] DownloadRequestDto request, CancellationToken cancellationToken)
    {
        if (!MagnetBuilder.IsValidHash(request.Hash))
        {
            return BadRequest("Not a valid torrent hash.");
        }

        YtsMovie? movie;
        try
        {
            // Re-fetch server-side by movie id rather than trusting client-supplied title/year, since
            // those flow into the on-disk import path -- never let request input drive that directly.
            movie = await _ytsClient.GetByMovieIdAsync(request.MovieId, cancellationToken).ConfigureAwait(false);
        }
        catch (YtsUnavailableException ex)
        {
            _logger.LogWarning(ex, "YTS lookup failed for movie {MovieId}", request.MovieId);
            return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
        }

        if (movie is null)
        {
            return BadRequest("Movie not found on YTS.");
        }

        var torrent = movie.Torrents.FirstOrDefault(t => string.Equals(t.Hash, request.Hash, StringComparison.OrdinalIgnoreCase));
        if (torrent is null)
        {
            return BadRequest("That torrent hash is not listed for this movie.");
        }

        await _coordinator.StartDownloadAsync(movie, torrent, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpGet("Downloads")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PendingDownloadDto[]> ListPending()
    {
        var dtos = _coordinator.ListPending()
            .Select(p => new PendingDownloadDto(p.Hash, p.MovieTitle, p.Year, p.State.ToString(), p.LastError, p.AddedUtc))
            .ToArray();
        return Ok(dtos);
    }

    private static SearchResultDto ToDto(YtsMovie movie)
    {
        var torrents = movie.Torrents
            .Select(t => new TorrentDto(t.Hash, t.Quality, t.Type, t.Size, t.Seeds, t.Peers))
            .ToArray();
        return new SearchResultDto(movie.Id, movie.Title, movie.Year, movie.Rating, torrents);
    }
}
