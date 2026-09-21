using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.YtsTorrents.Yts;

/// <summary>
/// Pure JSON-parsing seam for the YTS API, kept separate from HTTP concerns so it can be unit tested
/// with canned response fixtures (including malformed/Cloudflare-challenge bodies).
/// </summary>
public static class YtsResponseParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<YtsMovie> ParseSearchResponse(string json)
    {
        var envelope = Deserialize(json);
        var movies = envelope.Data?.Movies;
        if (movies is null)
        {
            return Array.Empty<YtsMovie>();
        }

        return movies.Select(ToYtsMovie).ToList();
    }

    public static YtsMovie? ParseMovieDetailsResponse(string json)
    {
        var envelope = Deserialize(json);
        var movie = envelope.Data?.Movie;
        return movie is null ? null : ToYtsMovie(movie);
    }

    private static ApiEnvelope Deserialize(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ApiEnvelope>(json, SerializerOptions);
            if (envelope is null || !string.Equals(envelope.Status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new YtsUnavailableException("YTS returned an unexpected response: " + (envelope?.StatusMessage ?? "empty body"));
            }

            return envelope;
        }
        catch (JsonException ex)
        {
            throw new YtsUnavailableException("YTS returned a non-JSON response (it may be behind a Cloudflare challenge or temporarily down).", ex);
        }
    }

    private static YtsMovie ToYtsMovie(ApiMovie movie)
    {
        var torrents = (movie.Torrents ?? new List<ApiTorrent>())
            .Select(t => new YtsTorrent(
                t.Url ?? string.Empty,
                t.Hash ?? string.Empty,
                t.Quality ?? string.Empty,
                t.Type ?? string.Empty,
                t.Size ?? string.Empty,
                t.SizeBytes,
                t.Seeds,
                t.Peers))
            .ToList();

        return new YtsMovie(
            movie.Id,
            movie.ImdbCode ?? string.Empty,
            movie.Title ?? string.Empty,
            movie.Year,
            movie.Rating,
            movie.Genres ?? new List<string>(),
            torrents);
    }

    private sealed class ApiEnvelope
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("status_message")]
        public string? StatusMessage { get; set; }

        [JsonPropertyName("data")]
        public ApiData? Data { get; set; }
    }

    private sealed class ApiData
    {
        [JsonPropertyName("movies")]
        public List<ApiMovie>? Movies { get; set; }

        [JsonPropertyName("movie")]
        public ApiMovie? Movie { get; set; }
    }

    private sealed class ApiMovie
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("imdb_code")]
        public string? ImdbCode { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("rating")]
        public double Rating { get; set; }

        [JsonPropertyName("genres")]
        public List<string>? Genres { get; set; }

        [JsonPropertyName("torrents")]
        public List<ApiTorrent>? Torrents { get; set; }
    }

    private sealed class ApiTorrent
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("quality")]
        public string? Quality { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("size_bytes")]
        public long SizeBytes { get; set; }

        [JsonPropertyName("seeds")]
        public int Seeds { get; set; }

        [JsonPropertyName("peers")]
        public int Peers { get; set; }
    }
}
