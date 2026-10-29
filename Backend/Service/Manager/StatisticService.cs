using System;
using System.Collections.Generic;
using System.Text;
using Domain;
using Domain.Shared.Enums;
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
                .Select(o => new { CompletedDate = o.CompletedAt!.Value.Date, o.TotalAmount })
                .ToListAsync();
            var trend = ordersInPeriod
                .GroupBy(x => x.CompletedDate)
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
            // Phễu tuyển dụng
            var appsQuery = dbContext.JobApplications.Where(a => a.AppliedAt >= from && a.AppliedAt <= to);
            var totalApplied = await appsQuery.CountAsync();
            var totalAccepted = await appsQuery.CountAsync(a => a.Status == JobApplicationStatus.Accepted);
            var totalRejected = await appsQuery.CountAsync(a => a.Status == JobApplicationStatus.Rejected);
            // Top 5 ngành nghề thu hút sinh viên nhất
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
            // 1. Hồ sơ bị ngâm quá 7 ngày
            var sevenDaysAgo = now.AddDays(-7);
            var pendingQuery = dbContext.JobApplications.Where(a => a.Status == JobApplicationStatus.Pending);
            var totalPending = await pendingQuery.CountAsync();
            var staleCount = await pendingQuery.CountAsync(a => a.AppliedAt <= sevenDaysAgo);
            // 2. Báo cáo vi phạm việc làm
            var totalReports = await dbContext.JobReports.CountAsync();
            var resolvedReports = await dbContext.JobReports.CountAsync(r => r.Status == JobReportStatus.Resolved);
            // 3. Top 5 Doanh nghiệp chi tiêu nhiều nhất (Top Spenders)
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
            // 4. Doanh nghiệp sắp hết hạn trong 7 ngày tới
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



        public static class DateRangeHelper
        {
            public static (DateTime CurrentFrom, DateTime CurrentTo, DateTime PrevFrom, DateTime PrevTo)
                ResolveDates(DateTime? from, DateTime? to)
            {
                DateTime currentTo = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;
                DateTime currentFrom = from?.Date ?? currentTo.AddDays(-30);

                TimeSpan duration = currentTo - currentFrom;

                DateTime prevTo = currentFrom.AddTicks(-1);
                DateTime prevFrom = prevTo - duration;

                return (currentFrom, currentTo, prevFrom, prevTo);
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
