using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Employer.Orders;

[Authorize(Roles = Role.Employer)]
public class DetailModel(ApiClient apiClient) : PageModel
{
    public OrderDetailResponseDto? Order { get; private set; }

    [BindProperty]
    public string PaymentMethod { get; set; } = "PayOS";

    public bool CanCreatePaymentTransaction =>
        Order != null &&
        Order.Status != OrderStatus.Completed &&
        Order.Status != OrderStatus.Cancelled &&
        (!Order.PaymentTransactions?.Any(t => t.Status == PaymentTransactionStatus.Pending) ?? true);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Order = await apiClient.Api.Employer.Orders[id].GetAsync();
            if (Order == null)
            {
                TempData.SetErrorMessage("Không tìm thấy thông tin đơn hàng.");
                return RedirectToPage("./Index");
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage("./Index");
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải thông tin đơn hàng.");
            return RedirectToPage("./Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelOrderAsync(Guid id)
    {
        try
        {
            await apiClient.Api.Employer.Orders[id].Cancel.PutAsync();
            TempData.SetSuccessMessage("Đã hủy đơn hàng thành công.");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi hủy đơn hàng.");
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelTransactionAsync(Guid transactionId, Guid id)
    {
        try
        {
            await apiClient.Api.Employer.PaymentTransactions[transactionId].Cancel.PutAsync();
            TempData.SetSuccessMessage("Đã hủy giao dịch thanh toán thành công.");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi hủy giao dịch.");
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCreatePaymentAsync(Guid id)
    {
        try
        {
            if (PaymentMethod == "PayOS")
            {
                var request = new CreatePayOSCheckoutRequestDto
                {
                    OrderId = id,
                    ReturnPath = "/Employer/Subscriptions/Success",
                    CancelPath = "/Employer/Subscriptions/Cancel"
                };

                var response = await apiClient.Api.Employer.Subscriptions.PayosCheckout.PostAsync(request);
                if (!string.IsNullOrEmpty(response?.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
                }

                TempData.SetErrorMessage("Không lấy được đường dẫn thanh toán từ PayOS.");
                return RedirectToPage(new { id });
            }

            if (PaymentMethod == "VNPAY")
            {
                var request = new CreateVNPAYCheckoutRequestDto
                {
                    OrderId = id,
                    ReturnPath = "/Employer/Subscriptions/VNPAYPaymentResult"
                };

                var response = await apiClient.Api.Employer.Subscriptions.VnpayCheckout.PostAsync(request);
                if (!string.IsNullOrEmpty(response?.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
                }

                TempData.SetErrorMessage("Không lấy được đường dẫn thanh toán từ VNPAY.");
                return RedirectToPage(new { id });
            }

            if (PaymentMethod == "ZaloPay")
            {
                var request = new CreateZaloPayCheckoutRequestDto
                {
                    OrderId = id,
                    ReturnPath = "/Employer/Subscriptions/ZaloPayPaymentResult"
                };

                var response = await apiClient.Api.Employer.Subscriptions.ZalopayCheckout.PostAsync(request);
                if (!string.IsNullOrEmpty(response?.PaymentUrl))
                {
                    return Redirect(response.PaymentUrl);
                }

                TempData.SetErrorMessage("Không lấy được đường dẫn thanh toán từ ZaloPay.");
                return RedirectToPage(new { id });
            }

            TempData.SetErrorMessage("Phương thức thanh toán không được hỗ trợ.");
            return RedirectToPage(new { id });
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return RedirectToPage(new { id });
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tạo yêu cầu thanh toán. Vui lòng thử lại sau.");
            return RedirectToPage(new { id });
        }
    }

    public (string Label, string CssClass) GetOrderStatusDisplay(OrderStatus? status) => status switch
    {
        OrderStatus.PendingPayment => ("Chờ thanh toán", "bg-warning text-dark"),
        OrderStatus.Completed => ("Hoàn thành", "bg-success"),
        OrderStatus.Cancelled => ("Đã hủy", "bg-secondary"),
        OrderStatus.Expired => ("Hết hạn", "bg-danger"),
        _ => (status?.ToString() ?? "-", "bg-secondary")
    };

    public (string Label, string CssClass) GetTransactionStatusDisplay(PaymentTransactionStatus? status) => status switch
    {
        PaymentTransactionStatus.Pending => ("Đang chờ", "bg-warning text-dark"),
        PaymentTransactionStatus.Paid => ("Đã thanh toán", "bg-success"),
        PaymentTransactionStatus.Cancelled => ("Đã hủy", "bg-secondary"),
        PaymentTransactionStatus.Expired => ("Hết hạn", "bg-danger"),
        _ => (status?.ToString() ?? "-", "bg-secondary")
    };

    public bool CanPayNow(PaymentTransactionResponseDto tx) =>
        tx.Status == PaymentTransactionStatus.Pending &&
        !string.IsNullOrWhiteSpace(tx.PaymentUrl) &&
        tx.ExpiredAt > DateTimeOffset.UtcNow;

    public bool IsTransactionExpired(PaymentTransactionResponseDto tx) =>
        tx.Status == PaymentTransactionStatus.Pending &&
        tx.ExpiredAt <= DateTimeOffset.UtcNow;
}
