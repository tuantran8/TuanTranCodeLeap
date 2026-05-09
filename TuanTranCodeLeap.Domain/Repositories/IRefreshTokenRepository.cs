using TuanTranCodeLeap.Domain.Entities;

namespace TuanTranCodeLeap.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(int userId);
    Task AddAsync(RefreshToken refreshToken);
    Task RemoveRangeAsync(IEnumerable<RefreshToken> refreshTokens);
    Task UpdateAsync(RefreshToken refreshToken);
}
