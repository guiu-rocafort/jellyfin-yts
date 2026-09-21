#!/usr/bin/env bash
# Runs dotnet inside the official SDK container so no local .NET install is required.
# Usage: ./build.sh <dotnet-args...>
#   ./build.sh build
#   ./build.sh test
#   ./build.sh publish Jellyfin.Plugin.YtsTorrents/Jellyfin.Plugin.YtsTorrents.csproj -c Release -o dist
set -euo pipefail

cd "$(dirname "$0")"
mkdir -p .nuget-cache

docker run --rm \
  --user "$(id -u):$(id -g)" \
  -e HOME=/tmp/home \
  -e DOTNET_CLI_HOME=/tmp/home \
  -e NUGET_PACKAGES=/nuget-cache \
  -v "$PWD":/src \
  -v "$PWD/.nuget-cache":/nuget-cache \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet "$@"
