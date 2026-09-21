# YTS Torrents — Jellyfin Plugin

Search [YTS](https://yts.gg) for movies from inside Jellyfin's dashboard, send a chosen torrent
release to an external [qBittorrent](https://www.qbittorrent.org/) instance, and have the finished
file automatically imported into a Jellyfin library folder (with a library scan triggered
afterwards) once the download completes. No embedded torrent engine — qBittorrent does the actual
downloading; this plugin only orchestrates search → download → import.

Targets Jellyfin **12.1** (net10.0). Key files are `Downloads/DownloadCoordinator.cs`
(orchestration), `QBittorrent/QBittorrentClient.cs`, `Yts/YtsClient.cs`, and
`Api/YtsTorrentsController.cs`.

## Features

- Search YTS by title from a dashboard page (`YTS Torrents` → Browse) and start a download with one
  click — no need to leave Jellyfin.
- Sends the chosen release to your own qBittorrent instance under a dedicated category, so it never
  mixes with your other torrents.
- A scheduled task (`Poll YTS Torrent Downloads`, every ~10 minutes) watches for completed downloads
  and imports them automatically: hardlinks (or copies/moves, configurable) the finished file into
  your chosen library folder as `Title (Year)/`, then triggers a Jellyfin library scan — no manual
  file moving.
- Everything runs server-side; the browse page just calls the plugin's own API endpoints
  (`Api/YtsTorrentsController.cs`), which require server admin elevation.

## Building

No local .NET SDK is required — everything builds inside the official `dotnet/sdk` Docker image via
the wrapper scripts:

```sh
./build.sh build Jellyfin.Plugin.YtsTorrents.slnx   # compile
./build.sh test tests/Jellyfin.Plugin.YtsTorrents.Tests/Jellyfin.Plugin.YtsTorrents.Tests.csproj
./publish.sh                                        # -> dist/YtsTorrents_<version>/ (DLL + meta.json)
```

## Installing on your own server

Run `./publish.sh` (see above) and copy the resulting `dist/YtsTorrents_<version>/` folder into
your Jellyfin server's plugins directory (the one containing one folder per installed plugin) and
restart Jellyfin. It'll show up
as "YTS Torrents" under Dashboard → Plugins, with an "An error occurred while getting the plugin
details from the repository" warning on its detail page — that's expected and harmless for any
plugin installed by hand rather than through a registered repository; Settings/Enable/Disable/
Uninstall all still work normally.

Then, from the plugin's Settings page, fill in:
- **qBittorrent WebUI URL / username / password** — where your qBittorrent instance's WebUI lives
- **qBittorrent category** — a dedicated category the plugin creates and uses, so it can tell its
  own torrents apart from anything else in qBittorrent
- **Staging save path** — a path in *qBittorrent's own filesystem view* where it saves torrents
  while downloading. Must **not** be inside (or overlap with) a Jellyfin library folder, otherwise
  partially-downloaded files get scanned into your library
- **Destination library folder** — which Jellyfin library folder finished movies are imported into
- **Import mode** — Hardlink (default; needs the staging path and library folder to be on the same
  filesystem), Copy, or Move

## Local end-to-end test environment

`dev/docker-compose.dev.yml` brings up Jellyfin 12.1 and qBittorrent sharing a volume
(`dev/data/shared/...`), which is required for `ImportMode.Hardlink` to actually hardlink instead
of falling back to a copy.

```sh
./publish.sh dev/data/jellyfin/config/plugins/YtsTorrents_0.1.0.0
docker compose -f dev/docker-compose.dev.yml up -d
```

- Jellyfin dashboard: http://localhost:8096
- qBittorrent WebUI: http://localhost:8081 (default creds are printed in `docker logs yts-dev-qbittorrent` on first start — change the password immediately)

Then:

1. In qBittorrent, note the WebUI username/password.
2. In Jellyfin, add a library pointed at `/media` (mapped from `dev/data/shared/media`).
3. Install/enable the plugin (Dashboard → Plugins → YTS Torrents), and fill in:
   - qBittorrent WebUI URL: `http://qbittorrent:8081` (the two containers share a Docker network — use the service name, not `localhost`)
   - qBittorrent username/password
   - Staging save path: a path *inside qBittorrent's own filesystem view*, e.g. `/downloads/staging` — must **not** be a Jellyfin library folder, so partially-downloaded files are never scanned
   - Destination library folder: pick your library from the dropdown
4. Open the "YTS Torrents" browse page, search a small/legally-distributed title (e.g. a
   Creative-Commons film such as *Sintel*, if listed), click Download, and watch it complete.
5. Confirm the movie shows up in your Jellyfin library without any manual steps.

Rebuilding after a code change: re-run
`./publish.sh dev/data/jellyfin/config/plugins/YtsTorrents_0.1.0.0` then
`docker compose -f dev/docker-compose.dev.yml restart jellyfin`.

## Security notes

- The `Search`/`Downloads` API endpoints require server admin (`RequiresElevation`) — they trigger
  internet downloads and disk writes.
- qBittorrent credentials are stored the same way every Jellyfin plugin stores third-party
  credentials: in the plugin's XML config, unencrypted on disk. Firewall the qBittorrent WebUI port
  to the Jellyfin host/network only.
- Import target paths are always resolved from a server-side movie lookup and sanitized/verified to
  stay under the configured library folder (see `Downloads/MovieFolderNamer.cs` and
  `PathSafetyTests.cs`) — never built from unvalidated client input.
