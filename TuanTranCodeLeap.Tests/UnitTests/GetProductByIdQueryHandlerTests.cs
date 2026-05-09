using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using TuanTranCodeLeap.Application.Common.Exceptions;
using TuanTranCodeLeap.Application.Handlers.Products;
using TuanTranCodeLeap.Application.Queries.Products;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;

namespace TuanTranCodeLeap.Tests.UnitTests;

public class GetProductByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ProductExists_ReturnsProductDto()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            SKU = "SKU-1",
            Price = 99.99m,
            StockQuantity = 20,
            IsActive = true,
            Created = DateTime.UtcNow,
            Guid = Guid.NewGuid()
        };

        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var loggerMock = new Mock<ILogger<GetProductByIdQueryHandler>>();

        var handler = new GetProductByIdQueryHandler(repositoryMock.Object, loggerMock.Object);

        var query = new GetProductByIdQuery { Id = 1 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Product");
        result.SKU.Should().Be("SKU-1");

        repositoryMock.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var loggerMock = new Mock<ILogger<GetProductByIdQueryHandler>>();

        var handler = new GetProductByIdQueryHandler(repositoryMock.Object, loggerMock.Object);

        var query = new GetProductByIdQuery { Id = 999 };

        // Act
        var act = () => handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        repositoryMock.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }
}
