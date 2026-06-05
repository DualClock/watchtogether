namespace WatchTogether.Core.Entities;

public class RoomBan
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public Guid BannedById { get; set; }
    public string? Reason { get; set; }
    public DateTime BannedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    public Room Room { get; set; } = null!;
    public User User { get; set; } = null!;
    public User BannedBy { get; set; } = null!;

    public bool IsActive => !IsRevoked && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
}
