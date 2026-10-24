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
public class IndexModel(ApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public OrderStatus? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? OrderBy { get; set; } = "CreatedAt_Desc";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; } = 10;

    public List<OrderSummaryResponseDto> Orders { get; private set; } = [];

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
            var result = await apiClient.Api.Employer.Orders
                .GetPagedWithODataAsync<OrderSummaryResponseDto, PagedResponseDtoOfOrderSummaryResponseDto>(q =>
                {
                    if (StatusFilter.HasValue)
                    {
                        q.Filter(o => o.Status == StatusFilter.Value);
                    }

                    switch (OrderBy)
                    {
                        case "CreatedAt_Asc":
                            q.OrderBy(o => o.CreatedAt);
                            break;
                        case "TotalAmount_Desc":
                            q.OrderByDescending(o => o.TotalAmount);
                            break;
                        case "TotalAmount_Asc":
                            q.OrderBy(o => o.TotalAmount);
                            break;
                        default:
                            q.OrderByDescending(o => o.CreatedAt);
                            break;
                    }

                    q.Skip(skip).Top(PageSize);
                }, dateFilter);

            Orders = result?.Items ?? [];
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
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tải danh sách đơn hàng.");
        }

        return Page();
    }

    public (string Label, string CssClass) GetStatusDisplay(OrderStatus? status) => status switch
    {
        OrderStatus.PendingPayment => ("Chờ thanh toán", "bg-warning text-dark"),
        OrderStatus.Completed => ("Hoàn thành", "bg-success"),
        OrderStatus.Cancelled => ("Đã hủy", "bg-secondary"),
        OrderStatus.Expired => ("Hết hạn", "bg-danger"),
        _ => (status?.ToString() ?? "-", "bg-secondary")
    };
}
