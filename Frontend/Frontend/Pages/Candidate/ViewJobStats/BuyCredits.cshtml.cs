using System.ComponentModel.DataAnnotations;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Candidate.ViewJobStats;

[Authorize(Roles = Role.Candidate)]
public class BuyCreditsModel(ApiClient apiClient, ILogger<BuyCreditsModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public int CurrentBalance { get; private set; }

    public List<CandidateViewJobStatsPackageResponseDto> Packages { get; private set; } = [];

    public class CheckoutInput
    {
        [Required(ErrorMessage = "Vui lòng chọn gói lượt xem.")]
        public Guid? PackageId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; } = "PayOS";

        public string? ReturnUrl { get; set; }
    }

    [BindProperty]
    public CheckoutInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Input.ReturnUrl = ReturnUrl;
        await LoadDataAsync();

        if (Packages.Count > 0 && Input.PackageId == null)
        {
            Input.PackageId = Packages[0].Id;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData.SetErrorMessage("Dữ liệu thanh toán không hợp lệ.");
            await LoadDataAsync();
            return Page();
        }

        try
        {
            var returnUrlParam = !string.IsNullOrWhiteSpace(Input.ReturnUrl) ? Input.ReturnUrl : null;

            if (Input.PaymentMethod == "PayOS")
            {
                var successPath = string.IsNullOrWhiteSpace(returnUrlParam)
                    ? "/Candidate/ViewJobStats/Success"
                    : $"/Candidate/ViewJobStats/Success?returnUrl={Uri.EscapeDataString(returnUrlParam)}";

                var cancelPath = string.IsNullOrWhiteSpace(returnUrlParam)
                    ? "/Candidate/ViewJobStats/Cancel"
                    : $"/Candidate/ViewJobStats/Cancel?returnUrl={Uri.EscapeDataString(returnUrlParam)}";

                var request = new CreateCandidateViewPayOSCheckoutRequestDto
                {
                    PackageId = Input.PackageId,
                    ReturnPath = successPath,
                    CancelPath = cancelPath
                };

                var response = await apiClient.Api.Candidate.ViewJobStats.Orders.PayosCheckout.PostAsync(request);
                if (!string.IsNullOrEmpty(response?.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
                }

                TempData.SetErrorMessage("Không lấy được đường dẫn thanh toán từ PayOS.");
                return RedirectToPage(new { returnUrl = Input.ReturnUrl });
            }

            if (Input.PaymentMethod == "VNPAY")
            {
                var vnpayReturn = string.IsNullOrWhiteSpace(returnUrlParam)
                    ? "/Candidate/ViewJobStats/VNPAYPaymentResult"
                    : $"/Candidate/ViewJobStats/VNPAYPaymentResult?returnUrl={Uri.EscapeDataString(returnUrlParam)}";

                var request = new CreateCandidateViewVNPAYCheckoutRequestDto
                {
                    PackageId = Input.PackageId,
                    ReturnPath = vnpayReturn
                };

                var response = await apiClient.Api.Candidate.ViewJobStats.Orders.VnpayCheckout.PostAsync(request);
                if (!string.IsNullOrEmpty(response?.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
                }

                TempData.SetErrorMessage("Không lấy được đường dẫn thanh toán từ VNPAY.");
                return RedirectToPage(new { returnUrl = Input.ReturnUrl });
            }

            if (Input.PaymentMethod == "ZaloPay")
            {
                var zaloReturn = string.IsNullOrWhiteSpace(returnUrlParam)
                    ? "/Candidate/ViewJobStats/ZaloPayPaymentResult"
                    : $"/Candidate/ViewJobStats/ZaloPayPaymentResult?returnUrl={Uri.EscapeDataString(returnUrlParam)}";

                var request = new CreateCandidateViewZaloPayCheckoutRequestDto
                {
                    PackageId = Input.PackageId,
                    ReturnPath = zaloReturn
                };

                var response = await apiClient.Api.Candidate.ViewJobStats.Orders.ZalopayCheckout.PostAsync(request);
                if (!string.IsNullOrEmpty(response?.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
                }

                TempData.SetErrorMessage("Không lấy được đường dẫn thanh toán từ ZaloPay.");
                return RedirectToPage(new { returnUrl = Input.ReturnUrl });
            }

            TempData.SetErrorMessage("Phương thức thanh toán không được hỗ trợ.");
            return RedirectToPage(new { returnUrl = Input.ReturnUrl });
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "Lỗi từ Backend API khi tạo đơn mua lượt xem thống kê.");
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage(new { returnUrl = Input.ReturnUrl });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi hệ thống khi tạo yêu cầu thanh toán mua lượt xem.");
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tạo yêu cầu thanh toán. Vui lòng thử lại sau.");
            return RedirectToPage(new { returnUrl = Input.ReturnUrl });
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var balanceDto = await apiClient.Api.Candidate.ViewJobStats.Balance.GetAsync();
            CurrentBalance = balanceDto?.Balance ?? 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể tải số dư lượt xem thống kê.");
            CurrentBalance = 0;
        }

        try
        {
            Packages = await apiClient.Api.Candidate.ViewJobStats.Packages.GetAsync() ?? [];
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi tải danh sách gói lượt xem.");
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách gói dịch vụ.");
        }
    }
}
