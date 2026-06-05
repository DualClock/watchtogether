using WatchTogether.Core.Entities;
using WatchTogether.Core.Interfaces;
using WatchTogether.Infrastructure.Data;

namespace WatchTogether.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    public IRepository<User> Users { get; }
    public IRepository<Room> Rooms { get; }
    public IRepository<RoomMember> RoomMembers { get; }
    public IRepository<Message> Messages { get; }
    public IRepository<Video> Videos { get; }
    public IRepository<Playlist> Playlists { get; }
    public IRepository<PlaylistItem> PlaylistItems { get; }
    public IRepository<RefreshToken> RefreshTokens { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        Rooms = new Repository<Room>(context);
        RoomMembers = new Repository<RoomMember>(context);
        Messages = new Repository<Message>(context);
        Videos = new Repository<Video>(context);
        Playlists = new Repository<Playlist>(context);
        PlaylistItems = new Repository<PlaylistItem>(context);
        RefreshTokens = new Repository<RefreshToken>(context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}
