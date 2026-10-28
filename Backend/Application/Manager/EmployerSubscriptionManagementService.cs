using System.Data;
using Application.Employer;
using Contracts.Application.Manager;
using Domain;
using Domain.Models.EmployerSubscriptions;
using Domain.Models.Notifications;
using Domain.Models.Orders;
using Domain.Models.Users;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Application.Manager;

public class EmployerSubscriptionManagementService(
    ApplicationDbContext context,
    SubscriptionQuotaService quotaService) : IEmployerSubscriptionManagementService
{
    public async Task<List<ManagedEmployerSubscriptionResponseDto>> GetSubscriptionsAsync(Guid employerId,
        CancellationToken cancellationToken = default)
    {
        await GetEmployerAsync(employerId, cancellationToken);
        return await context.EmployerSubscriptions.AsNoTracking()
            .Where(subscription => subscription.EmployerId == employerId)
            .OrderByDescending(subscription => subscription.CreatedAt)
            .ProjectToManagedDto()
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagedEmployerSubscriptionResponseDto> GetSubscriptionAsync(Guid employerId,
        Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        await GetEmployerAsync(employerId, cancellationToken);
        return await context.EmployerSubscriptions.AsNoTracking()
            .Where(subscription => subscription.Id == subscriptionId && subscription.EmployerId == employerId)
            .ProjectToManagedDto()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy gói đăng ký của nhà tuyển dụng.");
    }

    public async Task<ManagedEmployerSubscriptionResponseDto> GrantAsync(Guid employerId,
        GrantEmployerSubscriptionRequestDto request, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(request.Tier, request.Duration, cancellationToken);
        var now = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var employer = await GetEmployerAsync(employerId, cancellationToken);
        await EnsureNoPendingPaymentAsync(employerId, cancellationToken);

        var subscriptions = await context.EmployerSubscriptions
            .Where(subscription => subscription.EmployerId == employerId &&
                                   subscription.Status == EmployerSubscriptionStatus.Active)
            .ToListAsync(cancellationToken);
        if (subscriptions.Any(subscription => subscription.EndDate >= now))
        {
            throw new ConflictException("Nhà tuyển dụng đã có gói đăng ký đang hoạt động.");
        }

        foreach (var expired in subscriptions)
        {
            expired.Status = EmployerSubscriptionStatus.Expired;
        }

        var subscription = new EmployerSubscription
        {
            EmployerId = employerId,
            Tier = plan.Tier,
            StartDate = now,
            EndDate = now.AddDays(plan.DurationDays),
            ExtraActivePosts = plan.ExtraActivePosts,
            CanRecommendCandidates = plan.CanRecommendCandidates,
            CanQueryCandidates = plan.CanQueryCandidates
        };
        context.EmployerSubscriptions.Add(subscription);
        employer.BumpBalance += plan.FreeBumps;
        await quotaService.ApplyAsync(employer, subscription.ExtraActivePosts, now, cancellationToken);

        AddNotification(employerId, "Đã được cấp gói đăng ký",
            $"Bạn đã được cấp gói {plan.Name}. Gói có hiệu lực đến {subscription.EndDate:dd/MM/yyyy HH:mm} (UTC).");

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return subscription.MapToManagedDto();
    }

    public async Task<ManagedEmployerSubscriptionResponseDto> UpdatePeriodAsync(Guid employerId,
        Guid subscriptionId, UpdateEmployerSubscriptionPeriodRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndDate is null)
        {
            throw new BadRequestException("Ngày hết hạn là bắt buộc.");
        }

        var endDate = request.EndDate.Value.UtcDateTime;
        var now = DateTime.UtcNow;
        if (endDate <= now)
        {
            throw new BadRequestException("Ngày hết hạn mới phải ở tương lai.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        await GetEmployerAsync(employerId, cancellationToken);
        await EnsureNoPendingPaymentAsync(employerId, cancellationToken);
        var subscription = await GetActiveSubscriptionAsync(employerId, subscriptionId, now, cancellationToken);
        if (endDate <= subscription.StartDate)
        {
            throw new BadRequestException("Ngày hết hạn phải sau ngày bắt đầu gói.");
        }

        subscription.EndDate = endDate;
        AddNotification(employerId, "Thời hạn gói đăng ký đã thay đổi",
            $"Thời hạn gói {subscription.Tier} của bạn được điều chỉnh đến {endDate:dd/MM/yyyy HH:mm} (UTC).");

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return subscription.MapToManagedDto();
    }

    public async Task<ManagedEmployerSubscriptionResponseDto> UpdateTierAsync(Guid employerId,
        Guid subscriptionId, UpdateEmployerSubscriptionTierRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanAsync(request.Tier, request.Duration, cancellationToken);
        var now = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var employer = await GetEmployerAsync(employerId, cancellationToken);
        await EnsureNoPendingPaymentAsync(employerId, cancellationToken);
        var subscription = await GetActiveSubscriptionAsync(employerId, subscriptionId, now, cancellationToken);
        subscription.Tier = plan.Tier;
        subscription.ExtraActivePosts = plan.ExtraActivePosts;
        subscription.CanRecommendCandidates = plan.CanRecommendCandidates;
        subscription.CanQueryCandidates = plan.CanQueryCandidates;
        var closedJobs = await quotaService.ApplyAsync(employer, plan.ExtraActivePosts, now, cancellationToken);

        AddNotification(employerId, "Hạng gói đăng ký đã thay đổi",
            closedJobs > 0
                ? $"Gói của bạn đã đổi sang {plan.Tier}. Hệ thống đã đóng {closedJobs} tin tuyển dụng để phù hợp với hạn mức mới ({employer.MaxActivePosts} tin)."
                : $"Gói của bạn đã đổi sang {plan.Tier}. Hạn mức đăng tin hiện tại là {employer.MaxActivePosts} tin.");

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return subscription.MapToManagedDto();
    }

    public async Task RevokeAsync(Guid employerId, Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable,
            cancellationToken);
        var employer = await GetEmployerAsync(employerId, cancellationToken);
        await EnsureNoPendingPaymentAsync(employerId, cancellationToken);
        var subscription = await GetActiveSubscriptionAsync(employerId, subscriptionId, now, cancellationToken);
        subscription.Status = EmployerSubscriptionStatus.Cancelled;
        var otherActive = await context.EmployerSubscriptions.AsNoTracking()
            .Where(other => other.EmployerId == employerId && other.Id != subscriptionId &&
                            other.Status == EmployerSubscriptionStatus.Active && other.EndDate >= now)
            .OrderByDescending(other => other.EndDate)
            .FirstOrDefaultAsync(cancellationToken);
        var closedJobs = await quotaService.ApplyAsync(employer, otherActive?.ExtraActivePosts ?? 0, now,
            cancellationToken);

        AddNotification(employerId, "Gói đăng ký đã bị thu hồi",
            closedJobs > 0
                ? $"Gói {subscription.Tier} đã bị thu hồi. Hệ thống đã đóng {closedJobs} tin tuyển dụng để phù hợp với hạn mức hiện tại ({employer.MaxActivePosts} tin)."
                : $"Gói {subscription.Tier} đã bị thu hồi. Hạn mức đăng tin hiện tại là {employer.MaxActivePosts} tin.");

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<EmployerProfile> GetEmployerAsync(Guid employerId, CancellationToken cancellationToken) =>
        await context.Employers.FirstOrDefaultAsync(employer => employer.UserId == employerId, cancellationToken)
        ?? throw new NotFoundException("Không tìm thấy nhà tuyển dụng.");

    private async Task<EmployerSubscriptionPlan> GetPlanAsync(EmployerSubscriptionTier? tier,
        EmployerSubscriptionDuration? duration, CancellationToken cancellationToken)
    {
        if (tier is null || duration is null || !Enum.IsDefined(tier.Value) || !Enum.IsDefined(duration.Value))
        {
            throw new BadRequestException("Hạng gói hoặc kỳ hạn gói không hợp lệ.");
        }

        return await context.EmployerSubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(plan => plan.Tier == tier.Value && plan.Duration == duration.Value &&
                                         plan.IsActive, cancellationToken)
            ?? throw new NotFoundException("Gói đăng ký không tồn tại hoặc đã ngừng cung cấp.");
    }

    private async Task<EmployerSubscription> GetActiveSubscriptionAsync(Guid employerId, Guid subscriptionId,
        DateTime now, CancellationToken cancellationToken)
    {
        var subscription = await context.EmployerSubscriptions.FirstOrDefaultAsync(
            candidate => candidate.Id == subscriptionId && candidate.EmployerId == employerId,
            cancellationToken) ?? throw new NotFoundException("Không tìm thấy gói đăng ký của nhà tuyển dụng.");
        if (subscription.Status != EmployerSubscriptionStatus.Active || subscription.EndDate < now)
        {
            throw new ConflictException("Gói đăng ký không còn hoạt động.");
        }

        return subscription;
    }

    private async Task EnsureNoPendingPaymentAsync(Guid employerId, CancellationToken cancellationToken)
    {
        if (await context.PaymentTransactions.AsNoTracking().AnyAsync(
                payment => payment.UserId == employerId &&
                           payment.Status == PaymentTransactionStatus.Pending &&
                           payment.Order is EmployerSubscriptionOrder,
                cancellationToken))
        {
            throw new ConflictException(
                "Nhà tuyển dụng đang có giao dịch gói chờ thanh toán. Vui lòng hoàn tất hoặc hủy giao dịch trước khi điều chỉnh gói.");
        }
    }

    private void AddNotification(Guid employerId, string title, string content) =>
        context.Notifications.Add(new Notification { UserId = employerId, Title = title, Content = content });
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ManagedEmployerSubscriptionMapper
{
    public static partial IQueryable<ManagedEmployerSubscriptionResponseDto> ProjectToManagedDto(
        this IQueryable<EmployerSubscription> subscriptions);

    public static partial ManagedEmployerSubscriptionResponseDto MapToManagedDto(
        this EmployerSubscription subscription);
}
