using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YtsTorrents.QBittorrent;

/// <summary>
/// Minimal client for the qBittorrent WebUI API (v2). Rolled by hand rather than taking on a third-party
/// NuGet dependency, since the surface actually needed here (login, add, info, category, delete) is small
/// and the available community packages are low-maintenance.
/// </summary>
public class QBittorrentClient : IDownloadClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger<QBittorrentClient> _logger;
    private readonly CookieContainer _cookieContainer = new();
    private readonly HttpClient _httpClient;
    private bool _authenticated;

    public QBittorrentClient(ILogger<QBittorrentClient> logger)
    {
        _logger = logger;
        var handler = new HttpClientHandler { CookieContainer = _cookieContainer };
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }

    private static string BaseUrl => (Plugin.Instance?.Configuration.QbUrl ?? "http://localhost:8080").TrimEnd('/');

    public async Task TestConnectionAsync(string url, string username, string password, CancellationToken cancellationToken)
    {
        var baseUrl = (url ?? string.Empty).TrimEnd('/');
        if (baseUrl.Length == 0)
        {
            throw new QBittorrentException("qBittorrent WebUI URL is empty.");
        }

        // A throwaway client/cookie jar, not the shared _httpClient -- testing (possibly wrong,
        // unsaved) credentials here must never disturb the authenticated session used for real
        // downloads in the background.
        var cookieContainer = new CookieContainer();
        using var handler = new HttpClientHandler { CookieContainer = cookieContainer };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

        var response = await PostLoginAsync(client, baseUrl, username, password, cancellationToken).ConfigureAwait(false);
        await EnsureLoginSucceededAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task EnsureCategoryAsync(string category, string savePath, CancellationToken cancellationToken)
    {
        var existing = await ExecuteWithAuthAsync(
            () => _httpClient.GetAsync($"{BaseUrl}/api/v2/torrents/categories", cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var json = await existing.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(category, out _))
        {
            return;
        }

        var form = new FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("category", category),
            new System.Collections.Generic.KeyValuePair<string, string>("savePath", savePath),
        });

        await ExecuteWithAuthAsync(
            () => _httpClient.PostAsync($"{BaseUrl}/api/v2/torrents/createCategory", form, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task AddMagnetAsync(string magnet, string category, string savePath, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(magnet), "urls" },
            { new StringContent(category), "category" },
            { new StringContent("false"), "paused" },
            { new StringContent("false"), "autoTMM" },
            { new StringContent(savePath), "savepath" },
        };

        var response = await ExecuteWithAuthAsync(
            () => _httpClient.PostAsync($"{BaseUrl}/api/v2/torrents/add", content, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new QBittorrentException($"qBittorrent rejected the torrent add request (HTTP {(int)response.StatusCode}).");
        }
    }

    public async Task<TorrentInfo?> GetByHashAsync(string hash, CancellationToken cancellationToken)
    {
        var response = await ExecuteWithAuthAsync(
            () => _httpClient.GetAsync($"{BaseUrl}/api/v2/torrents/info?hashes={hash}", cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var entries = JsonSerializer.Deserialize<TorrentInfoDto[]>(json, JsonOptions);
        if (entries is null || entries.Length == 0)
        {
            return null;
        }

        var dto = entries[0];
        return new TorrentInfo(dto.Hash ?? hash, dto.State ?? "unknown", dto.Progress, dto.ContentPath ?? string.Empty, dto.SavePath ?? string.Empty);
    }

    public async Task DeleteAsync(string hash, bool deleteFiles, CancellationToken cancellationToken)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("hashes", hash),
            new System.Collections.Generic.KeyValuePair<string, string>("deleteFiles", deleteFiles ? "true" : "false"),
        });

        await ExecuteWithAuthAsync(
            () => _httpClient.PostAsync($"{BaseUrl}/api/v2/torrents/delete", form, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> ExecuteWithAuthAsync(Func<Task<HttpResponseMessage>> request, CancellationToken cancellationToken)
    {
        if (!_authenticated)
        {
            await LoginAsync(cancellationToken).ConfigureAwait(false);
        }

        var response = await request().ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            _authenticated = false;
            await LoginAsync(cancellationToken).ConfigureAwait(false);
            response = await request().ConfigureAwait(false);
        }

        return response;
    }

    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        var response = await PostLoginAsync(_httpClient, BaseUrl, config?.QbUsername ?? string.Empty, config?.QbPassword ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);
        await EnsureLoginSucceededAsync(response, cancellationToken).ConfigureAwait(false);

        _authenticated = true;
        _logger.LogDebug("Authenticated with qBittorrent at {Url}", BaseUrl);
    }

    private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string baseUrl, string username, string password, CancellationToken cancellationToken)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string>("username", username),
            new System.Collections.Generic.KeyValuePair<string, string>("password", password),
        });

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v2/auth/login") { Content = form };
            request.Headers.Referrer = new Uri(baseUrl);
            return await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new QBittorrentException("Could not reach qBittorrent: " + ex.Message, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new QBittorrentException("Connection to qBittorrent timed out.", ex);
        }
        catch (UriFormatException ex)
        {
            throw new QBittorrentException("qBittorrent WebUI URL is not a valid URL: " + ex.Message, ex);
        }
    }

    private static async Task EnsureLoginSucceededAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // qBittorrent's login endpoint has returned different bodies across versions: older builds send
        // "200 Ok." / "200 Fails.", newer ones send a bare "204 No Content" on success. Treat any 2xx as
        // success unless the body explicitly says otherwise, rather than requiring an exact "Ok." match.
        var trimmedBody = body.Trim();
        var failed = !response.IsSuccessStatusCode || string.Equals(trimmedBody, "Fails.", StringComparison.OrdinalIgnoreCase);
        if (failed)
        {
            throw new QBittorrentException($"qBittorrent login failed (HTTP {(int)response.StatusCode}): {body}");
        }
    }

    private sealed class TorrentInfoDto
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("progress")]
        public double Progress { get; set; }

        [JsonPropertyName("content_path")]
        public string? ContentPath { get; set; }

        [JsonPropertyName("save_path")]
        public string? SavePath { get; set; }
    }
}
