using System.Net;
using System.Text.Json;

namespace SaleApp.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Handle Unauthorized (401) responses
            if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized && !context.Response.HasStarted)
            {
                await HandleUnauthorizedAsync(context);
            }
            // Handle Forbidden (403) responses
            else if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden && !context.Response.HasStarted)
            {
                await HandleForbiddenAsync(context);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            if (!context.Response.HasStarted)
            {
                await HandleUnauthorizedAsync(context, ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred");
            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
    }

    private static Task HandleUnauthorizedAsync(HttpContext context, string? message = null)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        var response = new
        {
            success = false,
            message = message ?? "Unauthorized: Missing or invalid Bearer token. Please login first.",
            statusCode = context.Response.StatusCode
        };

        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

        var response = new
        {
            success = false,
            message = "Forbidden: You do not have permission to access this resource.",
            statusCode = context.Response.StatusCode
        };

        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            success = false,
            message = "Internal server error. Please try again later.",
            statusCode = context.Response.StatusCode,
            details = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
                ? exception.Message
                : null
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
