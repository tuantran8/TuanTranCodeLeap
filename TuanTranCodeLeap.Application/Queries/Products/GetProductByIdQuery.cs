using FluentValidation;
using MediatR;
using TuanTranCodeLeap.Application.DTOs;

namespace TuanTranCodeLeap.Application.Queries.Products;

public record GetProductByIdQuery : IRequest<ProductDto>
{
    public int Id { get; set; }
}

public class GetProductByIdQueryValidator : FluentValidation.AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Product ID is required");
    }
}
