namespace TuanTranCodeLeap.API.Common;

public class ApiResponse<T>
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Status = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Error(string message, T? data = default)
    {
        return new ApiResponse<T>
        {
            Status = false,
            Message = message,
            Data = data
        };
    }
}

public class ApiResponse
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResponse Success(string message = "Success")
    {
        return new ApiResponse
        {
            Status = true,
            Message = message
        };
    }

    public static ApiResponse Error(string message)
    {
        return new ApiResponse
        {
            Status = false,
            Message = message
        };
    }
}
