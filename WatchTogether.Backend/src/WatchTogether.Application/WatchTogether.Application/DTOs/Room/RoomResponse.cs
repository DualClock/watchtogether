namespace WatchTogether.Application.DTOs.Room;

public class RoomResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorUsername { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Purpose { get; set; } = "cinema";
    public int MaxUsers { get; set; }
    public int CurrentUsers { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoomDetailResponse : RoomResponse
{
    public List<RoomMemberDto> Members { get; set; } = new();
    public PlaylistDto? Playlist { get; set; }
    
    // Video state
    public string? CurrentVideoUrl { get; set; }
    public string? CurrentVideoType { get; set; }
    public bool IsVideoPlaying { get; set; }
    public double VideoCurrentTime { get; set; }
}

public class RoomMemberDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class PlaylistDto
{
    public Guid Id { get; set; }
    public Guid? CurrentVideoId { get; set; }
    public bool IsPlaying { get; set; }
    public double CurrentTime { get; set; }
    public List<PlaylistItemDto> Items { get; set; } = new();
}

public class PlaylistItemDto
{
    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public string AddedBy { get; set; } = string.Empty;
}
