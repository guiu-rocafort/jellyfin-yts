#!/usr/bin/env bash
# Builds the plugin and lays it out as a Jellyfin plugin folder, ready to copy into a server's
# plugins directory (or the dev stack's plugins volume, see dev/docker-compose.dev.yml).
#
# Usage: ./publish.sh [destination-dir]
#   ./publish.sh                                  # -> dist/YtsTorrents_<version>/
#   ./publish.sh dev/data/jellyfin/config/plugins/YtsTorrents_0.1.0.0
set -euo pipefail

cd "$(dirname "$0")"

VERSION=$(grep -m1 '"version"' meta.json.template | sed -E 's/.*"version": *"([^"]+)".*/\1/')
DEST="${1:-dist/YtsTorrents_${VERSION}}"

./build.sh publish Jellyfin.Plugin.YtsTorrents/Jellyfin.Plugin.YtsTorrents.csproj -c Release -o dist/_publish

mkdir -p "$DEST"
cp dist/_publish/Jellyfin.Plugin.YtsTorrents.dll "$DEST/"
cp Jellyfin.Plugin.YtsTorrents/thumb.png "$DEST/"

TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
sed "s/REPLACED_BY_publish.sh/${TIMESTAMP}/" meta.json.template > "$DEST/meta.json"

echo "Published plugin to: $DEST"
echo "Copy that folder into your Jellyfin server's plugins directory and restart Jellyfin."
