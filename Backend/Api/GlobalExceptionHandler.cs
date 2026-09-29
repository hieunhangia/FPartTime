using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Service.HttpErrorExceptions;

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