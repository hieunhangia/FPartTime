using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using Domain;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;

namespace Service.Manager
{
    public class StatisticService(ApplicationDbContext dbContext)
    {
        public async Task<OverviewKpiResponseDto> GetOverviewKpisAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (curFrom, curTo, prevFrom, prevTo) = DateRangeHelper.ResolveDates(fromDate, toDate);

            var curRevenue = await dbContext.EmployerSubscriptionOrders
                .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= curFrom && o.CompletedAt <= curTo)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var prevRevenue = await dbContext.EmployerSubscriptionOrders
                .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= prevFrom && o.CompletedAt <= prevTo)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var curJobs = await dbContext.Jobs
                .CountAsync(j => j.CreatedAt >= curFrom && j.CreatedAt <= curTo);
            var prevJobs = await dbContext.Jobs
                .CountAsync(j => j.CreatedAt >= prevFrom && j.CreatedAt <= prevTo);

            var curApps = await dbContext.JobApplications
                .CountAsync(a => a.AppliedAt >= curFrom && a.AppliedAt <= curTo);
            var prevApps = await dbContext.JobApplications
                .CountAsync(a => a.AppliedAt >= prevFrom && a.AppliedAt <= prevTo);

            var now = DateTime.UtcNow;
            var activeSubsCount = await dbContext.EmployerSubscriptions
                .Where(s => s.Status == EmployerSubscriptionStatus.Active && s.EndDate >= now)
                .Select(s => s.EmployerId)
                .Distinct()
                .CountAsync();
            return new OverviewKpiResponseDto
            {
                TotalRevenue = new KpiItemDto { Value = curRevenue, GrowthRate = DateRangeHelper.CalculateGrowthRate(curRevenue, prevRevenue) },
                NewJobs = new KpiItemDto { Value = curJobs, GrowthRate = DateRangeHelper.CalculateGrowthRate(curJobs, prevJobs) },
                TotalApplications = new KpiItemDto { Value = curApps, GrowthRate = DateRangeHelper.CalculateGrowthRate(curApps, prevApps) },
                ActiveSubscriptions = new KpiItemDto { Value = activeSubsCount, GrowthRate = 0 }
            };
        }

        public async Task<RevenueAnalyticsResponseDto> GetRevenueAnalyticsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, to, _, _) = DateRangeHelper.ResolveDates(fromDate, toDate);

            var ordersInPeriod = await dbContext.EmployerSubscriptionOrders
                .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= from && o.CompletedAt <= to)
                .Select(o => new { CompletedAt = o.CompletedAt!.Value, o.TotalAmount })
                .ToListAsync();
            var trend = ordersInPeriod
                .GroupBy(x => x.CompletedAt.AddHours(7).Date)
                            .OrderBy(g => g.Key)
                .Select(g => new ChartPointDto
                {
                    Label = g.Key.ToString("dd/MM"),
                    Value = g.Sum(x => x.TotalAmount)
                })
                .ToList();

            var breakdowns = await dbContext.EmployerSubscriptionOrders
                .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= from && o.CompletedAt <= to)
                .GroupBy(o => new { o.Tier, o.Duration })
                .Select(g => new PackageBreakdownDto
                {
                    Tier = g.Key.Tier.ToString(),
                    Duration = g.Key.Duration.ToString(),
                    TotalPurchases = g.Count(),
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var totalRev = breakdowns.Sum(x => x.Revenue);
            foreach (var item in breakdowns)
            {
                item.Percentage = totalRev > 0 ? Math.Round((double)(item.Revenue / totalRev) * 100, 2) : 0;
            }
            var payingUsersCount = await dbContext.EmployerSubscriptionOrders
                .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= from && o.CompletedAt <= to)
                .Select(o => o.UserId)
                .Distinct()
                .CountAsync();
            var arppu = payingUsersCount > 0 ? Math.Round(totalRev / payingUsersCount, 0) : 0;
            return new RevenueAnalyticsResponseDto
            {
                RevenueTrend = trend,
                PackageBreakdowns = breakdowns,
                Arppu = arppu
            };
        }


        public async Task<MarketplaceAnalyticsResponseDto> GetMarketplaceAnalyticsAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, to, _, _) = DateRangeHelper.ResolveDates(fromDate, toDate);

            var appsQuery = dbContext.JobApplications.Where(a => a.AppliedAt >= from && a.AppliedAt <= to);
            var totalApplied = await appsQuery.CountAsync();
            var totalAccepted = await appsQuery.CountAsync(a => a.Status == JobApplicationStatus.Accepted);
            var totalRejected = await appsQuery.CountAsync(a => a.Status == JobApplicationStatus.Rejected);

            var topIndustries = await dbContext.JobApplications
                .Include(a => a.Job).ThenInclude(j => j!.Industry)
                .Where(a => a.AppliedAt >= from && a.AppliedAt <= to && a.Job != null && a.Job.Industry != null)
                .GroupBy(a => a.Job!.Industry!.Name)
                .Select(g => new TopIndustryDto
                {
                    IndustryName = g.Key,
                    ApplicationCount = g.Count(),
                    JobCount = g.Select(x => x.JobId).Distinct().Count()
                })
                .OrderByDescending(x => x.ApplicationCount)
                .Take(5)
                .ToListAsync();
            return new MarketplaceAnalyticsResponseDto
            {
                Funnel = new RecruitmentFunnelDto
                {
                    TotalApplied = totalApplied,
                    TotalAccepted = totalAccepted,
                    TotalRejected = totalRejected,
                    FillRate = totalApplied > 0 ? Math.Round(((double)totalAccepted / totalApplied) * 100, 2) : 0
                },
                TopIndustries = topIndustries
            };
        }

        public async Task<QualityAndRetentionResponseDto> GetQualityAndRetentionAsync()
        {
            var now = DateTime.UtcNow;

            var sevenDaysAgo = now.AddDays(-7);
            var pendingQuery = dbContext.JobApplications.Where(a => a.Status == JobApplicationStatus.Pending);
            var totalPending = await pendingQuery.CountAsync();
            var staleCount = await pendingQuery.CountAsync(a => a.AppliedAt <= sevenDaysAgo);

            var totalReports = await dbContext.JobReports.CountAsync();
            var resolvedReports = await dbContext.JobReports.CountAsync(r => r.Status == JobReportStatus.Resolved);

            var topSpenders = await dbContext.Employers
                .Select(e => new
                {
                    EmployerName = e.CompanyName,
                    UserId = e.UserId,
                    TotalSpent = dbContext.EmployerSubscriptionOrders
                        .Where(o => o.UserId == e.UserId && o.Status == OrderStatus.Completed)
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0,
                    CurrentPlan = dbContext.EmployerSubscriptionOrders
                        .Where(o => o.UserId == e.UserId && o.Status == OrderStatus.Completed)
                        .OrderByDescending(o => o.CompletedAt)
                        .Select(o => o.PlanName)
                        .FirstOrDefault() ?? "Không có",
                    ActiveJobsCount = dbContext.Jobs
                        .Count(j => j.EmployerId == e.UserId && j.Status == JobStatus.Active)
                })
                .Where(x => x.TotalSpent > 0)
                .OrderByDescending(x => x.TotalSpent)
                .Take(5)
                .Select(x => new TopSpenderDto
                {
                    EmployerName = x.EmployerName,
                    TotalSpent = x.TotalSpent,
                    CurrentPlan = x.CurrentPlan,
                    ActiveJobsCount = x.ActiveJobsCount
                })
                .ToListAsync();

            var in7Days = now.AddDays(7);
            var expiringList = await dbContext.EmployerSubscriptions
                .Include(s => s.Employer)
                .Where(s => s.Status == EmployerSubscriptionStatus.Active && s.EndDate >= now && s.EndDate <= in7Days && s.Employer != null)
                .OrderBy(s => s.EndDate)
                .Select(s => new ExpiringEmployerDto
                {
                    EmployerName = s.Employer!.CompanyName,
                    Tier = s.Tier.ToString(),
                    EndDate = s.EndDate,
                    DaysRemaining = (s.EndDate - now).Days
                })
                .ToListAsync();
            return new QualityAndRetentionResponseDto
            {
                StaleApplicationsCount = staleCount,
                StaleRate = totalPending > 0 ? Math.Round(((double)staleCount / totalPending) * 100, 2) : 0,
                TotalReports = totalReports,
                ResolvedReports = resolvedReports,
                TopSpenders = topSpenders,
                ExpiringEmployers = expiringList
            };

        }


        public async Task<byte[]> ExportExcelAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (from, to, _, _) = DateRangeHelper.ResolveDates(fromDate, toDate);

            var overview = await GetOverviewKpisAsync(fromDate, toDate);
            var revenue = await GetRevenueAnalyticsAsync(fromDate, toDate);
            var quality = await GetQualityAndRetentionAsync();

            var detailedOrders = await dbContext.EmployerSubscriptionOrders
                .Include(o => o.User)!.ThenInclude(u => u!.EmployerProfile)
                .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= from && o.CompletedAt <= to)
                .OrderByDescending(o => o.CompletedAt)
                .ToListAsync();
            using var workbook = new XLWorkbook();

            var wsSummary = workbook.Worksheets.Add("Báo Cáo Tổng Hợp");

            wsSummary.Cell("A1").Value = "BÁO CÁO QUẢN TRỊ & THỐNG KÊ DOANH THU FPARTTIME";
            wsSummary.Cell("A1").Style.Font.Bold = true;
            wsSummary.Cell("A1").Style.Font.FontSize = 16;
            wsSummary.Cell("A2").Value = $"Kỳ báo cáo: {from.AddHours(7):dd/MM/yyyy} - {to.AddHours(7):dd/MM/yyyy} | Ngày xuất: {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm}";
            wsSummary.Cell("A2").Style.Font.Italic = true;

            wsSummary.Cell("A4").Value = "I. CHỈ SỐ KPI TỔNG QUAN";
            wsSummary.Cell("A4").Style.Font.Bold = true;
            wsSummary.Cell("A5").Value = "Tổng doanh thu thực nhận:";
            wsSummary.Cell("B5").Value = overview.TotalRevenue.Value;
            wsSummary.Cell("B5").Style.NumberFormat.Format = "#,##0 \"đ\"";
            wsSummary.Cell("A6").Value = "Doanh nghiệp trả phí (Active):";
            wsSummary.Cell("B6").Value = overview.ActiveSubscriptions.Value;
            wsSummary.Cell("A7").Value = "Tin tuyển dụng mới:";
            wsSummary.Cell("B7").Value = overview.NewJobs.Value;
            wsSummary.Cell("A8").Value = "Lượt ứng tuyển sinh viên:";
            wsSummary.Cell("B8").Value = overview.TotalApplications.Value;
            wsSummary.Cell("A9").Value = "Doanh thu TB / Khách hàng (ARPPU):";
            wsSummary.Cell("B9").Value = revenue.Arppu;
            wsSummary.Cell("B9").Style.NumberFormat.Format = "#,##0 \"đ\"";

            wsSummary.Cell("A11").Value = "II. PHÂN BỔ DOANH THU THEO GÓI HỘI VIÊN";
            wsSummary.Cell("A11").Style.Font.Bold = true;
            string[] headersPkg = { "Hạng Gói", "Thời Hạn", "Số Lượt Mua", "Doanh Thu", "Tỷ Trọng (%)" };
            for (int i = 0; i < headersPkg.Length; i++)
            {
                var cell = wsSummary.Cell(12, i + 1);
                cell.Value = headersPkg[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            int row = 13;
            foreach (var pkg in revenue.PackageBreakdowns)
            {
                wsSummary.Cell(row, 1).Value = pkg.Tier;
                wsSummary.Cell(row, 2).Value = pkg.Duration;
                wsSummary.Cell(row, 3).Value = pkg.TotalPurchases;
                wsSummary.Cell(row, 4).Value = pkg.Revenue;
                wsSummary.Cell(row, 4).Style.NumberFormat.Format = "#,##0 \"đ\"";
                wsSummary.Cell(row, 5).Value = pkg.Percentage / 100;
                wsSummary.Cell(row, 5).Style.NumberFormat.Format = "0.0%";
                row++;
            }
            wsSummary.Columns().AdjustToContents();

            var wsDetail = workbook.Worksheets.Add("Bảng Kê Đơn Hàng");
            string[] headersDetail = { "Mã Đơn", "Tên Doanh Nghiệp", "Gói Dịch Vụ", "Hạng", "Thời Hạn", "Số Tiền", "Thời Điểm Thanh Toán" };
            for (int i = 0; i < headersDetail.Length; i++)
            {
                var cell = wsDetail.Cell(1, i + 1);
                cell.Value = headersDetail[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.BabyBlue;
            }
            int dRow = 2;
            foreach (var order in detailedOrders)
            {
                wsDetail.Cell(dRow, 1).Value = order.Id.ToString();
                wsDetail.Cell(dRow, 2).Value = order.User?.EmployerProfile?.CompanyName ?? "N/A";
                wsDetail.Cell(dRow, 3).Value = order.PlanName;
                wsDetail.Cell(dRow, 4).Value = order.Tier.ToString();
                wsDetail.Cell(dRow, 5).Value = order.Duration.ToString();
                wsDetail.Cell(dRow, 6).Value = order.TotalAmount;
                wsDetail.Cell(dRow, 6).Style.NumberFormat.Format = "#,##0 \"đ\"";
                wsDetail.Cell(dRow, 7).Value = order.CompletedAt?.AddHours(7).ToString("dd/MM/yyyy HH:mm") ?? "";
                dRow++;
            }
            wsDetail.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }


        public static class DateRangeHelper
        {
            private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);
            public static (DateTime CurrentFromUtc, DateTime CurrentToUtc, DateTime PrevFromUtc, DateTime PrevToUtc)
                ResolveDates(DateTime? from, DateTime? to)
            {
                var vnNowDate = DateTime.UtcNow.Add(VnOffset).Date;

                static DateTime NormalizeDate(DateTime dt) =>
                dt.Kind == DateTimeKind.Unspecified ? dt.Date : dt.ToUniversalTime().Date;

                DateTime toLocalDate = to?.Date ?? vnNowDate;
                DateTime fromLocalDate = from?.Date ?? toLocalDate.AddDays(-30);

                if (toLocalDate > vnNowDate.AddDays(1))
                    throw new BadRequestException("Khoảng thời gian thống kê không hợp lệ.");
                if (fromLocalDate > toLocalDate)
                    throw new BadRequestException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                if ((toLocalDate - fromLocalDate).TotalDays > 365)
                    throw new BadRequestException("Chỉ hỗ trợ thống kê tối đa trong khoảng 365 ngày.");

                DateTime currentFromUtc = fromLocalDate.Subtract(VnOffset);
                DateTime currentToUtc = toLocalDate.AddDays(1).AddTicks(-1).Subtract(VnOffset);
                TimeSpan duration = currentToUtc - currentFromUtc;
                DateTime prevToUtc = currentFromUtc.AddTicks(-1);
                DateTime prevFromUtc = prevToUtc - duration;
                return (currentFromUtc, currentToUtc, prevFromUtc, prevToUtc);
            }

            public static double CalculateGrowthRate(decimal current, decimal previous)
            {
                if (previous == 0)
                {
                    return current > 0 ? 100 : 0;
                }
                return (double)Math.Round(((current - previous) / previous) * 100, 2);
            }
        }



        public class KpiItemDto
        {
            public required decimal Value { get; set; }
            public required double GrowthRate { get; set; }
            public bool IsPositive => GrowthRate >= 0;
        }
        public class OverviewKpiResponseDto
        {
            public required KpiItemDto TotalRevenue { get; set; }
            public required KpiItemDto ActiveSubscriptions { get; set; }
            public required KpiItemDto NewJobs { get; set; }
            public required KpiItemDto TotalApplications { get; set; }
        }
        public class ChartPointDto
        {
            public required string Label { get; set; }
            public required decimal Value { get; set; }
        }
        public class PackageBreakdownDto
        {
            public required string Tier { get; set; }
            public required string Duration { get; set; }
            public required int TotalPurchases { get; set; }
            public required decimal Revenue { get; set; }
            public double Percentage { get; set; }
        }
        public class RevenueAnalyticsResponseDto
        {
            public required List<ChartPointDto> RevenueTrend { get; set; }
            public required List<PackageBreakdownDto> PackageBreakdowns { get; set; }
            public required decimal Arppu { get; set; }
        }
        public class TopIndustryDto
        {
            public required string IndustryName { get; set; }
            public required int ApplicationCount { get; set; }
            public required int JobCount { get; set; }
        }
        public class RecruitmentFunnelDto
        {
            public required int TotalApplied { get; set; }
            public required int TotalAccepted { get; set; }
            public required int TotalRejected { get; set; }
            public required double FillRate { get; set; }
        }
        public class MarketplaceAnalyticsResponseDto
        {
            public required RecruitmentFunnelDto Funnel { get; set; }
            public required List<TopIndustryDto> TopIndustries { get; set; }
        }
        public class TopSpenderDto
        {
            public required string EmployerName { get; set; }
            public required decimal TotalSpent { get; set; }
            public required string CurrentPlan { get; set; }
            public required int ActiveJobsCount { get; set; }
        }
        public class ExpiringEmployerDto
        {
            public required string EmployerName { get; set; }
            public required string Tier { get; set; }
            public required DateTime EndDate { get; set; }
            public required int DaysRemaining { get; set; }
        }
        public class QualityAndRetentionResponseDto
        {
            public required int StaleApplicationsCount { get; set; }
            public required double StaleRate { get; set; }
            public required int TotalReports { get; set; }
            public required int ResolvedReports { get; set; }
            public required List<TopSpenderDto> TopSpenders { get; set; }
            public required List<ExpiringEmployerDto> ExpiringEmployers { get; set; }
        }
    }
}
