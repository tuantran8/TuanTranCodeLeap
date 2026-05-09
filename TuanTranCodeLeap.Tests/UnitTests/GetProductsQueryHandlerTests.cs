using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using TuanTranCodeLeap.Application.Handlers.Products;
using TuanTranCodeLeap.Application.Queries.Products;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;

namespace TuanTranCodeLeap.Tests.UnitTests;

public class GetProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedResponse()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Alpha", SKU = "SKU-1", Price = 10m, StockQuantity = 5, IsActive = true, Created = DateTime.UtcNow, Guid = Guid.NewGuid() },
            new() { Id = 2, Name = "Beta", SKU = "SKU-2", Price = 20m, StockQuantity = 10, IsActive = true, Created = DateTime.UtcNow, Guid = Guid.NewGuid() },
            new() { Id = 3, Name = "Gamma", SKU = "SKU-3", Price = 30m, StockQuantity = 15, IsActive = true, Created = DateTime.UtcNow, Guid = Guid.NewGuid() }
        };

        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var loggerMock = new Mock<ILogger<GetProductsQueryHandler>>();

        var handler = new GetProductsQueryHandler(repositoryMock.Object, loggerMock.Object);

        var query = new GetProductsQuery { PageNumber = 1, PageSize = 2 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(2);
        result.Products.Should().HaveCount(2);
        result.Products[0].Name.Should().Be("Alpha");
        result.Products[1].Name.Should().Be("Beta");

        repositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SecondPage_ReturnsRemainingItems()
    {
        // Arrange
        var products = Enumerable.Range(1, 5)
            .Select(i => new Product
            {
                Id = i,
                Name = $"Product {i}",
                SKU = $"SKU-{i}",
                Price = i * 10m,
                StockQuantity = i,
                IsActive = true,
                Created = DateTime.UtcNow,
                Guid = Guid.NewGuid()
            })
            .ToList();

        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var loggerMock = new Mock<ILogger<GetProductsQueryHandler>>();

        var handler = new GetProductsQueryHandler(repositoryMock.Object, loggerMock.Object);

        var query = new GetProductsQuery { PageNumber = 2, PageSize = 3 };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Products.Should().HaveCount(2);
        result.TotalPages.Should().Be(2);
    }
}
