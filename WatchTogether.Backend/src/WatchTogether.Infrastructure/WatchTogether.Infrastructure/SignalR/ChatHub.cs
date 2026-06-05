using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WatchTogether.Core.Entities;
using WatchTogether.Infrastructure.Data;

namespace WatchTogether.Infrastructure.SignalR;

public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private static readonly Dictionary<string, Guid> _userConnections = new();
    private static readonly Dictionary<Guid, HashSet<string>> _roomConnections = new();

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            _userConnections[Context.ConnectionId] = userId.Value;
            await UpdateUserStatus(userId.Value, UserStatus.Online);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_userConnections.TryGetValue(Context.ConnectionId, out var userId))
        {
            _userConnections.Remove(Context.ConnectionId);
            
            // Remove from all rooms
            foreach (var room in _roomConnections)
            {
                room.Value.Remove(Context.ConnectionId);
            }

            await UpdateUserStatus(userId, UserStatus.Offline);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        // Check if user is member
        var isMember = await _context.RoomMembers
            .AnyAsync(m => m.RoomId == roomId && m.UserId == userId.Value && m.IsActive);

        if (!isMember) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
        
        if (!_roomConnections.ContainsKey(roomId))
            _roomConnections[roomId] = new HashSet<string>();
        _roomConnections[roomId].Add(Context.ConnectionId);

        await UpdateUserStatus(userId.Value, UserStatus.InRoom);

        var user = await _context.Users.FindAsync(userId.Value);

        // Send current room members to the caller for participant list
        var members = await _context.RoomMembers
            .AsNoTracking()
            .Where(m => m.RoomId == roomId && m.IsActive)
            .Include(m => m.User)
            .Select(m => new
            {
                UserId = m.UserId,
                Username = m.User!.UserName,
                DisplayName = m.User.DisplayName,
                AvatarUrl = m.User.AvatarUrl,
                Role = m.Role.ToString()
            })
            .ToListAsync();

        await Clients.Caller.SendAsync("RoomMembers", new { Members = members });

        await Clients.Group(roomId.ToString()).SendAsync("UserJoined", new
        {
            UserId = userId.Value,
            Username = user?.UserName,
            DisplayName = user?.DisplayName,
            AvatarUrl = user?.AvatarUrl,
            JoinedAt = DateTime.UtcNow
        });
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
        
        if (_roomConnections.ContainsKey(roomId))
            _roomConnections[roomId].Remove(Context.ConnectionId);

        await UpdateUserStatus(userId.Value, UserStatus.Online);

        var user = await _context.Users.FindAsync(userId.Value);
        await Clients.Group(roomId.ToString()).SendAsync("UserLeft", new
        {
            UserId = userId.Value,
            Username = user?.UserName,
            DisplayName = user?.DisplayName,
            LeftAt = DateTime.UtcNow
        });
    }

    public async Task SendMessage(Guid roomId, string content, Guid? parentMessageId = null)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        // Check if user is member and not muted
        var member = await _context.RoomMembers
            .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId.Value && m.IsActive);

        if (member == null || member.IsMuted) return;

        var message = new Message
        {
            RoomId = roomId,
            UserId = userId.Value,
            Content = content,
            ParentMessageId = parentMessageId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId.Value);
        
        await Clients.Group(roomId.ToString()).SendAsync("NewMessage", new
        {
            Id = message.Id,
            RoomId = message.RoomId,
            UserId = message.UserId,
            Username = user?.UserName,
            AvatarUrl = user?.AvatarUrl,
            Content = message.Content,
            ParentMessageId = message.ParentMessageId,
            CreatedAt = message.CreatedAt,
            Reactions = new List<object>()
        });
    }

    public async Task AddReaction(Guid messageId, string emoji)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var message = await _context.Messages.FindAsync(messageId);
        if (message == null) return;

        var existing = await _context.Reactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId.Value && r.Emoji == emoji);

        if (existing != null) return;

        var reaction = new Reaction
        {
            MessageId = messageId,
            UserId = userId.Value,
            Emoji = emoji
        };

        _context.Reactions.Add(reaction);
        await _context.SaveChangesAsync();

        await Clients.Group(message.RoomId.ToString()).SendAsync("ReactionAdded", new
        {
            MessageId = messageId,
            ReactionId = reaction.Id,
            UserId = userId.Value,
            Emoji = emoji
        });
    }

    public async Task RemoveReaction(Guid messageId, string emoji)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var reaction = await _context.Reactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId.Value && r.Emoji == emoji);

        if (reaction == null) return;

        _context.Reactions.Remove(reaction);
        await _context.SaveChangesAsync();

        var message = await _context.Messages.FindAsync(messageId);
        if (message != null)
        {
            await Clients.Group(message.RoomId.ToString()).SendAsync("ReactionRemoved", new
            {
                MessageId = messageId,
                UserId = userId.Value,
                Emoji = emoji
            });
        }
    }

    public async Task Typing(Guid roomId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var user = await _context.Users.FindAsync(userId.Value);
        await Clients.Group(roomId.ToString()).SendAsync("UserTyping", new
        {
            UserId = userId.Value,
            Username = user?.UserName
        });
    }

    // ===== WebRTC Signaling =====

    public async Task SendWebRtcOffer(Guid roomId, Guid targetUserId, string sdp)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var caller = await _context.Users.FindAsync(userId.Value);
        await Clients.User(targetUserId.ToString()).SendAsync("ReceiveOffer", new
        {
            RoomId = roomId,
            FromUserId = userId.Value,
            FromUsername = caller?.UserName,
            Sdp = sdp
        });
    }

    public async Task SendWebRtcAnswer(Guid roomId, Guid targetUserId, string sdp)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        await Clients.User(targetUserId.ToString()).SendAsync("ReceiveAnswer", new
        {
            RoomId = roomId,
            FromUserId = userId.Value,
            Sdp = sdp
        });
    }

    public async Task SendIceCandidate(Guid roomId, Guid targetUserId, string candidate, string? sdpMid, int? sdpMLineIndex)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        await Clients.User(targetUserId.ToString()).SendAsync("ReceiveIceCandidate", new
        {
            RoomId = roomId,
            FromUserId = userId.Value,
            Candidate = candidate,
            SdpMid = sdpMid,
            SdpMLineIndex = sdpMLineIndex
        });
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }

    private async Task UpdateUserStatus(Guid userId, UserStatus status)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Status = status;
            user.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
