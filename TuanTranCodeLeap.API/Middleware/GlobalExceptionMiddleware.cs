using System.Net;
using System.Text.Json;
using TuanTranCodeLeap.API.Common;
using TuanTranCodeLeap.Application.Common.Exceptions;

namespace TuanTranCodeLeap.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        ErrorResponse response;

        switch (exception)
        {
            case ValidationException validationEx:
                response = new ErrorResponse(400, "Validation failed", validationEx.Errors);
                break;
            
            case NotFoundException notFoundEx:
                response = new ErrorResponse(404, notFoundEx.Message);
                break;
            
            case ArgumentException argEx:
                response = new ErrorResponse(400, argEx.Message);
                break;
            
            default:
                response = new ErrorResponse(500, "An internal server error occurred. Please try again later.");
                break;
        }

        context.Response.StatusCode = response.StatusCode;
        
        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}
