# Architecture

## Boundaries

`EmbyDownloadsSync.Core` targets `netstandard2.0` and contains no Emby types. It owns route validation, topology expansion, filtering, normalization, matching, transformations, conflict resolution, safety limits, and deterministic plan construction.

`EmbyDownloadsSync.Plugin` adapts Emby devices and sync jobs, stores configuration and run history, applies plans, exposes administrator APIs and UI pages, and registers the scheduled task.

Release builds merge Core into one platform-neutral `EmbyDownloadsSync.dll`. The generated all-endpoints client and standalone container are not runtime dependencies. Because the plugin has no native dependencies, the release publishes this DLL directly for Linux x64/ARM64 (glibc or musl), Windows x64, and macOS x64/ARM64 rather than producing redundant platform archives.

## Planning lifecycle

```text
device/job snapshot
       |
route validation and topology expansion
       |
filters -> transformations -> normalized identities
       |
source conflict resolution
       |
target diff and cross-route deduplication
       |
safety ceilings
       |
preview or apply
```

Every run fetches jobs once and plans against that immutable snapshot. A job created by one edge cannot become input to another edge until a later run. This makes mesh, bidirectional, overlapping, and cyclic route graphs deterministic.

## Identity

Content identity is the sorted, distinct set of requested item IDs. Other modes add user, name, or all stable behavioral and technical fields. Progress, status, timestamps, server IDs, and target IDs are excluded from exact identity.

## Safety

Create-only is the default reconciliation mode. Identical planned actions are deduplicated across routes. Conflicting target content is preserved unless a route explicitly permits variants. Global dry run overrides apply requests. Route-level ceilings convert excess mutations into visible limit actions.

Manual previews are retained in history without advancing a route's scheduled-run timestamp. Route-level dry runs are counted separately in run summaries. Sync-job reads may be retried after transient failures, while create requests are single-attempt because repeating a timed-out POST could create duplicates.

Plugin-created job IDs and fingerprints are recorded for a future managed cleanup mode. Cleanup remains disabled until the Emby update/delete API is verified in integration tests.

## Emby integration

Device discovery uses `IDeviceManager`. The public SDK does not expose the internal sync manager, so a minimal adapter calls only `GET /Sync/Jobs` and `POST /Sync/Jobs` through Emby's local API URL. Authentication uses the configured administrator API key in request headers.
