#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 0 ]]; then
  echo "Usage: $0" >&2
  exit 2
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/.." && pwd)"
build_output="$repository_root/src/EmbyDownloadsSync.Plugin/bin/Release/netstandard2.0/deploy/EmbyDownloadsSync.dll"
output_dir="$repository_root/dist"
release_dll="$output_dir/EmbyDownloadsSync.dll"
checksum_file="$release_dll.sha256"
release_license="$output_dir/LICENSE"

if [[ ! -f "$build_output" ]]; then
  echo "Release build not found. Run: dotnet build EmbyDownloadsSync.sln -c Release" >&2
  exit 1
fi

mkdir -p "$output_dir"
rm -f "$release_dll" "$checksum_file" "$release_license"
cp "$build_output" "$release_dll"
cp "$repository_root/LICENSE" "$release_license"
(cd "$output_dir" && sha256sum "$(basename "$release_dll")" > "$(basename "$checksum_file")")
echo "$release_dll"
