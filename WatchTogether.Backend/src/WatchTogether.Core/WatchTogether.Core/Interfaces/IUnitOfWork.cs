using WatchTogether.Core.Entities;

namespace WatchTogether.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Room> Rooms { get; }
    IRepository<RoomMember> RoomMembers { get; }
    IRepository<Message> Messages { get; }
    IRepository<Video> Videos { get; }
    IRepository<Playlist> Playlists { get; }
    IRepository<PlaylistItem> PlaylistItems { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    
    Task<int> SaveChangesAsync();
}
