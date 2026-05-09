using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TuanTranCodeLeap.Application.Common.Exceptions;
using TuanTranCodeLeap.Application.Handlers.Products;
using TuanTranCodeLeap.Application.Queries.Products;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;

namespace TuanTranCodeLeap.Tests.UnitTests;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var cacheMock = new Mock<IMemoryCache>();
        var loggerMock = new Mock<ILogger<UpdateProductCommandHandler>>();

        var handler = new UpdateProductCommandHandler(repositoryMock.Object, cacheMock.Object, loggerMock.Object);

        var command = new UpdateProductCommand
        {
            Id = 999,
            Name = "Updated",
            SKU = "SKU-NEW",
            Price = 10m,
            StockQuantity = 5,
            IsActive = true
        };

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        repositoryMock.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateSku_ThrowsException()
    {
        // Arrange
        var existingProduct = new Product
        {
            Id = 1,
            Name = "Existing",
            SKU = "OLD-SKU",
            Price = 20m,
            StockQuantity = 10,
            IsActive = true,
            Created = DateTime.UtcNow,
            Guid = Guid.NewGuid()
        };

        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        repositoryMock
            .Setup(r => r.ExistsBySkuAsync("DUPLICATE-SKU", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cacheMock = new Mock<IMemoryCache>();
        var loggerMock = new Mock<ILogger<UpdateProductCommandHandler>>();

        var handler = new UpdateProductCommandHandler(repositoryMock.Object, cacheMock.Object, loggerMock.Object);

        var command = new UpdateProductCommand
        {
            Id = 1,
            Name = "Updated",
            SKU = "DUPLICATE-SKU",
            Price = 30m,
            StockQuantity = 15,
            IsActive = true
        };

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<Exception>();
        exception.Which.Message.Should().Be("A product with this SKU already exists");
    }
}
