using WatchTogether.Application.DTOs.User;

namespace WatchTogether.Application.Services;

public interface IUserService
{
    Task<UserProfileResponse?> GetProfileAsync(Guid userId);
    Task<UserProfileResponse?> GetProfileByUsernameAsync(string username);
    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<string?> UpdateAvatarAsync(Guid userId, Stream fileStream, string fileName);
    Task DeleteAccountAsync(Guid userId);
}
