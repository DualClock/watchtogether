namespace WatchTogether.Core.Entities;

public class RoomMember
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public RoomRole Role { get; set; } = RoomRole.Viewer;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public DateTime? MutedUntil { get; set; }
    public bool IsActive { get; set; } = true;

    public Room Room { get; set; } = null!;
    public User User { get; set; } = null!;

    public bool IsMuted => MutedUntil != null && MutedUntil > DateTime.UtcNow;
}

public enum RoomRole
{
    Viewer,
    Member,
    Moderator,
    Owner
}
