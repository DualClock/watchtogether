using Microsoft.AspNetCore.Identity;

namespace WatchTogether.Core.Entities;

public class User : IdentityUser<Guid>
{
    // IdentityUser already has: Id, UserName, Email, PasswordHash, etc.
    // We add our custom fields
    
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Offline;
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<Room> CreatedRooms { get; set; } = new List<Room>();
    public ICollection<RoomMember> RoomMemberships { get; set; } = new List<RoomMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<RoomBan> IssuedBans { get; set; } = new List<RoomBan>();
    public ICollection<RoomBan> ReceivedBans { get; set; } = new List<RoomBan>();
}

public enum UserStatus
{
    Offline,
    Online,
    InRoom,
    Away
}
