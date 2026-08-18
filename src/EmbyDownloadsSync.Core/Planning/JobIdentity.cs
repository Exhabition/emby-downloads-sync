using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EmbyDownloadsSync.Core.Domain;

namespace EmbyDownloadsSync.Core.Planning;

public static class JobIdentity
{
    public static string Fingerprint(DownloadJob job, JobMatchMode mode)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        var parts = new List<string> { CanonicalItemIds(job.RequestedItemIds) };
        switch (mode)
        {
            case JobMatchMode.ContentOnly:
                break;
            case JobMatchMode.ContentAndUser:
                parts.Add(Number(job.UserId));
                break;
            case JobMatchMode.NameAndContent:
                parts.Add(Text(job.Name));
                break;
            case JobMatchMode.Exact:
                parts.AddRange(new[]
                {
                    Number(job.UserId), Number(job.ParentId), Text(job.Category), Text(job.Name),
                    Bool(job.UnwatchedOnly), Bool(job.SyncNewContent), Number(job.ItemLimit), Number(job.Bitrate),
                    Text(job.Quality), Text(job.Profile), Text(job.Container), Text(job.VideoCodec), Text(job.AudioCodec),
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\u001f", parts)));
        return string.Concat(bytes.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    public static string ContentFingerprint(DownloadJob job) => Fingerprint(job, JobMatchMode.ContentOnly);

    private static string CanonicalItemIds(IEnumerable<long> itemIds) =>
        string.Join(",", (itemIds ?? Array.Empty<long>()).Distinct().OrderBy(value => value).Select(value => value.ToString(CultureInfo.InvariantCulture)));

    private static string Text(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string Number<T>(T? value) where T : struct => value?.ToString() ?? string.Empty;

    private static string Bool(bool? value) => value.HasValue ? (value.Value ? "1" : "0") : string.Empty;
}
