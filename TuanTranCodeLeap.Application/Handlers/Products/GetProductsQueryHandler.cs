using MediatR;
using Microsoft.Extensions.Logging;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;
using TuanTranCodeLeap.Application.Queries.Products;

namespace TuanTranCodeLeap.Application.Handlers.Products;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedProductsResponse>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(IProductRepository repository, ILogger<GetProductsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedProductsResponse> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving products - Page: {Page}, PageSize: {PageSize}", request.PageNumber, request.PageSize);

            var products = await _repository.GetAllAsync(cancellationToken);
            
            var totalCount = products.Count;
            
            var paginatedProducts = products
                .OrderBy(p => p.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductDto
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

            _logger.LogInformation("Products retrieved successfully - Count: {Count}, Total: {Total}", paginatedProducts.Count, totalCount);

            return new PagedProductsResponse
            {
                Products = paginatedProducts,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products - Page: {Page}, PageSize: {PageSize}", request.PageNumber, request.PageSize);
            throw;
        }
    }
}
