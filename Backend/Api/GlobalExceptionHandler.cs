using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OData;
using Domain.Shared.Exceptions.HttpErrorExceptions;

namespace Api;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            HttpErrorExceptionBase httpError => (
                httpError.StatusCode,
                httpError.Title,
                httpError.Message
            ),
            BadHttpRequestException badHttpRequest => (
                badHttpRequest.StatusCode,
                "Bad Request",
                badHttpRequest.Message
            ),
            ODataException oDataException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                oDataException.Message
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Bạn không có quyền truy cập vào tài nguyên này."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "Đã xảy ra lỗi nội bộ trên máy chủ. Vui lòng liên hệ hỗ trợ hoặc thử lại sau."
            )
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "[{StatusCode}] Internal Server Error tại {Path}", statusCode,
                httpContext.Request.Path);

            SentrySdk.CaptureException(exception, scope =>
            {
                if (httpContext.User.Identity?.IsAuthenticated != true) return;
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    scope.User = new SentryUser { Id = userId };
                }
            });
        }
        else if (httpContext.Request.Path.StartsWithSegments("/api/PayOSWebhook"))
        {
            logger.LogWarning(exception, "[{StatusCode}] Lỗi Webhook tại {Path}: {Message}", statusCode,
                httpContext.Request.Path, detail);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}