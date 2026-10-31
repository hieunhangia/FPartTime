using System.Text.Json;
using ApiSdk;
using ApiSdk.Models;
using Frontend.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Frontend.Pages.Manager.Statistics
{
    [Authorize(Roles =Role.Manager)]
    public class IndexModel(ApiClient apiClient) : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Preset { get; set; } = "30days"; 
        public OverviewKpiResponseDto? Overview { get; private set; }
        public RevenueAnalyticsResponseDto? Revenue { get; private set; }
        public MarketplaceAnalyticsResponseDto? Marketplace { get; private set; }
        public QualityAndRetentionResponseDto? QualityRetention { get; private set; }
        public string RevenueTrendJson { get; private set; } = "[]";
        public string PackageBreakdownsJson { get; private set; } = "[]";
        public string TopIndustriesJson { get; private set; } = "[]";
        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            ResolvePresetDates();
            var overviewTask = apiClient.Api.Manager.Statistics.Overview.GetAsync(config =>
            {
                config.QueryParameters.FromDate = FromDate;
                config.QueryParameters.ToDate = ToDate;
            }, cancellationToken);
            var revenueTask = apiClient.Api.Manager.Statistics.Revenue.GetAsync(config =>
            {
                config.QueryParameters.FromDate = FromDate;
                config.QueryParameters.ToDate = ToDate;
            }, cancellationToken);
            var marketplaceTask = apiClient.Api.Manager.Statistics.Marketplace.GetAsync(config =>
            {
                config.QueryParameters.FromDate = FromDate;
                config.QueryParameters.ToDate = ToDate;
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
            return Page();
        }
        private void ResolvePresetDates()
        {
            var now = DateTime.UtcNow;
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
