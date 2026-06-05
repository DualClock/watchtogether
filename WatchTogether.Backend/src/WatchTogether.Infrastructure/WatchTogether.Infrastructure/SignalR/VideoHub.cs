using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WatchTogether.Core.Entities;
using WatchTogether.Infrastructure.Data;

namespace WatchTogether.Infrastructure.SignalR;

public class VideoHub : Hub
{
    private readonly ApplicationDbContext _context;
    // Store last sync state per room for quick access
    private static readonly Dictionary<Guid, RoomVideoState> _roomStates = new();

    public VideoHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task JoinVideoRoom(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var isMember = await _context.RoomMembers
            .AnyAsync(m => m.RoomId == roomId && m.UserId == userId.Value && m.IsActive);

        if (!isMember) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"video-{roomId}");

        // Send current video state with server timestamp
        var state = await GetCurrentState(roomId);
        if (state != null)
        {
            await Clients.Caller.SendAsync("SyncState", state);
        }
    }

    public async Task LeaveVideoRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"video-{roomId}");
    }

    public async Task PlayVideo(Guid roomId, double currentTime, long clientTimestamp)
    {
        if (!await CanControlVideo(roomId)) return;

        var serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var latency = (serverTime - clientTimestamp) / 1000.0; // latency in seconds
        
        // Adjust time: add half latency (round trip / 2)
        var adjustedTime = currentTime + (latency / 2);

        await UpdateRoomVideoState(roomId, true, adjustedTime);
        
        // Broadcast with server timestamp so clients can sync
        await Clients.Group($"video-{roomId}").SendAsync("VideoPlayed", new { 
            CurrentTime = adjustedTime, 
            ServerTimestamp = serverTime 
        });
    }

    public async Task PauseVideo(Guid roomId, double currentTime, long clientTimestamp)
    {
        if (!await CanControlVideo(roomId)) return;

        var serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var latency = (serverTime - clientTimestamp) / 1000.0;
        var adjustedTime = currentTime + (latency / 2);

        await UpdateRoomVideoState(roomId, false, adjustedTime);
        
        await Clients.Group($"video-{roomId}").SendAsync("VideoPaused", new { 
            CurrentTime = adjustedTime, 
            ServerTimestamp = serverTime 
        });
    }

    public async Task SeekVideo(Guid roomId, double currentTime, long clientTimestamp)
    {
        if (!await CanControlVideo(roomId)) return;

        var serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        await UpdateRoomVideoState(roomId, _roomStates.TryGetValue(roomId, out var s) && s.IsPlaying, currentTime);
        
        await Clients.Group($"video-{roomId}").SendAsync("VideoSeeked", new { 
            CurrentTime = currentTime, 
            ServerTimestamp = serverTime 
        });
    }

    public async Task ChangeVideo(Guid roomId, string videoUrl, string videoType)
    {
        if (!await CanControlVideo(roomId)) return;

        var room = await _context.Rooms.FindAsync(roomId);
        if (room != null)
        {
            room.CurrentVideoUrl = videoUrl;
            room.CurrentVideoType = videoType;
            room.IsVideoPlaying = false;
            room.VideoCurrentTime = 0;
            room.VideoLastSyncAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        _roomStates[roomId] = new RoomVideoState
        {
            VideoUrl = videoUrl,
            VideoType = videoType,
            IsPlaying = false,
            CurrentTime = 0,
            LastSyncAt = DateTime.UtcNow
        };

        await Clients.Group($"video-{roomId}").SendAsync("VideoChanged", new { VideoUrl = videoUrl, VideoType = videoType });
    }

    // Periodic sync - called by admin every 5 seconds while playing
    public async Task SyncTime(Guid roomId, double currentTime, long clientTimestamp)
    {
        if (!await CanControlVideo(roomId)) return;

        var serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var latency = (serverTime - clientTimestamp) / 1000.0;
        var adjustedTime = currentTime + (latency / 2);

        await UpdateRoomVideoState(roomId, true, adjustedTime);

        // Broadcast to others (not admin)
        await Clients.GroupExcept($"video-{roomId}", Context.ConnectionId)
            .SendAsync("TimeSync", new { 
                CurrentTime = adjustedTime, 
                ServerTimestamp = serverTime 
            });
    }

    public async Task RequestSync(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var state = await GetCurrentState(roomId);
        if (state != null)
        {
            await Clients.Caller.SendAsync("SyncState", state);
        }
    }

    private async Task<VideoSyncState?> GetCurrentState(Guid roomId)
    {
        // Try memory first
        if (_roomStates.TryGetValue(roomId, out var memState))
        {
            var adjustedTime = memState.CurrentTime;
            if (memState.IsPlaying)
            {
                var elapsed = (DateTime.UtcNow - memState.LastSyncAt).TotalSeconds;
                adjustedTime += elapsed;
            }

            return new VideoSyncState
            {
                VideoUrl = memState.VideoUrl,
                VideoType = memState.VideoType,
                IsPlaying = memState.IsPlaying,
                CurrentTime = adjustedTime,
                ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        // Fallback to DB
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null || string.IsNullOrEmpty(room.CurrentVideoUrl)) return null;

        var dbAdjustedTime = room.VideoCurrentTime;
        if (room.IsVideoPlaying && room.VideoLastSyncAt.HasValue)
        {
            var elapsed = (DateTime.UtcNow - room.VideoLastSyncAt.Value).TotalSeconds;
            dbAdjustedTime += elapsed;
        }

        return new VideoSyncState
        {
            VideoUrl = room.CurrentVideoUrl,
            VideoType = room.CurrentVideoType,
            IsPlaying = room.IsVideoPlaying,
            CurrentTime = dbAdjustedTime,
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    private async Task<bool> CanControlVideo(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return false;

        var member = await _context.RoomMembers
            .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId.Value && m.IsActive);

        return member != null && member.Role >= RoomRole.Moderator;
    }

    private async Task UpdateRoomVideoState(Guid roomId, bool isPlaying, double currentTime)
    {
        var now = DateTime.UtcNow;
        
        // Update memory cache
        _roomStates[roomId] = new RoomVideoState
        {
            VideoUrl = _roomStates.TryGetValue(roomId, out var existing) ? existing.VideoUrl : null,
            VideoType = _roomStates.TryGetValue(roomId, out existing) ? existing.VideoType : null,
            IsPlaying = isPlaying,
            CurrentTime = currentTime,
            LastSyncAt = now
        };

        // Update DB (fire and forget, don't wait)
        var room = await _context.Rooms.FindAsync(roomId);
        if (room != null)
        {
            room.IsVideoPlaying = isPlaying;
            room.VideoCurrentTime = currentTime;
            room.VideoLastSyncAt = now;
            await _context.SaveChangesAsync();
        }
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }
}

public class VideoSyncState
{
    public string? VideoUrl { get; set; }
    public string? VideoType { get; set; }
    public bool IsPlaying { get; set; }
    public double CurrentTime { get; set; }
    public long ServerTimestamp { get; set; }
}

public class RoomVideoState
{
    public string? VideoUrl { get; set; }
    public string? VideoType { get; set; }
    public bool IsPlaying { get; set; }
    public double CurrentTime { get; set; }
    public DateTime LastSyncAt { get; set; }
}
