using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmbyDownloadsSync.Core.Domain;
using MediaBrowser.Model.Sync;

namespace EmbyDownloadsSync.Plugin.Services;

internal sealed class SyncJobQueryPayload
{
    public List<SyncJobPayload>? Items { get; set; }
}

internal sealed class SyncJobCreationPayload
{
    public SyncJobPayload? Job { get; set; }
}

internal sealed class SyncJobPayload
{
    public long? Id { get; set; }
    public string? TargetId { get; set; }
    public string? Name { get; set; }
    public long? UserId { get; set; }
    public long? ParentId { get; set; }
    public List<long?>? RequestedItemIds { get; set; }
    public long? ItemId { get; set; }
    public SyncCategory Category { get; set; }
    public SyncJobStatus Status { get; set; }
    public bool? UnwatchedOnly { get; set; }
    public bool? SyncNewContent { get; set; }
    public int? ItemLimit { get; set; }
    public int? Bitrate { get; set; }
    public string? Quality { get; set; }
    public string? Profile { get; set; }
    public string? Container { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public DateTimeOffset? DateCreated { get; set; }
}

internal sealed class SyncJobRequestPayload
{
    public string TargetId { get; set; } = string.Empty;
    public string ItemIds { get; set; } = string.Empty;
    public SyncCategory Category { get; set; }
    public string? ParentId { get; set; }
    public string Quality { get; set; } = string.Empty;
    public string Profile { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string VideoCodec { get; set; } = string.Empty;
    public string AudioCodec { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public bool? UnwatchedOnly { get; set; }
    public bool? SyncNewContent { get; set; }
    public int? ItemLimit { get; set; }
    public int? Bitrate { get; set; }
    public bool Downloaded { get; set; }
}

internal static class SyncJobMapper
{
    public static SyncJobRequestPayload ToRequest(DownloadJob job) => new SyncJobRequestPayload
    {
        TargetId = job.TargetId,
        ItemIds = string.Join(",", job.RequestedItemIds.Distinct().OrderBy(value => value).Select(value => value.ToString(CultureInfo.InvariantCulture))),
        Category = Enum.TryParse(job.Category, true, out SyncCategory category) ? category : SyncCategory.Latest,
        ParentId = Number(job.ParentId),
        Quality = job.Quality,
        Profile = job.Profile,
        Container = job.Container,
        VideoCodec = job.VideoCodec,
        AudioCodec = job.AudioCodec,
        Name = job.Name,
        UserId = Number(job.UserId),
        UnwatchedOnly = job.UnwatchedOnly,
        SyncNewContent = job.SyncNewContent,
        ItemLimit = job.ItemLimit,
        Bitrate = job.Bitrate,
        Downloaded = false,
    };

    public static DownloadJob ToDomain(SyncJobPayload job) => new DownloadJob
    {
        Id = job.Id,
        TargetId = job.TargetId ?? string.Empty,
        Name = job.Name ?? string.Empty,
        UserId = job.UserId,
        ParentId = job.ParentId,
        RequestedItemIds = RequestedItems(job),
        Category = job.Category.ToString(),
        Status = job.Status.ToString(),
        UnwatchedOnly = job.UnwatchedOnly,
        SyncNewContent = job.SyncNewContent,
        ItemLimit = job.ItemLimit,
        Bitrate = job.Bitrate,
        Quality = job.Quality ?? string.Empty,
        Profile = job.Profile ?? string.Empty,
        Container = job.Container ?? string.Empty,
        VideoCodec = job.VideoCodec ?? string.Empty,
        AudioCodec = job.AudioCodec ?? string.Empty,
        DateCreatedUtc = job.DateCreated?.UtcDateTime,
    };

    private static string? Number(long? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static IList<long> RequestedItems(SyncJobPayload job)
    {
        var requested = (job.RequestedItemIds ?? new List<long?>())
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToList();
        if (requested.Count == 0 && job.ItemId.HasValue) requested.Add(job.ItemId.Value);
        return requested;
    }
}
