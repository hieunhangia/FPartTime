using Frontend.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages.Employer.Subscriptions;

[Authorize(Roles = Role.Employer)]
public class VNPAYPaymentResultModel : PageModel
{
    [BindProperty(SupportsGet = true, Name = "vnp_ResponseCode")]
    public string? VnpResponseCode { get; set; }

    [BindProperty(SupportsGet = true, Name = "vnp_TransactionStatus")]
    public string? VnpTransactionStatus { get; set; }

    [BindProperty(SupportsGet = true, Name = "vnp_TxnRef")]
    public string? VnpTxnRef { get; set; }

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        // Giao dịch thành công
        if (VnpResponseCode == "00" && (VnpTransactionStatus == null || VnpTransactionStatus == "00"))
        {
            return RedirectToPage("./Success");
        }

        // Người dùng chủ động hủy giao dịch trên cổng VNPAY
        if (VnpResponseCode == "24")
        {
            return RedirectToPage("./Cancel");
        }

        // Các trường hợp lỗi khác
        ErrorMessage = GetVnpayErrorMessage(VnpResponseCode);
        return Page();
    }

    private static string GetVnpayErrorMessage(string? code) => code switch
    {
        "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
        "09" => "Giao dịch không thành công do: Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
        "10" => "Giao dịch không thành công do: Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.",
        "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán.",
        "12" => "Giao dịch không thành công do: Thẻ/Tài khoản bị khóa.",
        "13" => "Giao dịch không thành công do: Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
        "51" => "Giao dịch không thành công do: Tài khoản không đủ số dư để thực hiện giao dịch.",
        "65" => "Giao dịch không thành công do: Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
        "75" => "Ngân hàng thanh toán đang bảo trì.",
        "79" => "Giao dịch không thành công do: Quý khách nhập sai mật khẩu thanh toán quá số lần quy định.",
        _ => $"Giao dịch không thành công. Mã phản hồi từ VNPAY: {code ?? "Không xác định"}."
    };
}
