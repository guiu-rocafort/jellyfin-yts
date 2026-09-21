using System;
using Jellyfin.Plugin.YtsTorrents.Downloads;
using Jellyfin.Plugin.YtsTorrents.QBittorrent;
using Jellyfin.Plugin.YtsTorrents.Yts;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.YtsTorrents;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient<IYtsClient, YtsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (X11; Linux x86_64) Jellyfin-YtsTorrents-Plugin");
        });

        // Registered as a singleton (not via AddHttpClient) because it needs a CookieContainer that
        // persists qBittorrent's session cookie across calls -- typed-client handler pooling can drop it.
        serviceCollection.AddSingleton<IDownloadClient, QBittorrentClient>();

        serviceCollection.AddSingleton<PendingDownloadStore>();
        serviceCollection.AddSingleton<DownloadCoordinator>();
    }
}
