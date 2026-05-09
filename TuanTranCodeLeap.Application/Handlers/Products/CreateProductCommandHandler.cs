using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TuanTranCodeLeap.Application.Common.Exceptions;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;
using TuanTranCodeLeap.Application.Queries.Products;

namespace TuanTranCodeLeap.Application.Handlers.Products;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(IProductRepository repository, IMemoryCache cache, ILogger<CreateProductCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating product with SKU: {SKU}", request.SKU);

            if (await _repository.ExistsBySkuAsync(request.SKU, cancellationToken))
            {
                _logger.LogWarning("Product creation failed: SKU already exists - {SKU}", request.SKU);
                throw new Exception("A product with this SKU already exists");
            }

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                SKU = request.SKU,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                IsActive = true,
                Created = DateTime.UtcNow
            };

            var createdProduct = await _repository.AddAsync(product, cancellationToken);

            // Invalidate search cache
            SearchProductsQueryHandler.InvalidateSearchCache(_cache);

            _logger.LogInformation("Product created successfully with ID: {ProductId}, SKU: {SKU}", createdProduct.Id, createdProduct.SKU);

            return new ProductDto
            {
                Id = createdProduct.Id,
                Name = createdProduct.Name,
                Description = createdProduct.Description,
                SKU = createdProduct.SKU,
                Price = createdProduct.Price,
                StockQuantity = createdProduct.StockQuantity,
                IsActive = createdProduct.IsActive,
                Created = createdProduct.Created,
                UpdatedAt = createdProduct.UpdatedAt,
                Deleted = createdProduct.Deleted,
                Guid = createdProduct.Guid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product with SKU: {SKU}", request.SKU);
            throw;
        }
    }
}
