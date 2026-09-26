# Installation

## Requirements

- Jellyfin **12.1** or later in the 12.1 ABI line.
- A [qBittorrent](https://www.qbittorrent.org/) instance with the **WebUI enabled**, reachable
  from the Jellyfin server.
- Jellyfin must be able to read the files qBittorrent downloads **at the same path qBittorrent
  reports them at**. See [Configuration → Paths](configuration.md#paths-read-this-first).

## Option A: plugin repository (recommended)

This way Jellyfin can show you updates for the plugin.

1. Open **Dashboard → Plugins → Repositories** (the *Manage repositories* button on the Plugins page).
2. Add a repository with any name (e.g. `YTS Torrents`) and this URL:
   ```
   https://raw.githubusercontent.com/guiu-rocafort/jellyfin-yts/main/manifest.json
   ```
3. Go to the **Catalog** tab, find **YTS Torrents** (under *Movies & Shows*) and install it.
4. Restart Jellyfin.

After the restart, two entries appear in the dashboard sidebar under *Plugins*:
**YTS Torrents Settings** and **YTS Torrents**.

## Option B: manual install

1. Download `YtsTorrents_<version>.zip` from the
   [Releases page](https://github.com/guiu-rocafort/jellyfin-yts/releases).
2. Extract it into your Jellyfin server's `plugins` directory so you end up with
   `plugins/YtsTorrents_<version>/Jellyfin.Plugin.YtsTorrents.dll` (plus `meta.json` and `thumb.png`).
   - Docker images: usually `/config/plugins`
   - Linux packages: usually `/var/lib/jellyfin/plugins`
3. Restart Jellyfin.

With a manual install, the plugin's detail page may show *"An error occurred while getting the
plugin details from the repository"*. This is expected and harmless for plugins not installed
from a repository. Settings, Enable/Disable and Uninstall all still work.

## Next step

[Configure the plugin →](configuration.md)
