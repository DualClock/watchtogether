using WatchTogether.Application.DTOs.Auth;

namespace WatchTogether.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeTokenAsync(string refreshToken, Guid userId);
    Task LogoutAsync(Guid userId);
}
