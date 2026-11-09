using System.Text.Json;
using ApiSdk;
using ApiSdk.Models;
using Frontend.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BackendApiClient.Extensions;
using Frontend.Extensions;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Manager.Statistics
{
    [Authorize(Roles = Role.Manager)]
    public class IndexModel(ApiClient apiClient) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? Preset { get; set; } = "30days";
        public OverviewKpiResponseDto? Overview { get; private set; }
        public RevenueAnalyticsResponseDto? Revenue { get; private set; }
        public MarketplaceAnalyticsResponseDto? Marketplace { get; private set; }
        public QualityAndRetentionResponseDto? QualityRetention { get; private set; }
        public string RevenueTrendJson { get; private set; } = "[]";
        public string PackageBreakdownsJson { get; private set; } = "[]";
        public string TopIndustriesJson { get; private set; } = "[]";
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            ResolvePresetDates();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (FromDate > ToDate)
            {
                ModelState.AddModelError(string.Empty, "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return Page();
            }

            var fromOffset = FromDate.HasValue ? new DateTimeOffset(FromDate.Value.Date, TimeSpan.Zero) : (DateTimeOffset?)null;
            var toOffset = ToDate.HasValue ? new DateTimeOffset(ToDate.Value.Date, TimeSpan.Zero) : (DateTimeOffset?)null;
            try
            {
                var overviewTask = apiClient.Api.Manager.Statistics.Overview.GetAsync(c =>
                {
                    c.QueryParameters.FromDate = fromOffset;
                    c.QueryParameters.ToDate = toOffset;
                }, cancellationToken);
                var revenueTask = apiClient.Api.Manager.Statistics.Revenue.GetAsync(c =>
                {
                    c.QueryParameters.FromDate = fromOffset;
                    c.QueryParameters.ToDate = toOffset;
                }, cancellationToken);
                var marketplaceTask = apiClient.Api.Manager.Statistics.Marketplace.GetAsync(c =>
                {
                    c.QueryParameters.FromDate = fromOffset;
                    c.QueryParameters.ToDate = toOffset;
                }, cancellationToken);
                var qualityTask = apiClient.Api.Manager.Statistics.QualityRetention.GetAsync(cancellationToken: cancellationToken);
                await Task.WhenAll(overviewTask, revenueTask, marketplaceTask, qualityTask);
                Overview = await overviewTask;
                Revenue = await revenueTask;
                Marketplace = await marketplaceTask;
                QualityRetention = await qualityTask;
                if (Revenue != null)
                {
                    RevenueTrendJson = JsonSerializer.Serialize(Revenue.RevenueTrend);
                    PackageBreakdownsJson = JsonSerializer.Serialize(Revenue.PackageBreakdowns);
                }
                if (Marketplace != null)
                {
                    TopIndustriesJson = JsonSerializer.Serialize(Marketplace.TopIndustries);
                }
            }
            catch (ApiException ex) when (ex.ResponseStatusCode != 401)
            {
                ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "Không thể kết nối đến máy chủ thống kê. Vui lòng thử lại sau.");
            }
            return Page();
        }

        public async Task<IActionResult> OnGetExportExcelAsync(CancellationToken cancellationToken)
        {
            ResolvePresetDates();

            if (FromDate > ToDate)
            {
                TempData.SetErrorMessage("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return RedirectToPage(new
                {
                    FromDate = FromDate?.ToString("yyyy-MM-dd"),
                    ToDate = ToDate?.ToString("yyyy-MM-dd"),
                    Preset
                });
            }

            var fromOffset = FromDate.HasValue ? new DateTimeOffset(FromDate.Value.Date, TimeSpan.Zero) : (DateTimeOffset?)null;
            var toOffset = ToDate.HasValue ? new DateTimeOffset(ToDate.Value.Date, TimeSpan.Zero) : (DateTimeOffset?)null;
            try
            {
                var stream = await apiClient.Api.Manager.Statistics.ExportExcel.GetAsync(c =>
                {
                    c.QueryParameters.FromDate = fromOffset;
                    c.QueryParameters.ToDate = toOffset;
                }, cancellationToken);
                if (stream == null)
                {
                    TempData.SetErrorMessage("Không có dữ liệu để xuất báo cáo.");
                    return RedirectToPage(new { FromDate, ToDate, Preset });
                }
                var vnNow = DateTime.UtcNow.Add(VietnamOffset);
                var fileName = $"BaoCao_FPartTime_{vnNow:yyyyMMdd_HHmm}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (ApiException ex) when (ex.ResponseStatusCode != 401)
            {
                TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
                return RedirectToPage(new { FromDate, ToDate, Preset });
            }
            catch (HttpRequestException)
            {
                TempData.SetErrorMessage("Không thể kết nối đến máy chủ để xuất file Excel.");
                return RedirectToPage(new { FromDate, ToDate, Preset });
            }
        }

        private void ResolvePresetDates()
        {
            var now = DateTime.UtcNow.Add(VietnamOffset);
            switch (Preset)
            {
                case "today":
                    FromDate = now.Date;
                    ToDate = now.Date;
                    break;
                case "7days":
                    FromDate = now.Date.AddDays(-7);
                    ToDate = now.Date;
                    break;
                case "thismonth":
                    FromDate = new DateTime(now.Year, now.Month, 1);
                    ToDate = now.Date;
                    break;
                case "30days":
                default:
                    if (!FromDate.HasValue || !ToDate.HasValue)
                    {
                        FromDate = now.Date.AddDays(-30);
                        ToDate = now.Date;
                        Preset = "30days";
                    }
                    break;
            }
        }
    }
}
