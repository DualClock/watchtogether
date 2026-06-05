namespace WatchTogether.Core.Entities;

public class Playlist
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid? CurrentVideoId { get; set; }
    public bool IsPlaying { get; set; }
    public double CurrentTime { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool IsLooping { get; set; }

    public Room Room { get; set; } = null!;
    public Video? CurrentVideo { get; set; }
    public ICollection<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
}
