#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/.." && pwd)"
plugin_path="$repository_root/src/EmbyDownloadsSync.Plugin/bin/Release/netstandard2.0/deploy/EmbyDownloadsSync.dll"
smoke_test="$repository_root/tests/EmbyDownloadsSync.DeploySmoke/bin/Release/net10.0/EmbyDownloadsSync.DeploySmoke.dll"
dotnet "$smoke_test" "$plugin_path"
