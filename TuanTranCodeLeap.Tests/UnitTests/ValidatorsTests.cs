using FluentAssertions;
using Xunit;
using FluentValidation.TestHelper;
using TuanTranCodeLeap.Application.Auth.Commands;
using TuanTranCodeLeap.Application.Queries.Products;

namespace TuanTranCodeLeap.Tests.UnitTests;

public class ValidatorsTests
{
    #region CreateProductCommandValidator

    [Theory]
    [InlineData(null, "SKU-1", 10, 5, "Product name is required")]
    [InlineData("", "SKU-1", 10, 5, "Product name is required")]
    [InlineData("Valid Name", null, 10, 5, "SKU is required")]
    [InlineData("Valid Name", "", 10, 5, "SKU is required")]
    [InlineData("Valid Name", "SKU-1", 0, 5, "Price must be greater than 0")]
    [InlineData("Valid Name", "SKU-1", -1, 5, "Price must be greater than 0")]
    [InlineData("Valid Name", "SKU-1", 10, -1, "Stock quantity cannot be negative")]
    public void CreateProductCommandValidator_InvalidInput_HasValidationError(
        string? name, string? sku, decimal price, int stock, string expectedError)
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand
        {
            Name = name ?? string.Empty,
            SKU = sku ?? string.Empty,
            Price = price,
            StockQuantity = stock
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(expectedError);
    }

    [Fact]
    public void CreateProductCommandValidator_ValidInput_Passes()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand
        {
            Name = "Valid Product",
            SKU = "SKU-123",
            Price = 19.99m,
            StockQuantity = 100
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region UpdateProductCommandValidator

    [Theory]
    [InlineData(0, "Name", "SKU", 10, 5, "Product ID is required")]
    [InlineData(1, null, "SKU", 10, 5, "Product name is required")]
    [InlineData(1, "Name", null, 10, 5, "SKU is required")]
    [InlineData(1, "Name", "SKU", 0, 5, "Price must be greater than 0")]
    [InlineData(1, "Name", "SKU", 10, -1, "Stock quantity cannot be negative")]
    public void UpdateProductCommandValidator_InvalidInput_HasValidationError(
        int id, string? name, string? sku, decimal price, int stock, string expectedError)
    {
        // Arrange
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand
        {
            Id = id,
            Name = name ?? string.Empty,
            SKU = sku ?? string.Empty,
            Price = price,
            StockQuantity = stock
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(expectedError);
    }

    [Fact]
    public void UpdateProductCommandValidator_ValidInput_Passes()
    {
        // Arrange
        var validator = new UpdateProductCommandValidator();
        var command = new UpdateProductCommand
        {
            Id = 1,
            Name = "Updated Product",
            SKU = "SKU-UPD",
            Price = 29.99m,
            StockQuantity = 50
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region DeleteProductCommandValidator

    [Fact]
    public void DeleteProductCommandValidator_InvalidId_HasValidationError()
    {
        // Arrange
        var validator = new DeleteProductCommandValidator();
        var command = new DeleteProductCommand { Id = 0 };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void DeleteProductCommandValidator_ValidId_Passes()
    {
        // Arrange
        var validator = new DeleteProductCommandValidator();
        var command = new DeleteProductCommand { Id = 1 };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region GetProductByIdQueryValidator

    [Fact]
    public void GetProductByIdQueryValidator_InvalidId_HasValidationError()
    {
        // Arrange
        var validator = new GetProductByIdQueryValidator();
        var query = new GetProductByIdQuery { Id = 0 };

        // Act
        var result = validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void GetProductByIdQueryValidator_ValidId_Passes()
    {
        // Arrange
        var validator = new GetProductByIdQueryValidator();
        var query = new GetProductByIdQuery { Id = 1 };

        // Act
        var result = validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region RegisterCommandValidator

    [Theory]
    [InlineData(null, "Password1!", "First", "Last", "Email is required")]
    [InlineData("not-an-email", "Password1!", "First", "Last", "Email is not valid")]
    [InlineData("test@test.com", null, "First", "Last", "Password is required")]
    [InlineData("test@test.com", "12345", "First", "Last", "Password must be at least 6 characters")]
    [InlineData("test@test.com", "Password1!", null, "Last", "First name is required")]
    [InlineData("test@test.com", "Password1!", "First", null, "Last name is required")]
    public void RegisterCommandValidator_InvalidInput_HasValidationError(
        string? email, string? password, string? firstName, string? lastName, string expectedError)
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand
        {
            Email = email ?? string.Empty,
            Password = password ?? string.Empty,
            FirstName = firstName ?? string.Empty,
            LastName = lastName ?? string.Empty
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(expectedError);
    }

    [Fact]
    public void RegisterCommandValidator_ValidInput_Passes()
    {
        // Arrange
        var validator = new RegisterCommandValidator();
        var command = new RegisterCommand
        {
            Email = "user@example.com",
            Password = "SecureP@ss1",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region LoginCommandValidator

    [Theory]
    [InlineData(null, "password", "Email is required")]
    [InlineData("not-an-email", "password", "Email is not valid")]
    [InlineData("test@test.com", null, "Password is required")]
    public void LoginCommandValidator_InvalidInput_HasValidationError(
        string? email, string? password, string expectedError)
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand
        {
            Email = email ?? string.Empty,
            Password = password ?? string.Empty
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should().Contain(expectedError);
    }

    [Fact]
    public void LoginCommandValidator_ValidInput_Passes()
    {
        // Arrange
        var validator = new LoginCommandValidator();
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "password123"
        };

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
