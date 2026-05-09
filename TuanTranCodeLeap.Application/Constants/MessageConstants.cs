namespace TuanTranCodeLeap.Application.Constants;

public static class MessageConstants
{
    // Auth
    public const string LoginInvalid = "Invalid email or password";
    public const string LogoutSuccess = "Logged out successfully";
    public const string TokenRevoked = "Token has been revoked";
    public const string Unauthorized = "Unauthorized - Please provide a valid token";
    public const string InvalidUserIdentifier = "Invalid user identifier";

    // Products
    public const string ProductNotFound = "Product with ID {0} not found";
    public const string ProductCreated = "Product created successfully";
    public const string ProductUpdated = "Product updated successfully";
    public const string ProductDeleted = "Product deleted successfully";
    public const string ProductsRetrieved = "Products retrieved successfully";
    public const string ProductSkuExists = "A product with this SKU already exists";

    // Validation
    public const string PageNumberInvalid = "Page number and page size must be greater than 0";
    public const string PriceRangeInvalid = "Minimum price cannot be greater than maximum price";
    public const string StockRangeInvalid = "Minimum stock quantity cannot be greater than maximum stock quantity";
    public const string IdInvalid = "{0} ID must be greater than 0";

    // General
    public const string Success = "Success";
    public const string ErrorOccurred = "An error occurred while {0}: {1}";
    public const string ValidationFailed = "Validation failed";
}
