using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.YtsTorrents.Yts;

public class YtsClient : IYtsClient
{
    private readonly HttpClient _httpClient;

    public YtsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static string BaseUrl =>
        (Plugin.Instance?.Configuration.YtsBaseUrl ?? "https://yts.gg/api/v2").TrimEnd('/');

    public async Task<IReadOnlyList<YtsMovie>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var clampedLimit = Math.Clamp(limit, 1, 50);
        var url = $"{BaseUrl}/list_movies.json?query_term={Uri.EscapeDataString(query)}&limit={clampedLimit}";
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return YtsResponseParser.ParseSearchResponse(json);
    }

    public async Task<YtsMovie?> GetByMovieIdAsync(long movieId, CancellationToken cancellationToken)
    {
        var json = await GetStringAsync($"{BaseUrl}/movie_details.json?movie_id={movieId}", cancellationToken).ConfigureAwait(false);
        return YtsResponseParser.ParseMovieDetailsResponse(json);
    }

    public async Task<YtsMovie?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/movie_details.json?imdb_id={Uri.EscapeDataString(imdbId)}";
        var json = await GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        return YtsResponseParser.ParseMovieDetailsResponse(json);
    }

    private async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new YtsUnavailableException("Could not reach YTS: " + ex.Message, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new YtsUnavailableException("YTS request timed out.", ex);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new YtsUnavailableException($"YTS returned HTTP {(int)response.StatusCode}.");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is not null && !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            throw new YtsUnavailableException("YTS returned a non-JSON response (likely a Cloudflare challenge page).");
        }

        return body;
    }
}
