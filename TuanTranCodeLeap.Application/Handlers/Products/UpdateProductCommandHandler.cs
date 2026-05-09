using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TuanTranCodeLeap.Application.Common.Exceptions;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;
using TuanTranCodeLeap.Application.Queries.Products;

namespace TuanTranCodeLeap.Application.Handlers.Products;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(IProductRepository repository, IMemoryCache cache, ILogger<UpdateProductCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating product with ID: {ProductId}, SKU: {SKU}", request.Id, request.SKU);

            var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
            
            if (product == null)
            {
                _logger.LogWarning("Product update failed: Product not found - ID: {ProductId}", request.Id);
                throw new NotFoundException(nameof(Product), request.Id);
            }

            // Check if SKU conflicts with another product
            if (await _repository.ExistsBySkuAsync(request.SKU, request.Id, cancellationToken))
            {
                _logger.LogWarning("Product update failed: SKU already exists - SKU: {SKU}, ProductId: {ProductId}", request.SKU, request.Id);
                throw new Exception("A product with this SKU already exists");
            }

            product.Name = request.Name;
            product.Description = request.Description;
            product.SKU = request.SKU;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.IsActive = request.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            var updatedProduct = await _repository.UpdateAsync(product, cancellationToken);

            // Invalidate search cache
            SearchProductsQueryHandler.InvalidateSearchCache(_cache);

            _logger.LogInformation("Product updated successfully - ID: {ProductId}, SKU: {SKU}", updatedProduct.Id, updatedProduct.SKU);

            return new ProductDto
            {
                Id = updatedProduct.Id,
                Name = updatedProduct.Name,
                Description = updatedProduct.Description,
                SKU = updatedProduct.SKU,
                Price = updatedProduct.Price,
                StockQuantity = updatedProduct.StockQuantity,
                IsActive = updatedProduct.IsActive,
                Created = updatedProduct.Created,
                UpdatedAt = updatedProduct.UpdatedAt,
                Deleted = updatedProduct.Deleted,
                Guid = updatedProduct.Guid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID: {ProductId}", request.Id);
            throw;
        }
    }
}
