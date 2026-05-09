using Microsoft.EntityFrameworkCore;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;
using TuanTranCodeLeap.Infrastructure.Data;

namespace TuanTranCodeLeap.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => p.Name.Contains(searchTerm));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<(List<Product> Products, int TotalCount)> SearchAdvancedAsync(
        string? name,
        string? description,
        string? sku,
        decimal? minPrice,
        decimal? maxPrice,
        int? minStockQuantity,
        int? maxStockQuantity,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(p => p.Name.Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            query = query.Where(p => p.Description != null && p.Description.Contains(description));
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            query = query.Where(p => p.SKU.Contains(sku));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        if (minStockQuantity.HasValue)
        {
            query = query.Where(p => p.StockQuantity >= minStockQuantity.Value);
        }

        if (maxStockQuantity.HasValue)
        {
            query = query.Where(p => p.StockQuantity <= maxStockQuantity.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var products = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (products, totalCount);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task DeleteAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.SKU == sku, cancellationToken);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, int excludeId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.SKU == sku && p.Id != excludeId, cancellationToken);
    }
}
