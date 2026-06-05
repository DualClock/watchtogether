using Microsoft.AspNetCore.Http;
using WatchTogether.Application.DTOs.User;
using WatchTogether.Core.Interfaces;

namespace WatchTogether.Application.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _uploadPath;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
        
        if (!Directory.Exists(_uploadPath))
            Directory.CreateDirectory(_uploadPath);
    }

    public async Task<UserProfileResponse?> GetProfileAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return null;

        return MapToProfileResponse(user);
    }

    public async Task<UserProfileResponse?> GetProfileByUsernameAsync(string username)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.UserName == username);
        var user = users.FirstOrDefault();
        if (user == null) return null;

        return MapToProfileResponse(user);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            if (request.DisplayName.Length > 50)
                throw new InvalidOperationException("Display name must be at most 50 characters");
            user.DisplayName = request.DisplayName;
        }

        if (request.Bio != null)
        {
            if (request.Bio.Length > 500)
                throw new InvalidOperationException("Bio must be at most 500 characters");
            user.Bio = request.Bio;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return MapToProfileResponse(user);
    }

    public async Task<string?> UpdateAvatarAsync(Guid userId, Stream fileStream, string fileName)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return null;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        
        if (!allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Invalid file type. Allowed: jpg, jpeg, png, gif, webp");

        // Delete old avatar
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldPath = Path.Combine(_uploadPath, Path.GetFileName(user.AvatarUrl));
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        }

        // Save new avatar
        var newFileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadPath, newFileName);

        using (var file = File.Create(filePath))
        {
            await fileStream.CopyToAsync(file);
        }

        user.AvatarUrl = $"/avatars/{newFileName}";
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return user.AvatarUrl;
    }

    public async Task DeleteAccountAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.Email = $"deleted_{userId}@deleted.com";
        user.UserName = $"deleted_{userId}";
        
        await _unitOfWork.SaveChangesAsync();
    }

    private UserProfileResponse MapToProfileResponse(Core.Entities.User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Username = user.UserName,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status.ToString().ToLower(),
            CreatedAt = user.CreatedAt,
            LastSeenAt = user.LastSeenAt
        };
    }
}
