using Microsoft.Kiota.Abstractions;

namespace BackendApiClient;

public static class ApiExceptionHelper
{
    public static string ToFriendlyErrorMessage(this ApiException ex)
    {
        if (ex is ApiSdk.Models.ProblemDetails { Detail: not null } problem)
        {
            return problem.Detail;
        }

        return ex.ResponseStatusCode switch
        {
            400 => "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại thông tin và thử lại.",
            401 => "Bạn chưa được xác thực. Vui lòng đăng nhập và thử lại.",
            403 => "Bạn không có quyền truy cập vào tài nguyên này.",
            404 => "Không tìm thấy tài nguyên yêu cầu.",
            409 => "Yêu cầu không hợp lệ. Vui lòng kiểm tra lại thông tin và thử lại.",
            429 => "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
            500 => "Đã xảy ra lỗi máy chủ. Vui lòng thử lại sau.",
            _ => "Đã xảy ra lỗi. Vui lòng thử lại sau."
        };
    }
}