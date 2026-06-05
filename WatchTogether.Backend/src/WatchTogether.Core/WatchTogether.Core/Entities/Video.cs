namespace WatchTogether.Core.Entities;

public class Video
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public VideoSourceType SourceType { get; set; }
    public string? SourceUrl { get; set; }
    public string? StoragePath { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Duration { get; set; }
    public Guid AddedById { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public VideoStatus Status { get; set; } = VideoStatus.Pending;

    public Room Room { get; set; } = null!;
    public User AddedBy { get; set; } = null!;
    public ICollection<PlaylistItem> PlaylistItems { get; set; } = new List<PlaylistItem>();
}

public enum VideoSourceType
{
    Upload,
    YouTube,
    DirectUrl
}

public enum VideoStatus
{
    Pending,
    Processing,
    Ready,
    Error
}
