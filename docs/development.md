# Development

The plugin targets Jellyfin **12.1** (`net10.0`, `targetAbi` 12.1.0.0).

## Code map

| Path | Purpose |
|---|---|
| `Jellyfin.Plugin.YtsTorrents/Downloads/DownloadCoordinator.cs` | Orchestration: start downloads, poll qBittorrent, import finished files |
| `Jellyfin.Plugin.YtsTorrents/QBittorrent/QBittorrentClient.cs` | qBittorrent WebUI API client |
| `Jellyfin.Plugin.YtsTorrents/Yts/YtsClient.cs` | YTS API client |
| `Jellyfin.Plugin.YtsTorrents/Api/YtsTorrentsController.cs` | REST endpoints under `/Plugins/YtsTorrents` (admin only) |
| `Jellyfin.Plugin.YtsTorrents/Tasks/PollDownloadsTask.cs` | Scheduled task that drives polling/importing |
| `Jellyfin.Plugin.YtsTorrents/Configuration/configPage.html` | Settings page |
| `Jellyfin.Plugin.YtsTorrents/Web/browsePage.html` | Search/downloads page |
| `tests/` | xUnit tests |

## Building

No local .NET SDK is required. Everything builds inside the official `dotnet/sdk` Docker image via
the wrapper scripts:

```sh
./build.sh build Jellyfin.Plugin.YtsTorrents.slnx   # compile
./build.sh test tests/Jellyfin.Plugin.YtsTorrents.Tests/Jellyfin.Plugin.YtsTorrents.Tests.csproj
./publish.sh                                        # -> dist/YtsTorrents_<version>/ (DLL + meta.json + thumb.png)
```

The plugin version comes from `meta.json.template`.

## Local end-to-end test environment

`dev/docker-compose.dev.yml` brings up Jellyfin 12.1 and qBittorrent sharing a volume
(`dev/data/shared/...`), which `ImportMode.Hardlink` needs in order to actually hardlink instead of
falling back to a copy.

```sh
./publish.sh dev/data/jellyfin/config/plugins/YtsTorrents_0.1.0.0
docker compose -f dev/docker-compose.dev.yml up -d
```

- Jellyfin dashboard: http://localhost:8096
- qBittorrent WebUI: http://localhost:8081 (the temporary password is printed in
  `docker logs yts-dev-qbittorrent` on first start. Change it immediately.)

Then:

1. In Jellyfin, add a Movies library pointed at `/media` (mapped from `dev/data/shared/media`).
2. Open **YTS Torrents Settings** and fill in:
   - qBittorrent WebUI URL: `http://qbittorrent:8081` (use the service name, not `localhost`)
   - qBittorrent username/password
   - Staging save path: `/downloads/staging`
   - Destination library folder: your `/media` library
3. Open **YTS Torrents**, search for a small, legally distributed title (e.g. the
   Creative-Commons film *Sintel*), click Download, and watch it complete.
4. Confirm the movie shows up in your Jellyfin library without any manual steps.

After a code change, re-run the same `./publish.sh dev/data/...` command, then
`docker compose -f dev/docker-compose.dev.yml restart jellyfin`.

## Releasing

1. Bump `version` and `changelog` in `meta.json.template`.
2. `./publish.sh`, then zip the contents of `dist/YtsTorrents_<version>/` as
   `YtsTorrents_<version>.zip`.
3. Tag `v<version>` and create a GitHub release with the zip attached.
4. Add a new entry at the top of `versions` in `manifest.json`, with the release asset URL as
   `sourceUrl` and the zip's MD5 (`md5sum YtsTorrents_<version>.zip`) as `checksum`. Commit this to
   `main`, since that's the branch the repository URL points to.

## Documentation

User docs live in `docs/` as plain Markdown rendered by GitHub. When you add or change a setting,
update [configuration.md](configuration.md), and add a [troubleshooting](troubleshooting.md) entry
for any new user-visible error message.
