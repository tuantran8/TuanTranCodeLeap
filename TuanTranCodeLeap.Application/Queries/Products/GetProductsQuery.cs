using MediatR;
using TuanTranCodeLeap.Application.DTOs;

namespace TuanTranCodeLeap.Application.Queries.Products;

public record GetProductsQuery : IRequest<PagedProductsResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
