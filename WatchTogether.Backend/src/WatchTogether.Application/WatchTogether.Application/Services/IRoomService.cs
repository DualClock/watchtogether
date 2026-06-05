using WatchTogether.Application.DTOs.Room;

namespace WatchTogether.Application.Services;

public interface IRoomService
{
    Task<RoomResponse> CreateRoomAsync(CreateRoomRequest request, Guid userId);
    Task<RoomDetailResponse?> GetRoomByIdAsync(Guid roomId);
    Task<IEnumerable<RoomResponse>> GetPublicRoomsAsync(int page = 1, int pageSize = 20);
    Task<IEnumerable<RoomResponse>> SearchRoomsAsync(string query, int page = 1, int pageSize = 20);
    Task<RoomDetailResponse?> JoinRoomAsync(Guid roomId, Guid userId, string? password = null);
    Task LeaveRoomAsync(Guid roomId, Guid userId);
    Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId);
    Task<bool> CanUserManageRoomAsync(Guid roomId, Guid userId);
    Task CloseRoomAsync(Guid roomId, Guid userId);
    Task KickUserAsync(Guid roomId, Guid targetUserId, Guid requestedById);
    Task BanUserAsync(Guid roomId, Guid targetUserId, Guid requestedById, string? reason = null, DateTime? expiresAt = null);
}
