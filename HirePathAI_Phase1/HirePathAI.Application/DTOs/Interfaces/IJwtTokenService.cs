namespace HirePathAI.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(
        int userId,
        string fullName,
        string email,
        IEnumerable<string> roles,
        out DateTime expiresAt);
}