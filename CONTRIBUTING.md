# Contributing

Use conventional commits such as `feat:`, `fix:`, `docs:`, and `chore:`. Release Please derives versions and changelog entries from them.

Before opening a pull request:

```bash
dotnet restore EmbyDownloadsSync.sln
dotnet format EmbyDownloadsSync.sln --no-restore
dotnet build EmbyDownloadsSync.sln --configuration Release --no-restore
dotnet test EmbyDownloadsSync.sln --configuration Release --no-build
./scripts/smoke-deploy.sh
```

Keep route and matching logic in Core free of Emby types. Changes to identity, conflict resolution, transformations, or mutation safety require boundary and idempotency tests. Never log API keys, mutate Emby's database, or delete jobs that are not recorded as plugin-owned.
