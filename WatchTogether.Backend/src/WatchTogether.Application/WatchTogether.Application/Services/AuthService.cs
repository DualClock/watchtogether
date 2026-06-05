using WatchTogether.Application.DTOs.Auth;
using WatchTogether.Core.Entities;
using WatchTogether.Core.Interfaces;

namespace WatchTogether.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;

    public AuthService(IUnitOfWork unitOfWork, IJwtService jwtService, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordService = passwordService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user exists
        if (await _unitOfWork.Users.ExistsAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("User with this email already exists");

        if (await _unitOfWork.Users.ExistsAsync(u => u.UserName == request.Username))
            throw new InvalidOperationException("User with this username already exists");

        // Validate password strength
        if (!_passwordService.IsPasswordStrong(request.Password))
            throw new InvalidOperationException("Password is not strong enough");

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            Email = request.Email,
            DisplayName = request.Username,
            PasswordHash = _passwordService.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email)
            .ContinueWith(t => t.Result.FirstOrDefault());

        if (user == null)
            throw new InvalidOperationException("Invalid email or password");

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid email or password");

        user.LastSeenAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshToken = await _unitOfWork.RefreshTokens.FindAsync(r => r.Token == request.RefreshToken)
            .ContinueWith(t => t.Result.FirstOrDefault());

        if (refreshToken == null || !refreshToken.IsActive)
            throw new InvalidOperationException("Invalid refresh token");

        var user = await _unitOfWork.Users.GetByIdAsync(refreshToken.UserId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        // Revoke old token
        refreshToken.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async Task RevokeTokenAsync(string refreshToken, Guid userId)
    {
        var token = await _unitOfWork.RefreshTokens.FindAsync(r => r.Token == refreshToken && r.UserId == userId)
            .ContinueWith(t => t.Result.FirstOrDefault());

        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task LogoutAsync(Guid userId)
    {
        var tokens = await _unitOfWork.RefreshTokens.FindAsync(r => r.UserId == userId && r.IsActive);
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.UserName);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status.ToString().ToLower()
            }
        };
    }
}
