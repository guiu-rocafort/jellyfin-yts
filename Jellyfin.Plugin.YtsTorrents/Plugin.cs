using System;
using System.Collections.Generic;
using Jellyfin.Plugin.YtsTorrents.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.YtsTorrents;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static readonly Guid PluginId = Guid.Parse("050cf8c8-72cc-49c4-8522-bf98c4b1b9aa");

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "YTS Torrents";

    public override Guid Id => PluginId;

    public override string Description => "Search YTS and download movies straight into your Jellyfin library via qBittorrent.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            // Listed first so Jellyfin's "Settings" button on the plugin's own detail page resolves
            // to this one (it picks the first EnableInMainMenu candidate, falling back to array
            // order) -- both pages also get their own persistent link under the dashboard sidebar's
            // "Plugins" section via EnableInMainMenu, which is the only UI path to the Browse page.
            new PluginPageInfo
            {
                Name = "YtsTorrentsConfig",
                DisplayName = "YTS Torrents Settings",
                EmbeddedResourcePath = string.Format("{0}.Configuration.configPage.html", GetType().Namespace),
                EnableInMainMenu = true,
                MenuIcon = "settings",
            },
            new PluginPageInfo
            {
                Name = "YtsTorrentsBrowse",
                DisplayName = "YTS Torrents",
                EmbeddedResourcePath = string.Format("{0}.Web.browsePage.html", GetType().Namespace),
                EnableInMainMenu = true,
                MenuIcon = "movie",
            },
        };
    }
}
