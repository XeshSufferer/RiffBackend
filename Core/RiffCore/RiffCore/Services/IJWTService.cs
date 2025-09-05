namespace RiffCore.Services;

public interface IJWTService
{
    string GenerateToken(string id, TimeSpan? lifetime = null);
    bool ValidateToken(string token);
}