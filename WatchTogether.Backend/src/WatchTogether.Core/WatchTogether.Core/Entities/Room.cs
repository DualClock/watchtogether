namespace WatchTogether.Core.Entities;

public class Room
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatorId { get; set; }
    public RoomType Type { get; set; } = RoomType.Public;
    public string? PasswordHash { get; set; }
    public int MaxUsers { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public RoomPurpose Purpose { get; set; } = RoomPurpose.Cinema;
    
    // Current video
    public string? CurrentVideoUrl { get; set; }
    public string? CurrentVideoType { get; set; } // "file", "youtube", "yandex", etc.
    public bool IsVideoPlaying { get; set; } = false;
    public double VideoCurrentTime { get; set; } = 0;
    public DateTime? VideoLastSyncAt { get; set; }

    // Navigation properties
    public User Creator { get; set; } = null!;
    public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<RoomBan> Bans { get; set; } = new List<RoomBan>();
    public Playlist? Playlist { get; set; }
    public ICollection<Video> Videos { get; set; } = new List<Video>();
}

public enum RoomType
{
    Public,
    ByLink,
    Private
}

public enum RoomPurpose
{
    Cinema,  // Watch movies together
    Call     // Video calls
}
