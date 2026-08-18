# Emby Downloads Sync

Emby Downloads Sync is an Emby Server plugin that synchronizes download jobs between devices using configurable, policy-driven routes.

## Features

- One-to-many, many-to-one, bidirectional, mesh, and explicit source-to-target routes.
- Content-only, content-and-user, name-and-content, and exact job matching.
- Include/exclude filters for names, statuses, users, categories, item IDs, bitrate, item limits, and age.
- Target-specific name, quality, profile, bitrate, container, and codec transformations.
- Conflict policies including target preservation, source priority, newest source, bitrate preference, and explicit variants.
- Immutable run snapshots that prevent bidirectional and cyclic routes from cascading newly created jobs.
- Global and per-route dry-run modes, mutation ceilings, cancellation, and per-job failure handling.
- Emby Scheduled Tasks integration, manual previews, recent run history, authenticated admin API, and declarative dashboard UI.

The plugin is create-only by default. Managed cleanup is intentionally disabled until Emby's job deletion contract is verified; user-created jobs are never deleted.

## Installation

Build or download `emby-downloads-sync-VERSION.zip`, extract `EmbyDownloadsSync.dll` into Emby's plugin directory, and restart Emby. Configure an administrator API key on the plugin settings page, then create routes from **Downloads Sync** in Emby's main menu.

The API key is required because Emby's public plugin SDK exposes device discovery but not its sync-job manager. The plugin sends the key only to Emby's loopback `/Sync/Jobs` endpoint and never logs it. Read requests use bounded retries; job creation is deliberately single-attempt to avoid duplicate jobs after an ambiguous timeout.

## Build and test

```bash
dotnet restore EmbyDownloadsSync.sln
dotnet build EmbyDownloadsSync.sln --configuration Release --no-restore
dotnet test EmbyDownloadsSync.sln --configuration Release --no-build
./scripts/smoke-deploy.sh
./scripts/package.sh 0.1.0
```

Run the pinned Emby 4.9.5.0 integration server with:

```bash
./scripts/emby-local.sh test
./scripts/emby-local.sh logs
./scripts/emby-local.sh down
```

See [Architecture](docs/architecture.md) for route and execution semantics.

## License

MIT. See [LICENSE](LICENSE).
