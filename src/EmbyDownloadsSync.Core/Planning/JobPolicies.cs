using System;
using System.Linq;
using System.Text.RegularExpressions;
using EmbyDownloadsSync.Core.Domain;

namespace EmbyDownloadsSync.Core.Planning;

internal static class JobPolicies
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(500);

    public static bool Matches(DownloadJob job, JobFilter filter, DateTime snapshotUtc)
    {
        if (!MatchesPattern(job.Name, filter.IncludeNamePattern, filter.PatternsAreRegularExpressions, true) ||
            !MatchesPattern(job.Name, filter.ExcludeNamePattern, filter.PatternsAreRegularExpressions, false))
        {
            return false;
        }

        if (filter.IncludedStatuses.Count > 0 && !filter.IncludedStatuses.Contains(job.Status, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.ExcludedStatuses.Contains(job.Status, StringComparer.OrdinalIgnoreCase) ||
            filter.UserId.HasValue && filter.UserId != job.UserId ||
            !string.IsNullOrWhiteSpace(filter.Category) && !string.Equals(filter.Category, job.Category, StringComparison.OrdinalIgnoreCase) ||
            filter.UnwatchedOnly.HasValue && filter.UnwatchedOnly != job.UnwatchedOnly ||
            filter.SyncNewContent.HasValue && filter.SyncNewContent != job.SyncNewContent ||
            !TextEqualsOrEmpty(filter.Quality, job.Quality) ||
            !TextEqualsOrEmpty(filter.Profile, job.Profile) ||
            !TextEqualsOrEmpty(filter.Container, job.Container) ||
            !TextEqualsOrEmpty(filter.VideoCodec, job.VideoCodec) ||
            !TextEqualsOrEmpty(filter.AudioCodec, job.AudioCodec) ||
            filter.MinimumBitrate.HasValue && (job.Bitrate ?? 0) < filter.MinimumBitrate ||
            filter.MaximumBitrate.HasValue && (job.Bitrate ?? int.MaxValue) > filter.MaximumBitrate ||
            filter.MinimumItemLimit.HasValue && (job.ItemLimit ?? 0) < filter.MinimumItemLimit ||
            filter.MaximumItemLimit.HasValue && (job.ItemLimit ?? int.MaxValue) > filter.MaximumItemLimit)
        {
            return false;
        }

        if (filter.MaximumAgeDays.HasValue && (!job.DateCreatedUtc.HasValue || job.DateCreatedUtc.Value < snapshotUtc.AddDays(-filter.MaximumAgeDays.Value)))
        {
            return false;
        }

        if (filter.IncludedItemIds.Count > 0 && !job.RequestedItemIds.Any(filter.IncludedItemIds.Contains))
        {
            return false;
        }

        return filter.ExcludedItemIds.Count == 0 || !job.RequestedItemIds.Any(filter.ExcludedItemIds.Contains);
    }

    private static bool TextEqualsOrEmpty(string filter, string value) =>
        string.IsNullOrWhiteSpace(filter) || string.Equals(filter.Trim(), value?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static DownloadJob Transform(DownloadJob source, string targetId, JobTransform transform)
    {
        var result = source.CopyTo(targetId);
        result.Id = null;
        result.Status = string.Empty;
        result.Name = transform.NamePrefix + result.Name + transform.NameSuffix;
        if (transform.OverrideUserId) result.UserId = transform.UserId;
        if (transform.UnwatchedOnly.HasValue) result.UnwatchedOnly = transform.UnwatchedOnly;
        if (transform.SyncNewContent.HasValue) result.SyncNewContent = transform.SyncNewContent;
        if (transform.ItemLimit.HasValue) result.ItemLimit = transform.ItemLimit;
        if (transform.Bitrate.HasValue) result.Bitrate = transform.Bitrate;
        if (!string.IsNullOrWhiteSpace(transform.Quality)) result.Quality = transform.Quality;
        if (!string.IsNullOrWhiteSpace(transform.Profile)) result.Profile = transform.Profile;
        if (!string.IsNullOrWhiteSpace(transform.Container)) result.Container = transform.Container;
        if (!string.IsNullOrWhiteSpace(transform.VideoCodec)) result.VideoCodec = transform.VideoCodec;
        if (!string.IsNullOrWhiteSpace(transform.AudioCodec)) result.AudioCodec = transform.AudioCodec;
        return result;
    }

    private static bool MatchesPattern(string value, string pattern, bool regularExpression, bool include)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        var regex = regularExpression
            ? pattern
            : "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        var matches = Regex.IsMatch(
            value ?? string.Empty,
            regex,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            PatternTimeout);
        return include ? matches : !matches;
    }
}
