using System.Security.Claims;
using Contracts.Application;
using Contracts.Application.Employer;
using Contracts.External.Payment;
using Domain;
using Domain.Models.EmployerSubscriptions;
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
    IPaymentServiceFactory paymentServiceFactory,
    IPaymentTransactionService paymentTransactionService,
    INotificationsService notificationsService,
    IFrontendUrlBuilder frontendUrlBuilder,
    ILogger<SubscriptionService> logger) : ISubscriptionService
{
    public async Task<List<EmployerSubscriptionPlanResponseDto>> GetPlansAsync() =>
        await context.EmployerSubscriptionPlans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
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

    public async Task<CreateSubscriptionCheckoutResponseDto> CreateCheckoutAsync(ClaimsPrincipal user,
        CreateSubscriptionCheckoutRequestDto request)
    {
        var userId = user.GetUserId();
        var employer = await context.Employers.AsNoTracking()
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId);
        if (employer == null)
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
        }

        if (await context.PaymentTransactions.AnyAsync(t =>
                t.UserId == userId &&
                t.Purpose == PaymentPurpose.EmployerSubscription &&
                t.Status == PaymentTransactionStatus.Pending &&
                t.ExpiredAt > DateTime.UtcNow))
        {
            throw new BadRequestException(
                "Bạn đang có đơn hàng Gói đăng ký chờ thanh toán. Vui lòng hoàn tất hoặc hủy các đơn hàng đó trước khi tạo đơn mới.");
        }

        if (!request.Tier.HasValue || !request.Duration.HasValue || !request.PaymentProvider.HasValue)
        {
            throw new BadRequestException("Thông tin đăng ký gói không đầy đủ.");
        }

        var tier = request.Tier.Value;
        var duration = request.Duration.Value;
        var provider = request.PaymentProvider.Value;

        var plan = await context.EmployerSubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Tier == tier && p.Duration == duration && p.IsActive);
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

        var description = $"Dang ki goi {plan.Tier} {plan.Duration}";
        var paymentResponse = await paymentServiceFactory.Get(provider)
            .CreatePaymentAsync(provider switch
            {
                PaymentProvider.PayOS => new PayOSCreatePaymentRequest
                {
                    Amount = plan.Price,
                    Description = description,
                    CancelUrl = frontendUrlBuilder.BuildUrl(request.CancelPath),
                    ReturnUrl = frontendUrlBuilder.BuildUrl(request.ReturnPath),
                    ExpiredAt = DateTime.UtcNow.AddMinutes(BusinessRuleConstants.Employers.Subscriptions
                        .OrderExpiresInMinutes),
                    OrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    BuyerPhone = employer.User?.PhoneNumber,
                    Items = [new PayOSPaymentItem { Name = plan.Name, Price = plan.Price, Quantity = 1 }]
                },
                _ => throw new BadRequestException("Phương thức thanh toán không hợp lệ hoặc không được hỗ trợ.")
            });

        var paymentTransactionId = await paymentTransactionService.CreatePaymentTransactionAsync(userId,
            PaymentPurpose.EmployerSubscription, provider, paymentResponse.PaymentReferenceId, plan.Price, description,
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
        return paymentResponse.MapToDto();
    }

    public async Task CancelCheckoutAsync(ClaimsPrincipal user, Guid transactionId)
    {
        var transaction = await context.PaymentTransactions.AsNoTracking()
            .Include(t => t.EmployerSubscriptionOrder)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            throw new NotFoundException("Không tìm thấy giao dịch thanh toán.");
        }

        if (transaction.UserId != user.GetUserId())
        {
            throw new ForbiddenException("Bạn không có quyền thực hiện hành động này.");
        }

        if (transaction.Purpose != PaymentPurpose.EmployerSubscription || transaction.EmployerSubscriptionOrder == null)
        {
            throw new BadRequestException("Giao dịch không phải là đơn hàng đăng ký gói dịch vụ.");
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

        if (transaction.ExpiredAt.HasValue && transaction.ExpiredAt.Value <= DateTime.UtcNow)
        {
            await context.PaymentTransactions
                .Where(t => t.Id == transaction.Id && t.Status == PaymentTransactionStatus.Pending)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, PaymentTransactionStatus.Expired));

            throw new BadRequestException("Giao dịch thanh toán đã hết hạn.");
        }

        await paymentServiceFactory.Get(transaction.Provider).CancelPaymentAsync(transaction.PaymentReferenceId);
        await paymentTransactionService.CancelPaymentTransactionAsync(transaction.Id);
    }

    public async Task ProcessIpnAsync(string paymentReferenceId)
    {
        var transaction = await context.PaymentTransactions
            .Include(t => t.EmployerSubscriptionOrder)
            .ThenInclude(o => o!.Employer)
            .FirstOrDefaultAsync(t => t.PaymentReferenceId == paymentReferenceId);

        if (transaction == null)
        {
            logger.LogError("Webhook thanh toán nhận mã tham chiếu không tồn tại trong hệ thống: {PaymentReferenceId}",
                paymentReferenceId);
            return;
        }

        if (transaction.Purpose != PaymentPurpose.EmployerSubscription)
        {
            logger.LogError(
                "Giao dịch {TransactionId} có mục đích thanh toán {Purpose} không khớp với {EmployerSubscription}",
                transaction.Id, transaction.Purpose, nameof(PaymentPurpose.EmployerSubscription));
            return;
        }

        var order = transaction.EmployerSubscriptionOrder;
        if (order == null)
        {
            logger.LogError(
                "Không tìm thấy đơn hàng đăng ký gói liên kết với giao dịch {TransactionId} (PaymentReferenceId: {PaymentReferenceId})",
                transaction.Id, paymentReferenceId);
            return;
        }

        if (transaction.Status != PaymentTransactionStatus.Pending)
        {
            logger.LogError(
                "Webhook thanh toán nhận mã tham chiếu {PaymentReferenceId} nhưng giao dịch {TransactionId} đã có trạng thái {Status}",
                paymentReferenceId, transaction.Id, transaction.Status);
            await notificationsService.CreateNotificationAsync(order.EmployerId,
                "Kích hoạt gói dịch vụ không thành công",
                $"Đơn hàng #{paymentReferenceId} đã thanh toán thành công nhưng trạng thái đơn hàng không hợp lệ ({transaction.Status}). Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
            return;
        }

        var now = DateTime.UtcNow;

        var employer = order.Employer ??
                       await context.Employers.FirstOrDefaultAsync(e => e.UserId == order.EmployerId);

        if (employer == null)
        {
            logger.LogError("Không tìm thấy thông tin nhà tuyển dụng cho đơn hàng {OrderId} (EmployerId: {EmployerId})",
                order.Id, order.EmployerId);
            transaction.Status = PaymentTransactionStatus.Failed;
            transaction.PaidAt = now;
            await context.SaveChangesAsync();

            await notificationsService.CreateNotificationAsync(order.EmployerId,
                "Kích hoạt gói dịch vụ không thành công",
                $"Đơn hàng #{paymentReferenceId} đã thanh toán thành công ({transaction.Amount:N0} đ) nhưng không tìm thấy thông tin nhà tuyển dụng. Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
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
                $"Đơn hàng #{paymentReferenceId} đã thanh toán thành công ({transaction.Amount:N0} đ) nhưng chưa thể kích hoạt do bạn đang có gói {activeSub.Tier} ({activeSub.Duration}) còn hạn đến {activeSub.EndDate:dd/MM/yyyy HH:mm} (UTC). Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
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

    public static partial CreateSubscriptionCheckoutResponseDto MapToDto(this CreatePaymentResponse paymentResponse);
}