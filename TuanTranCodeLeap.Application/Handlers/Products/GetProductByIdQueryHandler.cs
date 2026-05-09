using MediatR;
using Microsoft.Extensions.Logging;
using TuanTranCodeLeap.Application.Common.Exceptions;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;
using TuanTranCodeLeap.Application.Queries.Products;

namespace TuanTranCodeLeap.Application.Handlers.Products;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(IProductRepository repository, ILogger<GetProductByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving product with ID: {ProductId}", request.Id);

            var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
            
            if (product == null)
            {
                _logger.LogWarning("Product retrieval failed: Product not found - ID: {ProductId}", request.Id);
                throw new NotFoundException(nameof(Product), request.Id);
            }

            _logger.LogInformation("Product retrieved successfully - ID: {ProductId}, SKU: {SKU}", product.Id, product.SKU);

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                SKU = product.SKU,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive,
                Created = product.Created,
                UpdatedAt = product.UpdatedAt,
                Deleted = product.Deleted,
                Guid = product.Guid
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID: {ProductId}", request.Id);
            throw;
        }
    }
}
