namespace WatchTogether.Core.Entities;

// We use IdentityUser<Guid> directly in User entity, so this file is not needed
// Keeping it for compatibility if needed in future
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
