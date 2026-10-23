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

namespace Frontend.Pages.Employer.Subscriptions;

[Authorize(Roles = Role.Employer)]
public class IndexModel(ApiClient apiClient, ILogger<IndexModel> logger) : PageModel
{
    public List<EmployerSubscriptionPlanResponseDto> Plans { get; private set; } = [];

    public CurrentEmployerSubscriptionResponseDto? CurrentSubscription { get; private set; }

    public bool HasActiveSubscription => CurrentSubscription?.HasActiveSubscription ?? false;

    public class CheckoutInput
    {
        [Required(ErrorMessage = "Vui lòng chọn gói dịch vụ.")]
        public EmployerSubscriptionTier Tier { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kỳ hạn gói.")]
        public EmployerSubscriptionDuration Duration { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; } = "PayOS";
    }

    [BindProperty] public CheckoutInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        if (!ModelState.IsValid)
        {
            TempData.SetErrorMessage("Dữ liệu thanh toán không hợp lệ.");
            return RedirectToPage();
        }

        try
        {
            if (Input.PaymentMethod == "PayOS")
            {
                var request = new CreatePayOSCheckoutRequestDto
                {
                    Tier = Input.Tier,
                    Duration = Input.Duration,
                    ReturnPath = "/Employer/Subscriptions/Success",
                    CancelPath = "/Employer/Subscriptions/Cancel"
                };

                var response = await apiClient.Api.Employer.Subscriptions.PayosCheckout.PostAsync(request);
                if (!string.IsNullOrEmpty(response?.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
                }

                TempData.SetErrorMessage("Không lấy được đường dẫn thanh toán từ PayOS.");
                return RedirectToPage();
            }

            if (Input.PaymentMethod == "VNPAY")
            {
                var request = new CreateVNPAYCheckoutRequestDto
                {
                    Tier = Input.Tier,
                    Duration = Input.Duration,
                    ReturnPath = "/Employer/Subscriptions/PaymentResult"
                };

                var response = await apiClient.Api.Employer.Subscriptions.VnpayCheckout.PostAsync(request);
                if (!string.IsNullOrEmpty(response?.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
                }

                TempData.SetErrorMessage("Không lấy được đường dẫn thanh toán từ VNPAY.");
                return RedirectToPage();
            }

            TempData.SetErrorMessage("Phương thức thanh toán không được hỗ trợ.");
            return RedirectToPage();
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "Lỗi từ Backend API khi tạo đơn thanh toán gói dịch vụ.");
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi hệ thống khi tạo yêu cầu thanh toán.");
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tạo yêu cầu thanh toán. Vui lòng thử lại sau.");
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostCancelSubscriptionAsync()
    {
        try
        {
            await apiClient.Api.Employer.Subscriptions.Cancel.PostAsync();
            TempData.SetSuccessMessage("Hủy gói dịch vụ thành công.");
            return RedirectToPage();
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "Lỗi khi hủy gói dịch vụ.");
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi không xác định khi hủy gói dịch vụ.");
            TempData.SetErrorMessage("Đã xảy ra lỗi khi hủy gói dịch vụ. Vui lòng thử lại sau.");
            return RedirectToPage();
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            CurrentSubscription = await apiClient.Api.Employer.Subscriptions.Current.GetAsync();
        }
        catch
        {
            CurrentSubscription = null;
        }

        try
        {
            Plans = await apiClient.Api.Employer.Subscriptions.Plans.GetAsync() ?? [];
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách gói dịch vụ.");
        }
    }
}