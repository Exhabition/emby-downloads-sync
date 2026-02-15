using Emby.ApiClient.Model;

namespace EmbyDownloadsSync.Domain.ValueObjects;

public class SyncJobRequestOld
{
    public string TargetId { get; set; }
    public string ItemIds { get; set; }
    public string Name { get; set; }
    public string UserId { get; set; }
    public string ParentId { get; set; }
    public bool? UnwatchedOnly { get; set; }
    public bool? SyncNewContent { get; set; }
    public bool? Downloaded { get; set; }
    public int? ItemLimit { get; set; }
    public int? Bitrate { get; set; }
    public string Quality { get; set; }
    public string Profile { get; set; }
    public string Container { get; set; }
    public string VideoCodec { get; set; }
    public string AudioCodec { get; set; }

    public SyncJobRequestOld(SyncJob syncJob, string targetId)
    {
        TargetId = targetId;
        ItemIds = syncJob.ItemId.ToString();
        Name = syncJob.Name;
        UserId = syncJob.UserId?.ToString();
        ParentId = syncJob.ParentId?.ToString();

        // Behavioral flags
        UnwatchedOnly = syncJob.UnwatchedOnly;
        SyncNewContent = syncJob.SyncNewContent;
        Downloaded = false;
        ItemLimit = syncJob.ItemLimit;
        Bitrate = syncJob.Bitrate;

        Quality = syncJob.Quality;
        Profile = syncJob.Profile;
        Container = syncJob.Container;
        VideoCodec = syncJob.VideoCodec;
        AudioCodec = syncJob.AudioCodec;
    }
}
