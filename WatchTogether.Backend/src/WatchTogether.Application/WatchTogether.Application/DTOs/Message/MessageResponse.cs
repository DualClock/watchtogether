namespace WatchTogether.Application.DTOs.Message;

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsSystem { get; set; }
    public List<ReactionDto> Reactions { get; set; } = new();
}

public class ReactionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Emoji { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentMessageId { get; set; }
}
