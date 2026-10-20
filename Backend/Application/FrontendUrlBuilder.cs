using Contracts.Application;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.Extensions.Configuration;

namespace Application;

public class FrontendUrlBuilder(IConfiguration configuration) : IFrontendUrlBuilder
{
    private readonly string _baseUrl = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ??
                                       throw new InvalidOperationException(
                                           "Cấu hình Frontend:BaseUrl chưa được thiết lập.");

    public string BuildUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new BadRequestException("Đường dẫn chuyển hướng không được để trống.");
        }

        var cleanPath = relativePath.Trim();

        if (!cleanPath.StartsWith('/') || cleanPath.StartsWith("//") || cleanPath.StartsWith("/\\"))
        {
            throw new BadRequestException(
                "Đường dẫn chuyển hướng không hợp lệ. Phải là đường dẫn tương đối bắt đầu bằng '/'.");
        }

        return $"{_baseUrl}{cleanPath}";
    }
}