namespace WatchTogether.Core.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string username);
    string GenerateRefreshToken();
    Guid? ValidateToken(string token);
}
