using Frontend.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages.Employer.Subscriptions;

[Authorize(Roles = Role.Employer)]
public class ZaloPayPaymentResultModel : PageModel
{
    [BindProperty(SupportsGet = true, Name = "appid")]
    public string? AppId { get; set; }

    [BindProperty(SupportsGet = true, Name = "apptransid")]
    public string? AppTransId { get; set; }

    [BindProperty(SupportsGet = true, Name = "pmcid")]
    public string? PmcId { get; set; }

    [BindProperty(SupportsGet = true, Name = "bankcode")]
    public string? BankCode { get; set; }

    [BindProperty(SupportsGet = true, Name = "amount")]
    public long? Amount { get; set; }

    [BindProperty(SupportsGet = true, Name = "discountamount")]
    public long? DiscountAmount { get; set; }

    [BindProperty(SupportsGet = true, Name = "status")]
    public int? Status { get; set; }

    [BindProperty(SupportsGet = true, Name = "checksum")]
    public string? Checksum { get; set; }

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        // Giao dịch thành công trên ZaloPay
        if (Status == 1)
        {
            return RedirectToPage("./Success");
        }

        // Người dùng chủ động hủy giao dịch trên cổng ZaloPay
        if (Status == -49)
        {
            return RedirectToPage("./Cancel");
        }

        // Các trường hợp lỗi khác
        ErrorMessage = GetZaloPayErrorMessage(Status);
        return Page();
    }

    private static string GetZaloPayErrorMessage(int? status) => status switch
    {
        1 => "Giao dịch thành công.",
        2 => "Giao dịch thất bại.",
        -49 => "Người dùng đã chủ động hủy giao dịch trên cổng ZaloPay.",
        -54 => "Giao dịch không thành công: Đơn hàng đã hết hạn thanh toán.",
        -63 => "Giao dịch không thành công: Số dư tài khoản hoặc ví ZaloPay không đủ.",
        -217 => "Giao dịch không thành công do lỗi từ phía ngân hàng liên kết.",
        _ => $"Giao dịch không thành công. Mã trạng thái từ ZaloPay: {status?.ToString() ?? "Không xác định"}."
    };
}
