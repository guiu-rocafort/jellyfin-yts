using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.YtsTorrents.Api.Dto;
using Jellyfin.Plugin.YtsTorrents.Downloads;
using Jellyfin.Plugin.YtsTorrents.QBittorrent;
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
    private readonly IDownloadClient _downloadClient;
    private readonly ILogger<YtsTorrentsController> _logger;

    public YtsTorrentsController(
        IYtsClient ytsClient,
        DownloadCoordinator coordinator,
        IDownloadClient downloadClient,
        ILogger<YtsTorrentsController> logger)
    {
        _ytsClient = ytsClient;
        _coordinator = coordinator;
        _downloadClient = downloadClient;
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
            var results = movies.Select(m => ToDto(m, _coordinator)).ToArray();
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

    [HttpPost("TestConnection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TestConnectionResultDto>> TestConnection([FromBody] TestConnectionRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _downloadClient.TestConnectionAsync(request.QbUrl, request.QbUsername, request.QbPassword, cancellationToken).ConfigureAwait(false);
            return Ok(new TestConnectionResultDto(true, "Connected successfully."));
        }
        catch (QBittorrentException ex)
        {
            return Ok(new TestConnectionResultDto(false, ex.Message));
        }
    }

    [HttpGet("Downloads")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PendingDownloadDto[]> ListDownloads()
    {
        var dtos = _coordinator.ListDownloads()
            .Select(p => new PendingDownloadDto(p.Hash, p.MovieTitle, p.Year, p.State.ToString(), p.LastError, p.AddedUtc, p.Progress, p.QbState, p.DownloadSpeed, p.EstimatedTimeRemaining, p.UploadedBytes))
            .ToArray();
        return Ok(dtos);
    }

    private static SearchResultDto ToDto(YtsMovie movie, DownloadCoordinator coordinator)
    {
        var torrents = movie.Torrents
            .Select(t => new TorrentDto(t.Hash, t.Quality, t.Type, t.Size, t.Seeds, t.Peers, coordinator.IsDownloaded(t.Hash)))
            .ToArray();
        return new SearchResultDto(movie.Id, movie.Title, movie.Year, movie.Rating, torrents);
    }
}
