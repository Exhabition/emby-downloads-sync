#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_dir/.." && pwd)"
integration_directory="$repository_root/tests/EmbyDownloadsSync.EmbyIntegration"
compose_file="$integration_directory/compose.yml"
config_directory="$integration_directory/.state/config"
plugin_directory="$config_directory/plugins"
deployable_plugin="$repository_root/src/EmbyDownloadsSync.Plugin/bin/Release/netstandard2.0/deploy/EmbyDownloadsSync.dll"
test_port="${EMBY_TEST_PORT:-18097}"
export EMBY_TEST_PUID="${EMBY_TEST_PUID:-$(id -u)}"
export EMBY_TEST_PGID="${EMBY_TEST_PGID:-$(id -g)}"

compose() { docker compose --file "$compose_file" "$@"; }

stage_plugin() {
  dotnet build "$repository_root/EmbyDownloadsSync.sln" --configuration Release --no-restore --disable-build-servers --maxcpucount:1
  if [[ ! -f "$deployable_plugin" ]]; then
    echo "The merged plugin is missing. Run dotnet restore first." >&2
    return 1
  fi
  mkdir -p "$plugin_directory"
  cp "$deployable_plugin" "$plugin_directory/EmbyDownloadsSync.dll"
}

show_logs() { compose logs --no-color emby; }

assert_requires_auth() {
  local method="$1"
  local path="$2"
  local status
  local curl_args=(--silent --output /dev/null --write-out '%{http_code}' --request "$method")
  if [[ "$method" != "GET" ]]; then
    curl_args+=(--header 'Content-Type: application/json' --data '{}')
  fi
  for prefix in /emby ""; do
    status="$(curl "${curl_args[@]}" "http://127.0.0.1:$test_port$prefix$path")"
    if [[ "$status" == "401" || "$status" == "403" ]]; then
      return 0
    fi
  done
  echo "Expected $method $path to be registered and require authentication; last HTTP status was $status." >&2
  return 1
}

verify_http_contracts() {
  assert_requires_auth GET "/Sync/Jobs"
  assert_requires_auth GET "/EmbyDownloadsSync/Routes"
  assert_requires_auth POST "/EmbyDownloadsSync/Preview"
}

wait_for_startup() {
  local deadline=$((SECONDS + 120))
  local container_id startup_logs
  while (( SECONDS < deadline )); do
    container_id="$(compose ps --quiet emby)"
    if [[ -z "$container_id" ]] || [[ "$(docker inspect --format '{{.State.Running}}' "$container_id")" != "true" ]]; then
      echo "The Emby integration container stopped before startup completed." >&2
      show_logs >&2
      return 1
    fi
    startup_logs="$(show_logs 2>&1)"
    if grep -Eiq "DllNotFoundException|TypeInitializationException|ReflectionTypeLoadException|Error Main: Error in appHost.Init" <<<"$startup_logs"; then
      printf '%s\n' "$startup_logs" >&2
      return 1
    fi
    if grep -Fq "Loading EmbyDownloadsSync" <<<"$startup_logs" &&
      (curl --fail --silent "http://127.0.0.1:$test_port/emby/System/Info/Public" >/dev/null 2>&1 ||
       curl --fail --silent "http://127.0.0.1:$test_port/System/Info/Public" >/dev/null 2>&1); then
      verify_http_contracts
      echo "Emby 4.9.5.0 loaded EmbyDownloadsSync on http://127.0.0.1:$test_port"
      return 0
    fi
    sleep 2
  done
  echo "Timed out waiting for Emby to load EmbyDownloadsSync." >&2
  show_logs >&2
  return 1
}

case "${1:-help}" in
  up) stage_plugin; compose up --detach --force-recreate emby ;;
  test) stage_plugin; compose up --detach --force-recreate emby; wait_for_startup ;;
  logs) compose logs --follow emby ;;
  status) compose ps ;;
  down) compose down ;;
  help|-h|--help) echo "Usage: ./scripts/emby-local.sh {up|test|logs|status|down}" ;;
  *) echo "Unknown command: $1" >&2; exit 2 ;;
esac
