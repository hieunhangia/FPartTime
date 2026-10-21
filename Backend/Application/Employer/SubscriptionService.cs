using System.Security.Claims;
using Contracts.Application;
using Contracts.Application.Employer;
using Contracts.External;
using Domain;
using Domain.Models.EmployerSubscriptions;
using Domain.Models.Orders;
using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace Application.Employer;

public class SubscriptionService(
    ApplicationDbContext context,
    IPayOSPaymentService payOsPaymentService,
    IVNPAYPaymentService vnpayPaymentService,
    IPaymentTransactionService paymentTransactionService,
    INotificationsService notificationsService,
    IFrontendUrlBuilder frontendUrlBuilder,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    public async Task<List<EmployerSubscriptionPlanResponseDto>> GetPlansAsync() =>
        await context.EmployerSubscriptionPlans.AsNoTracking()
            .Where(p => p.IsActive)
            .ProjectToDto()
            .ToListAsync();

    public async Task<CurrentEmployerSubscriptionResponseDto> GetCurrentSubscriptionAsync(ClaimsPrincipal user) =>
        (await context.EmployerSubscriptions.AsNoTracking()
            .Where(s => s.EmployerId == user.GetUserId() &&
                        s.Status == EmployerSubscriptionStatus.Active && s.EndDate >= DateTime.UtcNow)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync()).MapToDto() ?? new CurrentEmployerSubscriptionResponseDto
        {
            HasActiveSubscription = false
        };

    public async Task CancelCurrentSubscriptionAsync(ClaimsPrincipal user)
    {
        var userId = user.GetUserId();
        var now = DateTime.UtcNow;
        var activeSub = await context.EmployerSubscriptions
            .Where(s => s.EmployerId == userId && s.Status == EmployerSubscriptionStatus.Active && s.EndDate >= now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (activeSub == null)
        {
            throw new BadRequestException("Bạn không có gói đăng ký nào đang hoạt động để hủy.");
        }

        activeSub.Status = EmployerSubscriptionStatus.Cancelled;

        var employer = await context.Employers.FirstOrDefaultAsync(e => e.UserId == userId)
                       ?? throw new NotFoundException(
                           "Không tìm thấy thông tin nhà tuyển dụng. Liên hệ bộ phận hỗ trợ để được trợ giúp.");

        employer.MaxActivePosts = BusinessRuleConstants.Employers.DefaultMaxActivePosts;

        var activeJobs = await context.Jobs
            .Where(j => j.EmployerId == userId && j.Status == JobStatus.Active)
            .OrderBy(j => j.UpdatedAt)
            .ToListAsync();

        if (activeJobs.Count > BusinessRuleConstants.Employers.DefaultMaxActivePosts)
        {
            var excessCount = activeJobs.Count - BusinessRuleConstants.Employers.DefaultMaxActivePosts;
            var jobsToClose = activeJobs.Take(excessCount).ToList();
            foreach (var job in jobsToClose)
            {
                job.Status = JobStatus.Closed;
                job.UpdatedAt = now;
            }

            await notificationsService.CreateNotificationAsync(userId, "Gói đăng ký đã bị hủy",
                $"Bạn đã hủy gói đăng ký {activeSub.Tier} ({activeSub.Duration}). Hệ thống đã tự động đóng {excessCount} tin tuyển dụng có thời gian cập nhật cũ nhất để phù hợp với hạn mức mặc định ({BusinessRuleConstants.Employers.DefaultMaxActivePosts} tin).");
        }
        else
        {
            await notificationsService.CreateNotificationAsync(userId, "Gói đăng ký đã bị hủy",
                $"Bạn đã hủy gói đăng ký {activeSub.Tier} ({activeSub.Duration}). Hạn mức đăng tin đã trở về mặc định ({BusinessRuleConstants.Employers.DefaultMaxActivePosts} tin).");
        }

        await context.SaveChangesAsync();
    }

    public async Task<CreatePayOSCheckoutResponseDto> CreatePayOSCheckoutAsync(ClaimsPrincipal user,
        CreatePayOSCheckoutRequestDto request) => new()
    {
        PaymentUrl = await CreateCheckoutAsync(
            user,
            request.Tier,
            request.Duration,
            async plan =>
            {
                var response = await payOsPaymentService.CreatePaymentAsync(new CreatePayOSPaymentRequest
                {
                    Amount = plan.Price,
                    Description = $"{plan.Tier} {plan.Duration}",
                    CancelUrl = frontendUrlBuilder.BuildUrl(request.CancelPath),
                    ReturnUrl = frontendUrlBuilder.BuildUrl(request.ReturnPath),
                    ExpiredAt = DateTime.UtcNow.AddMinutes(BusinessRuleConstants.Employers.Subscriptions
                        .OrderExpiresInMinutes),
                    Items =
                    [
                        new CreatePayOSPaymentRequest.PaymentItem
                            { Name = plan.Name, Price = plan.Price, Quantity = 1 }
                    ]
                });
                return (response.PaymentReferenceId, response.PaymentUrl);
            })
    };

    public async Task CancelPayOSCheckoutAsync(ClaimsPrincipal user, Guid transactionId)
    {
        var transaction = await context.PaymentTransactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            throw new NotFoundException("Không tìm thấy giao dịch thanh toán.");
        }

        if (transaction.UserId != user.GetUserId())
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
        }

        switch (transaction.Status)
        {
            case PaymentTransactionStatus.Paid:
                throw new BadRequestException("Giao dịch đã được thanh toán thành công, không thể hủy.");
            case PaymentTransactionStatus.Cancelled:
                throw new BadRequestException("Giao dịch đã được hủy trước đó.");
            case PaymentTransactionStatus.Expired:
                throw new BadRequestException("Giao dịch thanh toán đã hết hạn.");
            case PaymentTransactionStatus.Failed:
                throw new BadRequestException("Giao dịch thanh toán đã thất bại, không thể hủy.");
            case PaymentTransactionStatus.Pending:
                break;
            default:
                throw new BadRequestException("Trạng thái giao dịch không hợp lệ để hủy.");
        }

        if (transaction.ExpiredAt <= DateTime.UtcNow)
        {
            await context.PaymentTransactions
                .Where(t => t.Id == transaction.Id && t.Status == PaymentTransactionStatus.Pending)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, PaymentTransactionStatus.Expired));

            throw new BadRequestException("Giao dịch thanh toán đã hết hạn.");
        }

        await payOsPaymentService.CancelPaymentAsync(transaction.PaymentReferenceId);
        await paymentTransactionService.CancelPaymentTransactionAsync(transaction.Id);
    }

    public async Task<CreateVNPAYCheckoutResponseDto> CreateVNPAYCheckoutAsync(ClaimsPrincipal user,
        CreateVNPAYCheckoutRequestDto request) => new()
    {
        PaymentUrl = await CreateCheckoutAsync(
            user,
            request.Tier,
            request.Duration,
            async plan =>
            {
                var response = await vnpayPaymentService.CreatePaymentAsync(new CreateVNPAYPaymentRequest
                {
                    Amount = plan.Price,
                    Description = $"Dang ki goi {plan.Tier} {plan.Duration}",
                    ReturnUrl = frontendUrlBuilder.BuildUrl(request.ReturnPath),
                    IpAddress = request.IpAddress,
                    ExpiredAt = DateTime.UtcNow.AddMinutes(BusinessRuleConstants.Employers.Subscriptions
                        .OrderExpiresInMinutes)
                });
                return (response.PaymentReferenceId, response.PaymentUrl);
            })
    };

    public async Task ProcessSubscriptionOrderAsync(Guid transactionId)
    {
        var transaction = await context.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == transactionId);
        if (transaction == null)
        {
            logger.LogError("Không tìm thấy giao dịch với ID: {TransactionId}", transactionId);
            return;
        }

        var order = context.EmployerSubscriptionOrders
            .Include(o => o.Employer)
            .FirstOrDefault(o => o.PaymentTransactionId == transactionId);
        if (order == null)
        {
            logger.LogError("Không tìm thấy đơn hàng Gói đăng ký cho giao dịch {TransactionId}", transactionId);
            return;
        }

        if (transaction.Status != PaymentTransactionStatus.Pending)
        {
            logger.LogError("Giao dịch {TransactionId} đã được xử lý trước đó với trạng thái {Status}", transactionId,
                transaction.Status);
            await notificationsService.CreateNotificationAsync(order.EmployerId,
                "Kích hoạt gói dịch vụ không thành công",
                $"Đơn hàng #{transactionId} đã thanh toán thành công nhưng trạng thái đơn hàng không hợp lệ ({transaction.Status}). Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
            return;
        }

        var now = DateTime.UtcNow;
        var employer = order.Employer;
        if (employer == null)
        {
            logger.LogError("Không tìm thấy thông tin nhà tuyển dụng cho đơn hàng {OrderId} (EmployerId: {EmployerId})",
                order.Id, order.EmployerId);
            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.PaidAt = now;
            await context.SaveChangesAsync();

            await notificationsService.CreateNotificationAsync(order.EmployerId,
                "Kích hoạt gói dịch vụ không thành công",
                $"Đơn hàng #{transactionId} đã thanh toán thành công ({transaction.Amount:N0} đ) nhưng không tìm thấy thông tin nhà tuyển dụng. Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
            return;
        }

        var activeSub = await context.EmployerSubscriptions
            .Where(s => s.EmployerId == order.EmployerId && s.Status == EmployerSubscriptionStatus.Active &&
                        s.EndDate >= now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (activeSub != null && (activeSub.Tier != order.Tier || activeSub.Duration != order.Duration))
        {
            logger.LogError(
                "Đơn hàng {OrderId} thanh toán thành công nhưng gói hiện tại ({ActiveTier} {ActiveDuration}) không khớp với gói mới ({NewTier} {NewDuration})",
                order.Id, activeSub.Tier, activeSub.Duration, order.Tier, order.Duration);

            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.PaidAt = now;
            await context.SaveChangesAsync();

            await notificationsService.CreateNotificationAsync(order.EmployerId,
                "Kích hoạt gói dịch vụ không thành công",
                $"Đơn hàng #{transactionId} đã thanh toán thành công ({transaction.Amount:N0} đ) nhưng chưa thể kích hoạt do bạn đang có gói {activeSub.Tier} ({activeSub.Duration}) còn hạn đến {activeSub.EndDate:dd/MM/yyyy HH:mm} (UTC). Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
            return;
        }

        transaction.Status = PaymentTransactionStatus.Paid;
        transaction.PaidAt = now;
        employer.BumpBalance += order.FreeBumps;
        employer.MaxActivePosts = BusinessRuleConstants.Employers.DefaultMaxActivePosts + order.ExtraActivePosts;

        if (activeSub == null)
        {
            context.EmployerSubscriptions.Add(new EmployerSubscription
            {
                EmployerId = order.EmployerId,
                Tier = order.Tier,
                Duration = order.Duration,
                EndDate = now.AddDays(order.DurationDays),
                Status = EmployerSubscriptionStatus.Active,
                ExtraActivePosts = order.ExtraActivePosts,
                CanRecommendCandidates = order.CanRecommendCandidates,
                CanQueryCandidates = order.CanQueryCandidates
            });
        }
        else
        {
            activeSub.EndDate = activeSub.EndDate.AddDays(order.DurationDays);
            activeSub.ExtraActivePosts = order.ExtraActivePosts;
            activeSub.CanRecommendCandidates = order.CanRecommendCandidates;
            activeSub.CanQueryCandidates = order.CanQueryCandidates;
        }

        await context.SaveChangesAsync();
        await notificationsService.CreateNotificationAsync(order.EmployerId, "Đăng ký gói thành công",
            $"Bạn đã kích hoạt thành công gói {order.PlanName}. Hạn mức tin đăng tối đa: {employer.MaxActivePosts}, số lượt đẩy tin hiện có: {employer.BumpBalance}.");
    }

    public async Task<int> ProcessExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredSubs = await context.EmployerSubscriptions
            .Include(s => s.Employer)
            .Where(s => s.Status == EmployerSubscriptionStatus.Active && s.EndDate < now)
            .ToListAsync(cancellationToken);

        if (expiredSubs.Count == 0)
        {
            return 0;
        }

        foreach (var sub in expiredSubs)
        {
            sub.Status = EmployerSubscriptionStatus.Expired;
        }

        var employerIds = expiredSubs.Select(s => s.EmployerId).Distinct().ToList();

        foreach (var employerId in employerIds)
        {
            var hasActiveSub = await context.EmployerSubscriptions.AnyAsync(
                s => s.EmployerId == employerId &&
                     s.Status == EmployerSubscriptionStatus.Active &&
                     s.EndDate >= now,
                cancellationToken);

            if (hasActiveSub)
            {
                continue;
            }

            var employer = await context.Employers.FirstOrDefaultAsync(e => e.UserId == employerId, cancellationToken);
            if (employer == null)
            {
                continue;
            }

            employer.MaxActivePosts = BusinessRuleConstants.Employers.DefaultMaxActivePosts;

            var activeJobs = await context.Jobs
                .Where(j => j.EmployerId == employerId && j.Status == JobStatus.Active)
                .OrderBy(j => j.UpdatedAt)
                .ToListAsync(cancellationToken);

            if (activeJobs.Count > BusinessRuleConstants.Employers.DefaultMaxActivePosts)
            {
                var excessCount = activeJobs.Count - BusinessRuleConstants.Employers.DefaultMaxActivePosts;
                var jobsToClose = activeJobs.Take(excessCount).ToList();
                foreach (var job in jobsToClose)
                {
                    job.Status = JobStatus.Closed;
                    job.UpdatedAt = now;
                }

                await notificationsService.CreateNotificationAsync(employerId, "Gói đăng ký đã hết hạn",
                    $"Gói đăng ký của bạn đã hết hạn. Hệ thống đã tự động đóng {excessCount} tin tuyển dụng có thời gian cập nhật cũ nhất để phù hợp với hạn mức mặc định ({BusinessRuleConstants.Employers.DefaultMaxActivePosts} tin).");
            }
            else
            {
                await notificationsService.CreateNotificationAsync(employerId, "Gói đăng ký đã hết hạn",
                    $"Gói đăng ký của bạn đã hết hạn. Hạn mức đăng tin đã trở về mặc định ({BusinessRuleConstants.Employers.DefaultMaxActivePosts} tin).");
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return expiredSubs.Count;
    }

    private async Task<string> CreateCheckoutAsync(ClaimsPrincipal user,
        EmployerSubscriptionTier? tier, EmployerSubscriptionDuration? duration,
        Func<EmployerSubscriptionPlan, Task<(string PaymentReferenceId, string PaymentUrl)>> createPaymentFunc)
    {
        var userId = user.GetUserId();
        var employer = await context.Employers.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null)
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
        }

        if (await context.EmployerSubscriptionOrders.AnyAsync(o =>
                o.EmployerId == userId && o.PaymentTransaction != null &&
                o.PaymentTransaction.ExpiredAt > DateTime.UtcNow &&
                o.PaymentTransaction.Status == PaymentTransactionStatus.Pending))
        {
            throw new BadRequestException(
                "Bạn đang có đơn hàng Gói đăng ký chờ thanh toán. Vui lòng hoàn tất hoặc hủy các đơn hàng đó trước khi tạo đơn mới.");
        }

        if (!tier.HasValue || !duration.HasValue)
        {
            throw new BadRequestException("Thông tin đăng ký gói không đầy đủ.");
        }

        var plan = await context.EmployerSubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Tier == tier.Value && p.Duration == duration.Value && p.IsActive);
        if (plan == null)
        {
            throw new BadRequestException("Gói đăng ký không tồn tại hoặc đã ngừng cung cấp.");
        }

        var now = DateTime.UtcNow;
        var activeSub = await context.EmployerSubscriptions.AsNoTracking()
            .Where(s => s.EmployerId == userId && s.Status == EmployerSubscriptionStatus.Active && s.EndDate >= now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (activeSub != null && (activeSub.Tier != plan.Tier || activeSub.Duration != plan.Duration))
        {
            throw new BadRequestException(
                $"Bạn đang sử dụng gói {activeSub.Tier} ({activeSub.Duration}). Gói mới không trùng khớp với gói hiện tại. Bạn cần hủy gói cũ để đăng ký gói mới, hoặc gia hạn gói {activeSub.Tier} ({activeSub.Duration}).");
        }

        var (paymentReferenceId, paymentUrl) = await createPaymentFunc(plan);
        var paymentTransactionId = await paymentTransactionService.CreatePaymentTransactionAsync(userId,
            paymentReferenceId, plan.Price,
            DateTime.UtcNow.AddMinutes(BusinessRuleConstants.Employers.Subscriptions.OrderExpiresInMinutes));

        context.EmployerSubscriptionOrders.Add(new EmployerSubscriptionOrder
        {
            PaymentTransactionId = paymentTransactionId,
            EmployerId = userId,
            Tier = plan.Tier,
            Duration = plan.Duration,
            PlanName = plan.Name,
            DurationDays = plan.DurationDays,
            ExtraActivePosts = plan.ExtraActivePosts,
            FreeBumps = plan.FreeBumps,
            CanRecommendCandidates = plan.CanRecommendCandidates,
            CanQueryCandidates = plan.CanQueryCandidates
        });
        await context.SaveChangesAsync();
        return paymentUrl;
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class EmployerSubscriptionMapper
{
    public static partial IQueryable<EmployerSubscriptionPlanResponseDto> ProjectToDto(
        this IQueryable<EmployerSubscriptionPlan> plans);

    [MapProperty(nameof(EmployerSubscription), nameof(CurrentEmployerSubscriptionResponseDto.HasActiveSubscription),
        Use = nameof(HasActiveSubscription))]
    public static partial CurrentEmployerSubscriptionResponseDto? MapToDto(this EmployerSubscription? subscription);

    private static bool HasActiveSubscription(EmployerSubscription? subscription) => subscription != null;

    public static partial CreatePayOSCheckoutResponseDto MapToDto(this CreatePayOSPaymentResponse paymentResponse);
    public static partial CreateVNPAYCheckoutResponseDto MapToDto(this CreateVNPAYPaymentResponse paymentResponse);
}