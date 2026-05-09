using TuanTranCodeLeap.Domain.Entities;

namespace TuanTranCodeLeap.Domain.Repositories;

public interface IRevokedTokenRepository
{
    Task<bool> IsRevokedAsync(string tokenJti);
    Task AddAsync(RevokedToken revokedToken);
    Task RemoveExpiredTokensAsync();
}
