using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Plugin.Services;
using MediaBrowser.Model.Sync;

namespace EmbyDownloadsSync.Tests;

public sealed class SyncJobMapperTests
{
    [Fact]
    public void RequestPreservesEveryRequestedItemInCanonicalOrder()
    {
        var request = SyncJobMapper.ToRequest(new DownloadJob
        {
            TargetId = "tablet",
            RequestedItemIds = [30, 10, 20, 10],
            UserId = 42,
            ParentId = 7,
            Category = "NextUp",
            Name = "Travel",
            Bitrate = 5_000_000,
        });

        Assert.Equal("10,20,30", request.ItemIds);
        Assert.Equal("42", request.UserId);
        Assert.Equal("7", request.ParentId);
        Assert.Equal(SyncCategory.NextUp, request.Category);
        Assert.False(request.Downloaded);
    }

    [Fact]
    public void ResponseMappingDropsNullItemIdsWithoutDroppingValidItems()
    {
        var result = SyncJobMapper.ToDomain(new SyncJobPayload
        {
            TargetId = "tablet",
            RequestedItemIds = [1, null, 2],
            Category = SyncCategory.Latest,
            Status = SyncJobStatus.Queued,
        });

        Assert.Equal([1L, 2L], result.RequestedItemIds);
        Assert.Equal("Queued", result.Status);
    }

    [Fact]
    public void ResponseMappingFallsBackToSingularItemId()
    {
        var result = SyncJobMapper.ToDomain(new SyncJobPayload
        {
            TargetId = "tablet",
            ItemId = 42,
            Category = SyncCategory.Latest,
            Status = SyncJobStatus.Queued,
        });

        Assert.Equal([42L], result.RequestedItemIds);
    }
}
