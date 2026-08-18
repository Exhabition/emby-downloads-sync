using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Core.Planning;

namespace EmbyDownloadsSync.Tests;

public sealed class JobIdentityTests
{
    [Fact]
    public void ContentIdentityIgnoresItemOrderAndDuplicates()
    {
        var first = Job("one", 30, 10, 20, 20);
        var second = Job("two", 20, 30, 10);

        Assert.Equal(JobIdentity.ContentFingerprint(first), JobIdentity.ContentFingerprint(second));
    }

    [Fact]
    public void ExactIdentityDetectsTechnicalDifferences()
    {
        var first = Job("Movie", 10);
        first.Bitrate = 1_000_000;
        var second = Job("Movie", 10);
        second.Bitrate = 2_000_000;

        Assert.NotEqual(JobIdentity.Fingerprint(first, JobMatchMode.Exact), JobIdentity.Fingerprint(second, JobMatchMode.Exact));
        Assert.Equal(JobIdentity.Fingerprint(first, JobMatchMode.ContentOnly), JobIdentity.Fingerprint(second, JobMatchMode.ContentOnly));
    }

    private static DownloadJob Job(string name, params long[] itemIds) => new DownloadJob
    {
        Name = name,
        RequestedItemIds = itemIds,
    };
}
