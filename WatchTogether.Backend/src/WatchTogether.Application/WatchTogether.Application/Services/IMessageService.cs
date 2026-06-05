using WatchTogether.Application.DTOs.Message;

namespace WatchTogether.Application.Services;

public interface IMessageService
{
    Task<MessageResponse> SendMessageAsync(Guid roomId, Guid userId, SendMessageRequest request);
    Task<IEnumerable<MessageResponse>> GetMessagesAsync(Guid roomId, int page = 1, int pageSize = 50);
    Task<MessageResponse?> GetMessageByIdAsync(Guid messageId);
    Task<MessageResponse?> EditMessageAsync(Guid messageId, Guid userId, string newContent);
    Task DeleteMessageAsync(Guid messageId, Guid userId);
    Task AddReactionAsync(Guid messageId, Guid userId, string emoji);
    Task RemoveReactionAsync(Guid messageId, Guid userId, string emoji);
}
