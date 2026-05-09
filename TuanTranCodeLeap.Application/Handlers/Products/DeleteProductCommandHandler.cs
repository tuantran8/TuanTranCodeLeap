using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TuanTranCodeLeap.Application.Common.Exceptions;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;
using TuanTranCodeLeap.Application.Queries.Products;

namespace TuanTranCodeLeap.Application.Handlers.Products;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(IProductRepository repository, IMemoryCache cache, ILogger<DeleteProductCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting product with ID: {ProductId}", request.Id);

            var product = await _repository.GetByIdAsync(request.Id, cancellationToken);
            
            if (product == null)
            {
                _logger.LogWarning("Product deletion failed: Product not found - ID: {ProductId}", request.Id);
                throw new NotFoundException(nameof(Product), request.Id);
            }

            await _repository.DeleteAsync(product, cancellationToken);

            // Invalidate search cache
            SearchProductsQueryHandler.InvalidateSearchCache(_cache);

            _logger.LogInformation("Product deleted successfully - ID: {ProductId}", request.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID: {ProductId}", request.Id);
            throw;
        }
    }
}
