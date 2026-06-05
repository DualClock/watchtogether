namespace WatchTogether.Core.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentMessageId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsSystem { get; set; }

    public Room Room { get; set; } = null!;
    public User User { get; set; } = null!;
    public Message? ParentMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();

    public bool IsDeleted => DeletedAt != null;
    public bool IsEdited => EditedAt != null;
}
