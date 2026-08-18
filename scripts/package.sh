#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <version>" >&2
  exit 2
fi

package_version="$1"
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/.." && pwd)"
build_output="$repository_root/src/EmbyDownloadsSync.Plugin/bin/Release/netstandard2.0/deploy/EmbyDownloadsSync.dll"
output_dir="$repository_root/dist"
staging_dir="$(mktemp -d "${TMPDIR:-/tmp}/emby-downloads-sync-package.XXXXXX")"
trap 'rm -rf "$staging_dir"' EXIT

if [[ ! -f "$build_output" ]]; then
  echo "Release build not found. Run: dotnet build EmbyDownloadsSync.sln -c Release" >&2
  exit 1
fi

mkdir -p "$output_dir"
cp "$build_output" "$repository_root/README.md" "$repository_root/LICENSE" "$staging_dir/"
archive_name="emby-downloads-sync-${package_version}.zip"
archive_path="$output_dir/$archive_name"
rm -f "$archive_path" "$archive_path.sha256"
(cd "$staging_dir" && zip -q -r "$archive_path" .)
(cd "$output_dir" && sha256sum "$archive_name" > "$archive_name.sha256")
echo "$archive_path"
