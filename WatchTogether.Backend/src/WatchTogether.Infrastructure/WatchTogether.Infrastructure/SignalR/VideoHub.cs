using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WatchTogether.Infrastructure.Data;

namespace WatchTogether.Infrastructure.SignalR;

public class VideoHub : Hub
{
    private readonly ApplicationDbContext _context;
    private static readonly Dictionary<Guid, VideoSyncState> _roomSyncStates = new();

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

        // Send current sync state to new user
        if (_roomSyncStates.TryGetValue(roomId, out var state))
        {
            await Clients.Caller.SendAsync("SyncState", state);
        }
    }

    public async Task LeaveVideoRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"video-{roomId}");
    }

    public async Task PlayVideo(Guid roomId, double currentTime)
    {
        if (!await CanControlVideo(roomId)) return;

        UpdateSyncState(roomId, true, currentTime);
        await Clients.Group($"video-{roomId}").SendAsync("VideoPlayed", new { CurrentTime = currentTime, Timestamp = DateTime.UtcNow });
    }

    public async Task PauseVideo(Guid roomId, double currentTime)
    {
        if (!await CanControlVideo(roomId)) return;

        UpdateSyncState(roomId, false, currentTime);
        await Clients.Group($"video-{roomId}").SendAsync("VideoPaused", new { CurrentTime = currentTime, Timestamp = DateTime.UtcNow });
    }

    public async Task SeekVideo(Guid roomId, double currentTime)
    {
        if (!await CanControlVideo(roomId)) return;

        UpdateSyncState(roomId, _roomSyncStates.TryGetValue(roomId, out var state) && state.IsPlaying, currentTime);
        await Clients.Group($"video-{roomId}").SendAsync("VideoSeeked", new { CurrentTime = currentTime, Timestamp = DateTime.UtcNow });
    }

    public async Task ChangeVideo(Guid roomId, Guid videoId)
    {
        if (!await CanControlVideo(roomId)) return;

        var playlist = await _context.Playlists.FirstOrDefaultAsync(p => p.RoomId == roomId);
        if (playlist != null)
        {
            playlist.CurrentVideoId = videoId;
            playlist.CurrentTime = 0;
            playlist.IsPlaying = false;
            playlist.LastSyncAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        _roomSyncStates[roomId] = new VideoSyncState
        {
            CurrentVideoId = videoId,
            IsPlaying = false,
            CurrentTime = 0,
            LastSyncAt = DateTime.UtcNow
        };

        await Clients.Group($"video-{roomId}").SendAsync("VideoChanged", new { VideoId = videoId });
    }

    public async Task RequestSync(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        if (_roomSyncStates.TryGetValue(roomId, out var state))
        {
            // Calculate adjusted time if playing
            var adjustedTime = state.CurrentTime;
            if (state.IsPlaying && state.LastSyncAt.HasValue)
            {
                var elapsed = (DateTime.UtcNow - state.LastSyncAt.Value).TotalSeconds;
                adjustedTime += elapsed;
            }

            await Clients.Caller.SendAsync("SyncState", new VideoSyncState
            {
                CurrentVideoId = state.CurrentVideoId,
                IsPlaying = state.IsPlaying,
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

        return member != null && member.Role >= Core.Entities.RoomRole.Member;
    }

    private void UpdateSyncState(Guid roomId, bool isPlaying, double currentTime)
    {
        _roomSyncStates[roomId] = new VideoSyncState
        {
            CurrentVideoId = _roomSyncStates.TryGetValue(roomId, out var existing) ? existing.CurrentVideoId : null,
            IsPlaying = isPlaying,
            CurrentTime = currentTime,
            LastSyncAt = DateTime.UtcNow
        };
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
    public Guid? CurrentVideoId { get; set; }
    public bool IsPlaying { get; set; }
    public double CurrentTime { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
