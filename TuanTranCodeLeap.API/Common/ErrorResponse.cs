namespace TuanTranCodeLeap.API.Common;

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ErrorResponse(int statusCode, string message)
    {
        StatusCode = statusCode;
        Message = message;
    }

    public ErrorResponse(int statusCode, string message, List<string> errors)
    {
        StatusCode = statusCode;
        Message = message;
        Errors = errors;
    }

    public ErrorResponse(int statusCode, string message, IDictionary<string, string[]> validationErrors)
    {
        StatusCode = statusCode;
        Message = message;
        Errors = validationErrors.SelectMany(kvp => kvp.Value.Select(err => $"{kvp.Key}: {err}")).ToList();
    }
}

public class ValidationErrorResponse : ErrorResponse
{
    public Dictionary<string, List<string>> ValidationErrors { get; set; } = new();

    public ValidationErrorResponse(Dictionary<string, List<string>> validationErrors) 
        : base(400, "Validation failed")
    {
        ValidationErrors = validationErrors;
    }
}
