namespace WatchTogether.Core.Entities;

public class PlaylistItem
{
    public Guid Id { get; set; }
    public Guid PlaylistId { get; set; }
    public Guid VideoId { get; set; }
    public int Order { get; set; }
    public Guid AddedById { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public Playlist Playlist { get; set; } = null!;
    public Video Video { get; set; } = null!;
}
