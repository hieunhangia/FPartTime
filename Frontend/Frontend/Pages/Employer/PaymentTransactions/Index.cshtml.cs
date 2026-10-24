using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Employer.PaymentTransactions;

[Authorize(Roles = Role.Employer)]
public class IndexModel(ApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public PaymentTransactionStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? OrderBy { get; set; } = "CreatedAt_Desc";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 10;

    public List<PaymentTransactionResponseDto> Transactions { get; private set; } = [];

    public long TotalCount { get; private set; }

    public int TotalPages { get; private set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        if (PageNumber < 1)
        {
            PageNumber = 1;
        }

        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            TempData.SetWarningMessage("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            (FromDate, ToDate) = (ToDate, FromDate);
        }

        var skip = (PageNumber - 1) * PageSize;

        // Xây dựng điều kiện lọc khoảng thời gian CreatedAt cho OData
        var dateClauses = new List<string>();
        if (FromDate.HasValue)
        {
            dateClauses.Add($"CreatedAt ge {FromDate.Value:yyyy-MM-dd}T00:00:00Z");
        }

        if (ToDate.HasValue)
        {
            dateClauses.Add($"CreatedAt le {ToDate.Value:yyyy-MM-dd}T23:59:59Z");
        }

        var dateFilter = dateClauses.Count > 0 ? string.Join(" and ", dateClauses) : null;

        try
        {
            var result = await apiClient.Api.Employer.PaymentTransactions
                .GetPagedWithODataAsync<PaymentTransactionResponseDto, PagedResponseDtoOfPaymentTransactionResponseDto>(q =>
                {
                    if (StatusFilter.HasValue)
                    {
                        q.Filter(t => t.Status == StatusFilter.Value);
                    }

                    switch (OrderBy)
                    {
                        case "CreatedAt_Asc":
                            q.OrderBy(t => t.CreatedAt);
                            break;
                        case "Amount_Desc":
                            q.OrderByDescending(t => t.Amount);
                            break;
                        case "Amount_Asc":
                            q.OrderBy(t => t.Amount);
                            break;
                        default:
                            q.OrderByDescending(t => t.CreatedAt);
                            break;
                    }

                    q.Skip(skip).Top(PageSize);
                }, dateFilter);

            Transactions = result?.Items ?? [];
            TotalCount = result?.TotalCount ?? 0;
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
            if (TotalPages == 0)
            {
                TotalPages = 1;
            }

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách giao dịch.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        try
        {
            await apiClient.Api.Employer.PaymentTransactions[id].Cancel.PutAsync();
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

        return RedirectToPage(new
        {
            StatusFilter,
            FromDate = FromDate?.ToString("yyyy-MM-dd"),
            ToDate = ToDate?.ToString("yyyy-MM-dd"),
            OrderBy,
            PageNumber
        });
    }

    public (string Label, string CssClass) GetStatusDisplay(PaymentTransactionStatus? status) => status switch
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
