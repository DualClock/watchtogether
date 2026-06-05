namespace WatchTogether.Application.DTOs.Video;

public class VideoResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? Duration { get; set; }
    public string AddedBy { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AddVideoRequest
{
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
}
