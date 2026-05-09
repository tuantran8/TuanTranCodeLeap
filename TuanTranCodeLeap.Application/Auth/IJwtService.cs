using TuanTranCodeLeap.Domain.Entities;

namespace TuanTranCodeLeap.Application.Auth;

public interface IJwtService
{
    Task<(string token, DateTime expiresAt)> GenerateTokenAsync(User user);
}
