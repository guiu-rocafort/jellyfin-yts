using System.Linq;
using Jellyfin.Plugin.YtsTorrents.Yts;

namespace Jellyfin.Plugin.YtsTorrents.Tests;

public class YtsResponseParsingTests
{
    private const string SearchResponse = """
    {
      "status": "ok",
      "status_message": "Query was successful",
      "data": {
        "movie_count": 1,
        "limit": 1,
        "page_number": 1,
        "movies": [
          {
            "id": 78659,
            "imdb_code": "tt0384573",
            "title": "Swimmers",
            "year": 2005,
            "rating": 6.1,
            "genres": ["Drama"],
            "torrents": [
              {
                "url": "https://yts.gg/torrent/download/78FA5DA0...",
                "hash": "78FA5DA053BC45C9B72A29704DE86353600F7C0F",
                "quality": "720p",
                "type": "web",
                "size": "826.59 MB",
                "size_bytes": 866742436,
                "seeds": 0,
                "peers": 0
              }
            ]
          }
        ]
      }
    }
    """;

    private const string DetailsResponse = """
    {
      "status": "ok",
      "status_message": "Query was successful",
      "data": {
        "movie": {
          "id": 78659,
          "imdb_code": "tt0384573",
          "title": "Swimmers",
          "year": 2005,
          "rating": 6.1,
          "genres": ["Drama"],
          "torrents": []
        }
      }
    }
    """;

    private const string CloudflareChallengeResponse = "<!DOCTYPE html><html><head><title>Just a moment...</title></head><body>Checking your browser...</body></html>";

    [Fact]
    public void ParseSearchResponse_MapsMovieAndTorrentFields()
    {
        var movies = YtsResponseParser.ParseSearchResponse(SearchResponse);

        var movie = Assert.Single(movies);
        Assert.Equal(78659, movie.Id);
        Assert.Equal("tt0384573", movie.ImdbCode);
        Assert.Equal("Swimmers", movie.Title);
        Assert.Equal(2005, movie.Year);
        Assert.Equal(6.1, movie.Rating);
        Assert.Equal(new[] { "Drama" }, movie.Genres);

        var torrent = Assert.Single(movie.Torrents);
        Assert.Equal("78FA5DA053BC45C9B72A29704DE86353600F7C0F", torrent.Hash);
        Assert.Equal("720p", torrent.Quality);
        Assert.Equal("web", torrent.Type);
        Assert.Equal(866742436, torrent.SizeBytes);
    }

    [Fact]
    public void ParseMovieDetailsResponse_ReturnsSingleMovie()
    {
        var movie = YtsResponseParser.ParseMovieDetailsResponse(DetailsResponse);

        Assert.NotNull(movie);
        Assert.Equal("Swimmers", movie!.Title);
        Assert.Empty(movie.Torrents);
    }

    [Fact]
    public void ParseSearchResponse_OnMalformedBody_ThrowsYtsUnavailable()
    {
        Assert.Throws<YtsUnavailableException>(() => YtsResponseParser.ParseSearchResponse(CloudflareChallengeResponse));
    }

    [Fact]
    public void ParseSearchResponse_WithNoMovies_ReturnsEmptyList()
    {
        const string empty = """{"status":"ok","status_message":"ok","data":{"movie_count":0,"limit":20,"page_number":1}}""";

        var movies = YtsResponseParser.ParseSearchResponse(empty);

        Assert.Empty(movies);
    }
}
