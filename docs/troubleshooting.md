# Troubleshooting

Most problems leave a message in the Jellyfin log. Open **Dashboard → Logs**, pick the latest
`log_*.log` and search for `YtsTorrents`.

## Test connection fails

- **Connection refused / timeout.** The WebUI URL must be reachable **from the Jellyfin server**,
  not from your browser. In Docker, use the qBittorrent service name (`http://qbittorrent:8080`),
  not `localhost`, and check both containers share a network.
- **Wrong port.** qBittorrent's WebUI port is set in *qBittorrent → Options → WebUI*, or via
  `WEBUI_PORT` in the linuxserver.io image.
- **Authentication failed.** Check the username and password. On first start, the
  linuxserver.io image prints a temporary password in `docker logs <qbittorrent-container>`.
- **Banned IP.** After several failed logins, qBittorrent temporarily bans the client IP. Wait,
  or restart qBittorrent.

## Search shows an error or no results

- The YTS API may be down or blocked on your network. In Settings, change *YTS API base URL* to the
  mirror `https://movies-api.accel.li/api/v2` and try again.
- Try a shorter or different spelling of the title.

## Download is stuck in "Downloading"

- Check the torrent in qBittorrent (in the `jellyfin-imports` category, or your configured
  category). If it has no seeds, it may never finish. Try another release.
- The poll task may not be running. Check **Dashboard → Scheduled Tasks → Poll YTS Torrent
  Downloads** and click *Run* to trigger it manually.

## Download "Failed"

The error text on the row tells you why:

| Error | Cause and fix |
|---|---|
| *qBittorrent no longer has a record of this torrent.* | The torrent was removed from qBittorrent outside the plugin. Start the download again. |
| *qBittorrent reported state 'error'* / *'missingFiles'* | qBittorrent itself hit a problem, often a staging path it can't write to. Check the torrent in qBittorrent. |
| *No video files found under '…'* | Jellyfin can't see the downloaded files at the path qBittorrent reported, or the torrent had no video. Check [path rule 1](configuration.md#paths-read-this-first): Jellyfin must see qBittorrent's download folder **at the same path**. |
| *Access to the path '…' is denied* | The Jellyfin user can't read the download or write to the library folder. Fix file ownership/permissions (in Docker, check `PUID`/`PGID` on both containers). |

## Imported, but the movie doesn't appear in the library

- Wait for the library scan to finish (**Dashboard → Scheduled Tasks → Scan Media Library**).
- Make sure *Destination library folder* belongs to a **Movies** library.
- Check the movie has a proper name. YTS titles are used as-is for the `Title (Year)` folder.

## Hardlink mode is using extra disk space

The plugin falls back to copying when a hardlink isn't possible, and logs
`Could not hardlink … falling back to copy`. Staging and library must be on the **same filesystem**,
which in Docker means the **same volume mount**. See
[path rule 3](configuration.md#paths-read-this-first).

## Half-downloaded files show up in my library

Your staging save path is inside a library folder. Move it outside, see
[path rule 2](configuration.md#paths-read-this-first).

## "An error occurred while getting the plugin details from the repository"

This is harmless and only shows with a [manual install](installation.md#option-b-manual-install).
Install through the plugin repository to get rid of it.

## Still stuck?

[Open an issue](https://github.com/guiu-rocafort/jellyfin-yts/issues/new/choose) and include the
plugin version, Jellyfin version, how you run Jellyfin/qBittorrent (Docker, bare metal…), and the
relevant `YtsTorrents` log lines.
