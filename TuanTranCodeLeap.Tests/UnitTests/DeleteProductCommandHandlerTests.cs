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

public class DeleteProductCommandHandlerTests
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
        var loggerMock = new Mock<ILogger<DeleteProductCommandHandler>>();

        var handler = new DeleteProductCommandHandler(repositoryMock.Object, cacheMock.Object, loggerMock.Object);

        var command = new DeleteProductCommand { Id = 999 };

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        repositoryMock.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "To Delete",
            SKU = "DEL-1",
            Price = 10m,
            StockQuantity = 5,
            IsActive = true,
            Created = DateTime.UtcNow,
            Guid = Guid.NewGuid()
        };

        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        repositoryMock
            .Setup(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cacheMock = new Mock<IMemoryCache>();
        cacheMock.Setup(c => c.Remove(It.IsAny<object>()));

        var loggerMock = new Mock<ILogger<DeleteProductCommandHandler>>();

        var handler = new DeleteProductCommandHandler(repositoryMock.Object, cacheMock.Object, loggerMock.Object);

        var command = new DeleteProductCommand { Id = 1 };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        repositoryMock.Verify(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }
}
