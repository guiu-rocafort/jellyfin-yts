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
            new PluginPageInfo
            {
                Name = "YtsTorrentsConfig",
                EmbeddedResourcePath = string.Format("{0}.Configuration.configPage.html", GetType().Namespace),
            },
            new PluginPageInfo
            {
                Name = "YtsTorrentsBrowse",
                EmbeddedResourcePath = string.Format("{0}.Web.browsePage.html", GetType().Namespace),
            },
        };
    }
}
