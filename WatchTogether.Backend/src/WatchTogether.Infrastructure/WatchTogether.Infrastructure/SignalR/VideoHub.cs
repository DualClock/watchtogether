using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WatchTogether.Core.Entities;
using WatchTogether.Infrastructure.Data;

namespace WatchTogether.Infrastructure.SignalR;

public class VideoHub : Hub
{
    private readonly ApplicationDbContext _context;

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

        // Send current video state to new user
        var room = await _context.Rooms.FindAsync(roomId);
        if (room != null && !string.IsNullOrEmpty(room.CurrentVideoUrl))
        {
            var adjustedTime = room.VideoCurrentTime;
            if (room.IsVideoPlaying && room.VideoLastSyncAt.HasValue)
            {
                var elapsed = (DateTime.UtcNow - room.VideoLastSyncAt.Value).TotalSeconds;
                adjustedTime += elapsed;
            }

            await Clients.Caller.SendAsync("SyncState", new VideoSyncState
            {
                VideoUrl = room.CurrentVideoUrl,
                VideoType = room.CurrentVideoType,
                IsPlaying = room.IsVideoPlaying,
                CurrentTime = adjustedTime,
                LastSyncAt = DateTime.UtcNow
            });
        }
    }

    public async Task LeaveVideoRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"video-{roomId}");
    }

    public async Task PlayVideo(Guid roomId, double currentTime)
    {
        if (!await CanControlVideo(roomId)) return;

        await UpdateRoomVideoState(roomId, true, currentTime);
        await Clients.Group($"video-{roomId}").SendAsync("VideoPlayed", new { CurrentTime = currentTime });
    }

    public async Task PauseVideo(Guid roomId, double currentTime)
    {
        if (!await CanControlVideo(roomId)) return;

        await UpdateRoomVideoState(roomId, false, currentTime);
        await Clients.Group($"video-{roomId}").SendAsync("VideoPaused", new { CurrentTime = currentTime });
    }

    public async Task SeekVideo(Guid roomId, double currentTime)
    {
        if (!await CanControlVideo(roomId)) return;

        var room = await _context.Rooms.FindAsync(roomId);
        if (room != null)
        {
            room.VideoCurrentTime = currentTime;
            room.VideoLastSyncAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        await Clients.Group($"video-{roomId}").SendAsync("VideoSeeked", new { CurrentTime = currentTime });
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

        await Clients.Group($"video-{roomId}").SendAsync("VideoChanged", new { VideoUrl = videoUrl, VideoType = videoType });
    }

    public async Task RequestSync(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var room = await _context.Rooms.FindAsync(roomId);
        if (room != null && !string.IsNullOrEmpty(room.CurrentVideoUrl))
        {
            var adjustedTime = room.VideoCurrentTime;
            if (room.IsVideoPlaying && room.VideoLastSyncAt.HasValue)
            {
                var elapsed = (DateTime.UtcNow - room.VideoLastSyncAt.Value).TotalSeconds;
                adjustedTime += elapsed;
            }

            await Clients.Caller.SendAsync("SyncState", new VideoSyncState
            {
                VideoUrl = room.CurrentVideoUrl,
                VideoType = room.CurrentVideoType,
                IsPlaying = room.IsVideoPlaying,
                CurrentTime = adjustedTime,
                LastSyncAt = DateTime.UtcNow
            });
        }
    }

    private async Task<bool> CanControlVideo(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return false;

        var member = await _context.RoomMembers
            .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId.Value && m.IsActive);

        // Owner and Moderators can control video
        return member != null && member.Role >= RoomRole.Moderator;
    }

    private async Task UpdateRoomVideoState(Guid roomId, bool isPlaying, double currentTime)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room != null)
        {
            room.IsVideoPlaying = isPlaying;
            room.VideoCurrentTime = currentTime;
            room.VideoLastSyncAt = DateTime.UtcNow;
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
    public DateTime? LastSyncAt { get; set; }
}
