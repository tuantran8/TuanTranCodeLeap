using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text;
using TuanTranCodeLeap.Application.Constants;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;
using TuanTranCodeLeap.Application.Queries.Products;

namespace TuanTranCodeLeap.Application.Handlers.Products;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, PagedProductsResponse>
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly ILogger<SearchProductsQueryHandler> _logger;

    public SearchProductsQueryHandler(IProductRepository repository, IMemoryCache cache, ILogger<SearchProductsQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheConstants.SearchCacheAbsoluteExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(CacheConstants.SearchCacheSlidingExpirationMinutes)
        };
    }

    public async Task<PagedProductsResponse> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Searching products with parameters - Name: {Name}, SKU: {SKU}, Page: {Page}, PageSize: {PageSize}", 
                request.Name, request.SKU, request.PageNumber, request.PageSize);

            // Generate cache key based on search parameters
            var cacheKey = GenerateCacheKey(request);
            
            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out PagedProductsResponse? cachedResult))
            {
                _logger.LogDebug("Cache hit for search products");
                return cachedResult;
            }

            _logger.LogDebug("Cache miss, querying database for products");

            // If not in cache, query database
            var (products, totalCount) = await _repository.SearchAdvancedAsync(
                request.Name,
                request.Description,
                request.SKU,
                request.MinPrice,
                request.MaxPrice,
                request.MinStockQuantity,
                request.MaxStockQuantity,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var paginatedProducts = products.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    SKU = p.SKU,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    IsActive = p.IsActive,
                    Created = p.Created,
                    UpdatedAt = p.UpdatedAt,
                    Deleted = p.Deleted,
                    Guid = p.Guid
                })
                .ToList();

            var result = new PagedProductsResponse
            {
                Products = paginatedProducts,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };

            // Cache the result
            _cache.Set(cacheKey, result, _cacheOptions);

            _logger.LogInformation("Product search completed. Found {Count} products out of {Total} total", paginatedProducts.Count, totalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with parameters - Name: {Name}, SKU: {SKU}", request.Name, request.SKU);
            throw;
        }
    }

    private string GenerateCacheKey(SearchProductsQuery request)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(CacheConstants.SearchCacheKeyPrefix);

        if (!string.IsNullOrWhiteSpace(request.Name))
            keyBuilder.Append($"|name:{request.Name}");

        if (!string.IsNullOrWhiteSpace(request.Description))
            keyBuilder.Append($"|desc:{request.Description}");

        if (!string.IsNullOrWhiteSpace(request.SKU))
            keyBuilder.Append($"|sku:{request.SKU}");

        if (request.MinPrice.HasValue)
            keyBuilder.Append($"|minPrice:{request.MinPrice}");

        if (request.MaxPrice.HasValue)
            keyBuilder.Append($"|maxPrice:{request.MaxPrice}");

        if (request.MinStockQuantity.HasValue)
            keyBuilder.Append($"|minStock:{request.MinStockQuantity}");

        if (request.MaxStockQuantity.HasValue)
            keyBuilder.Append($"|maxStock:{request.MaxStockQuantity}");

        keyBuilder.Append($"|page:{request.PageNumber}");
        keyBuilder.Append($"|pageSize:{request.PageSize}");

        return keyBuilder.ToString();
    }

    public static void InvalidateSearchCache(IMemoryCache cache)
    {
        // Remove all search-related cache entries
        // Since memory cache doesn't support wildcard deletion, we'll clear version
        // In a real scenario, you might want to track cache keys
        var versionKey = CacheConstants.SearchCacheVersionKey;
        cache.Remove(versionKey);
    }
}
