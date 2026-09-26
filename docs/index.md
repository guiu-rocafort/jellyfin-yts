# YTS Torrents — User Manual

YTS Torrents adds two pages to your Jellyfin dashboard:

- **YTS Torrents Settings**, where you connect the plugin to qBittorrent and choose the library it
  imports into.
- **YTS Torrents**, where you search YTS, start downloads and watch their progress.

## How it works

```
 Search (YTS API) ──► Download (qBittorrent) ──► Import (library folder) ──► Library scan
```

1. You search for a movie and pick a release (quality/size).
2. The plugin sends the release to qBittorrent as a magnet link, in its own category and staging
   folder.
3. A Jellyfin scheduled task checks qBittorrent every minute. When a download finishes, the plugin
   places the video (and any subtitles) in your library as `Title (Year)/` and triggers a library
   scan.

## Contents

1. [Installation](installation.md): add the plugin repository, or install by hand
2. [Configuration](configuration.md): every setting explained, plus the path rules that matter
3. [Usage](usage.md): searching, downloading, and download states
4. [Troubleshooting](troubleshooting.md): common problems and how to fix them

Found a bug that isn't covered here?
[Open an issue](https://github.com/guiu-rocafort/jellyfin-yts/issues/new/choose).
