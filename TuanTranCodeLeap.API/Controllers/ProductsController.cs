using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuanTranCodeLeap.API.Common;
using TuanTranCodeLeap.Application.Common.Exceptions;
using TuanTranCodeLeap.Application.Constants;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Application.Queries.Products;

namespace TuanTranCodeLeap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get list of all products had by pageNumber and pageSize
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10)</param>
    /// <returns>Paginated product list</returns>
    /// <response code="200">Products retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">Unauthorized - authentication required</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedProductsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedProductsResponse>>> GetProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest(ApiResponse<PagedProductsResponse>.Error(MessageConstants.PageNumberInvalid));
            }

            var query = new GetProductsQuery { PageNumber = pageNumber, PageSize = pageSize };
            var result = await _mediator.Send(query);
            return Ok(ApiResponse<PagedProductsResponse>.Success(result, MessageConstants.ProductsRetrieved));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedProductsResponse>.Error(string.Format(MessageConstants.ErrorOccurred, "retrieving products", ex.Message)));
        }
    }

    /// <summary>
    /// Get a specific product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    /// <response code="200">Product found</response>
    /// <response code="400">Invalid ID</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="404">Product not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponse<ProductDto>.Error(string.Format(MessageConstants.IdInvalid, "Product")));
            }

            var query = new GetProductByIdQuery { Id = id };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                return NotFound(ApiResponse<ProductDto>.Error(string.Format(MessageConstants.ProductNotFound, id)));
            }

            return Ok(ApiResponse<ProductDto>.Success(result, "Product retrieved successfully"));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<ProductDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ProductDto>.Error(string.Format(MessageConstants.ErrorOccurred, "retrieving the product", ex.Message)));
        }
    }

    /// <summary>
    /// Search products with filters
    /// </summary>
    /// <param name="name">Filter by product name (optional)</param>
    /// <param name="description">Filter by description (optional)</param>
    /// <param name="sku">Filter by SKU (optional)</param>
    /// <param name="minPrice">Minimum price filter (optional)</param>
    /// <param name="maxPrice">Maximum price filter (optional)</param>
    /// <param name="minStockQuantity">Minimum stock quantity filter (optional)</param>
    /// <param name="maxStockQuantity">Maximum stock quantity filter (optional)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10)</param>
    /// <returns>Filtered and paginated product list</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="401">Unauthorized - authentication required</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedProductsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedProductsResponse>>> SearchProducts(
        [FromQuery] string? name,
        [FromQuery] string? description,
        [FromQuery] string? sku,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minStockQuantity,
        [FromQuery] int? maxStockQuantity,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageNumber < 1 || pageSize < 1)
            {
                return BadRequest(ApiResponse<PagedProductsResponse>.Error(MessageConstants.PageNumberInvalid));
            }

            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                return BadRequest(ApiResponse<PagedProductsResponse>.Error(MessageConstants.PriceRangeInvalid));
            }

            if (minStockQuantity.HasValue && maxStockQuantity.HasValue && minStockQuantity > maxStockQuantity)
            {
                return BadRequest(ApiResponse<PagedProductsResponse>.Error(MessageConstants.StockRangeInvalid));
            }

            var query = new SearchProductsQuery
            {
                Name = name,
                Description = description,
                SKU = sku,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinStockQuantity = minStockQuantity,
                MaxStockQuantity = maxStockQuantity,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _mediator.Send(query);
            return Ok(ApiResponse<PagedProductsResponse>.Success(result, "Products searched successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedProductsResponse>.Error(string.Format(MessageConstants.ErrorOccurred, "searching products", ex.Message)));
        }
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="request">Product creation details</param>
    /// <returns>Created product</returns>
    /// <response code="201">Product created successfully</response>
    /// <response code="400">Invalid input or duplicate SKU</response>
    /// <response code="401">Unauthorized - authentication required</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] ProductCreateDto request)
    {
        try
        {
            var command = new CreateProductCommand
            {
                Name = request.Name,
                Description = request.Description,
                SKU = request.SKU,
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, ApiResponse<ProductDto>.Success(result, MessageConstants.ProductCreated));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProductDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ProductDto>.Error(string.Format(MessageConstants.ErrorOccurred, "creating the product", ex.Message)));
        }
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Product update details</param>
    /// <returns>Updated product</returns>
    /// <response code="200">Product updated successfully</response>
    /// <response code="400">Invalid input or duplicate SKU</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="404">Product not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] ProductUpdateDto request)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponse<ProductDto>.Error(string.Format(MessageConstants.IdInvalid, "Product")));
            }

            var command = new UpdateProductCommand
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                SKU = request.SKU,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                IsActive = request.IsActive
            };

            var result = await _mediator.Send(command);
            return Ok(ApiResponse<ProductDto>.Success(result, MessageConstants.ProductUpdated));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<ProductDto>.Error(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ProductDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<ProductDto>.Error(string.Format(MessageConstants.ErrorOccurred, "updating the product", ex.Message)));
        }
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>True if deleted</returns>
    /// <response code="200">Product deleted successfully</response>
    /// <response code="400">Invalid ID</response>
    /// <response code="401">Unauthorized - authentication required</response>
    /// <response code="404">Product not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(ApiResponse<bool>.Error(string.Format(MessageConstants.IdInvalid, "Product")));
            }

            var command = new DeleteProductCommand { Id = id };
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Success(result, MessageConstants.ProductDeleted));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.Error(string.Format(MessageConstants.ErrorOccurred, "deleting the product", ex.Message)));
        }
    }
}
