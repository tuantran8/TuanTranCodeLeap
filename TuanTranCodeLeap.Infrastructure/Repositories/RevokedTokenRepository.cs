using Microsoft.EntityFrameworkCore;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;
using TuanTranCodeLeap.Infrastructure.Data;

namespace TuanTranCodeLeap.Infrastructure.Repositories;

public class RevokedTokenRepository : IRevokedTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RevokedTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsRevokedAsync(string tokenJti)
    {
        return await _context.RevokedTokens
            .AnyAsync(rt => rt.TokenJti == tokenJti);
    }

    public async Task AddAsync(RevokedToken revokedToken)
    {
        await _context.RevokedTokens.AddAsync(revokedToken);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveExpiredTokensAsync()
    {
        var expiredTokens = await _context.RevokedTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.RevokedTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();
    }
}
