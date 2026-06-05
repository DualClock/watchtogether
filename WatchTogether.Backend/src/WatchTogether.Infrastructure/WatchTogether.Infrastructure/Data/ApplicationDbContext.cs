using Microsoft.EntityFrameworkCore;
using WatchTogether.Core.Entities;

namespace WatchTogether.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql();
        }
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<RoomMember> RoomMembers { get; set; } = null!;
    public DbSet<RoomBan> RoomBans { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<Reaction> Reactions { get; set; } = null!;
    public DbSet<Video> Videos { get; set; } = null!;
    public DbSet<Playlist> Playlists { get; set; } = null!;
    public DbSet<PlaylistItem> PlaylistItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.UserName).HasMaxLength(30);
            entity.Property(e => e.DisplayName).HasMaxLength(50);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Room
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasOne(e => e.Creator)
                .WithMany(u => u.CreatedRooms)
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RoomMember
        modelBuilder.Entity<RoomMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoomId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Members)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.RoomMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoomBan
        modelBuilder.Entity<RoomBan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Bans)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.ReceivedBans)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BannedBy)
                .WithMany(u => u.IssuedBans)
                .HasForeignKey(e => e.BannedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(2000);
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(e => e.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Reaction
        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MessageId, e.UserId, e.Emoji }).IsUnique();
            entity.Property(e => e.Emoji).HasMaxLength(50);
            entity.HasOne(e => e.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Video
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.SourceUrl).HasMaxLength(1000);
            entity.Property(e => e.StoragePath).HasMaxLength(1000);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(1000);
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Videos)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AddedBy)
                .WithMany()
                .HasForeignKey(e => e.AddedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Playlist
        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Room)
                .WithOne(r => r.Playlist)
                .HasForeignKey<Playlist>(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CurrentVideo)
                .WithMany()
                .HasForeignKey(e => e.CurrentVideoId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // PlaylistItem
        modelBuilder.Entity<PlaylistItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Playlist)
                .WithMany(p => p.Items)
                .HasForeignKey(e => e.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Video)
                .WithMany(v => v.PlaylistItems)
                .HasForeignKey(e => e.VideoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
