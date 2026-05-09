using MediatR;
using TuanTranCodeLeap.Application.DTOs;

namespace TuanTranCodeLeap.Application.Queries.Products;

public record SearchProductsQuery : IRequest<PagedProductsResponse>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? SKU { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinStockQuantity { get; set; }
    public int? MaxStockQuantity { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
