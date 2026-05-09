using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TuanTranCodeLeap.Application.Handlers.Products;
using TuanTranCodeLeap.Application.Queries.Products;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;

namespace TuanTranCodeLeap.Tests.UnitTests;

public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_DuplicateSku_ThrowsException()
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.ExistsBySkuAsync("DUPLICATE-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cacheMock = new Mock<IMemoryCache>();
        var loggerMock = new Mock<ILogger<CreateProductCommandHandler>>();

        var handler = new CreateProductCommandHandler(
            repositoryMock.Object,
            cacheMock.Object,
            loggerMock.Object);

        var command = new CreateProductCommand
        {
            Name = "Test Product",
            SKU = "DUPLICATE-SKU",
            Price = 99.99m,
            StockQuantity = 10
        };

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Message.Should().Be("A product with this SKU already exists");

        repositoryMock.Verify(
            r => r.ExistsBySkuAsync("DUPLICATE-SKU", It.IsAny<CancellationToken>()),
            Times.Once);

        repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProductDto()
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.ExistsBySkuAsync("NEW-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var createdProduct = new Product
        {
            Id = 1,
            Name = "New Product",
            SKU = "NEW-SKU",
            Price = 49.99m,
            StockQuantity = 5,
            IsActive = true,
            Created = DateTime.UtcNow,
            Guid = Guid.NewGuid()
        };

        repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProduct);

        object? cachedValue = null;
        var cacheMock = new Mock<IMemoryCache>();
        cacheMock
            .Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);
        cacheMock
            .Setup(c => c.Remove(It.IsAny<object>()));

        var loggerMock = new Mock<ILogger<CreateProductCommandHandler>>();

        var handler = new CreateProductCommandHandler(
            repositoryMock.Object,
            cacheMock.Object,
            loggerMock.Object);

        var command = new CreateProductCommand
        {
            Name = "New Product",
            SKU = "NEW-SKU",
            Price = 49.99m,
            StockQuantity = 5
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("New Product");
        result.SKU.Should().Be("NEW-SKU");
        result.Price.Should().Be(49.99m);
        result.StockQuantity.Should().Be(5);
        result.IsActive.Should().BeTrue();

        repositoryMock.Verify(
            r => r.ExistsBySkuAsync("NEW-SKU", It.IsAny<CancellationToken>()),
            Times.Once);

        repositoryMock.Verify(
            r => r.AddAsync(It.Is<Product>(p =>
                p.Name == "New Product" &&
                p.SKU == "NEW-SKU" &&
                p.Price == 49.99m &&
                p.StockQuantity == 5 &&
                p.IsActive == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
