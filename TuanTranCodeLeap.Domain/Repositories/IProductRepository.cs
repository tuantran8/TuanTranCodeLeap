using TuanTranCodeLeap.Domain.Entities;

namespace TuanTranCodeLeap.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<(List<Product> Products, int TotalCount)> SearchAdvancedAsync(
        string? name,
        string? description,
        string? sku,
        decimal? minPrice,
        decimal? maxPrice,
        int? minStockQuantity,
        int? maxStockQuantity,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySkuAsync(string sku, int excludeId, CancellationToken cancellationToken = default);
}
