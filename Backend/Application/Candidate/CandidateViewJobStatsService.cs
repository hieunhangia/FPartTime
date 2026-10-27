using System.Security.Claims;
using Contracts.Application;
using Contracts.Application.Candidate;
using Contracts.External.Payments;
using Domain;
using Domain.Models.CandidateViewJobStats;
using Domain.Models.Orders;
using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Riok.Mapperly.Abstractions;

namespace Application.Candidate;

public class CandidateViewJobStatsService(
    ApplicationDbContext context,
    IPaymentServiceFactory paymentServiceFactory,
    INotificationsService notificationsService,
    IFrontendUrlBuilder frontendUrlBuilder,
    ILogger<CandidateViewJobStatsService> logger) : ICandidateViewJobStatsService
{
    public async Task<CandidateViewJobStatsBalanceResponseDto> GetBalanceAsync(ClaimsPrincipal user) =>
        new()
        {
            Balance = (await context.Candidates.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == user.GetUserId()))?.ViewJobStatsBalance ?? 0
        };

    public async Task<List<CandidateViewJobStatsPackageResponseDto>> GetPackagesAsync() =>
        await context.CandidateViewJobStatsPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ProjectToDto()
            .ToListAsync();

    public async Task<CandidateJobStatsResponseDto> GetJobStatsAsync(ClaimsPrincipal user, Guid jobId)
    {
        var userId = user.GetUserId();
        var candidate = await context.Candidates.AsNoTracking()
                            .FirstOrDefaultAsync(c => c.UserId == userId)
                        ?? throw new ForbiddenException("Không tìm thấy thông tin ứng viên.");

        var job = await context.Jobs.AsNoTracking()
                      .FirstOrDefaultAsync(j => j.Id == jobId)
                  ?? throw new NotFoundException("Không tìm thấy công việc.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isJobActiveAndOpen = job.Status == JobStatus.Active && job.ExpiryAt >= today;

        var snapshot = await context.CandidateViewJobStatsSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(s => s.CandidateId == userId && s.JobId == jobId);

        return new CandidateJobStatsResponseDto
        {
            JobId = jobId,
            HasUnlocked = snapshot != null,
            CurrentBalance = candidate.ViewJobStatsBalance,
            IsJobActiveAndOpen = isJobActiveAndOpen,
            Snapshot = snapshot?.MapToDto()
        };
    }

    public async Task<CandidateJobStatsResponseDto> UnlockOrRefreshJobStatsAsync(ClaimsPrincipal user, Guid jobId)
    {
        var userId = user.GetUserId();
        var candidate = await context.Candidates
                            .FirstOrDefaultAsync(c => c.UserId == userId)
                        ?? throw new ForbiddenException("Không tìm thấy thông tin ứng viên.");

        var job = await context.Jobs.AsNoTracking()
                      .FirstOrDefaultAsync(j => j.Id == jobId)
                  ?? throw new NotFoundException("Không tìm thấy công việc.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (job.Status != JobStatus.Active || job.ExpiryAt < today)
        {
            throw new BadRequestException(
                "Bài đăng tuyển dụng đã đóng hoặc hết hạn, không thể mở khóa hoặc cập nhật số liệu mới.");
        }

        if (candidate.ViewJobStatsBalance <= 0)
        {
            throw new BadRequestException("Bạn đã hết lượt xem thống kê. Vui lòng mua thêm gói lượt xem.");
        }

        candidate.ViewJobStatsBalance--;

        var apps = await context.JobApplications.AsNoTracking()
            .Where(a => a.JobId == jobId)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var pending = apps.FirstOrDefault(g => g.Status == JobApplicationStatus.Pending)?.Count ?? 0;
        var accepted = apps.FirstOrDefault(g => g.Status == JobApplicationStatus.Accepted)?.Count ?? 0;
        var rejected = apps.FirstOrDefault(g => g.Status == JobApplicationStatus.Rejected)?.Count ?? 0;
        var total = pending + accepted + rejected;
        var now = DateTime.UtcNow;

        var snapshot = await context.CandidateViewJobStatsSnapshots
            .FirstOrDefaultAsync(s => s.CandidateId == userId && s.JobId == jobId);

        if (snapshot == null)
        {
            snapshot = new CandidateViewJobStatsSnapshot
            {
                CandidateId = userId,
                JobId = jobId,
                TotalApplications = total,
                PendingApplications = pending,
                AcceptedApplications = accepted,
                RejectedApplications = rejected,
            };
            context.CandidateViewJobStatsSnapshots.Add(snapshot);
        }
        else
        {
            snapshot.TotalApplications = total;
            snapshot.PendingApplications = pending;
            snapshot.AcceptedApplications = accepted;
            snapshot.RejectedApplications = rejected;
            snapshot.SnapshotAt = now;
            snapshot.UpdatedAt = now;
        }

        await context.SaveChangesAsync();

        return new CandidateJobStatsResponseDto
        {
            JobId = jobId,
            HasUnlocked = true,
            CurrentBalance = candidate.ViewJobStatsBalance,
            IsJobActiveAndOpen = true,
            Snapshot = snapshot.MapToDto()
        };
    }

    public async Task<CandidateViewJobStatsCheckoutResponseDto> CreatePayOSCheckoutAsync(ClaimsPrincipal user,
        CreateCandidateViewPayOSCheckoutRequestDto request) =>
        await CreateCheckoutBaseAsync(user, request, PaymentProvider.PayOS, (packageName, price) =>
            new CreatePayOSPaymentRequest
            {
                Amount = price,
                Description = $"Mua {packageName}",
                CancelUrl = frontendUrlBuilder.BuildUrl(request.CancelPath),
                ReturnUrl = frontendUrlBuilder.BuildUrl(request.ReturnPath),
                ExpiredAt = DateTime.UtcNow.AddMinutes(
                    BusinessRuleConstants.Candidates.ViewJobStats.PaymentExpiresInMinutes),
                Items = [new CreatePayOSPaymentRequest.PaymentItem { Name = packageName, Price = price, Quantity = 1 }]
            });

    public async Task<CandidateViewJobStatsCheckoutResponseDto> CreateVNPAYCheckoutAsync(ClaimsPrincipal user,
        string clientIpAddress, CreateCandidateViewVNPAYCheckoutRequestDto request) =>
        await CreateCheckoutBaseAsync(user, request, PaymentProvider.VNPAY, (packageName, price) =>
            new CreateVNPAYPaymentRequest
            {
                Amount = price,
                Description = $"Mua {packageName}",
                ReturnUrl = frontendUrlBuilder.BuildUrl(request.ReturnPath),
                IpAddress = clientIpAddress,
                ExpiredAt = DateTime.UtcNow.AddMinutes(
                    BusinessRuleConstants.Candidates.ViewJobStats.PaymentExpiresInMinutes)
            });

    public async Task<CandidateViewJobStatsCheckoutResponseDto> CreateZaloPayCheckoutAsync(ClaimsPrincipal user,
        CreateCandidateViewZaloPayCheckoutRequestDto request) =>
        await CreateCheckoutBaseAsync(user, request, PaymentProvider.ZaloPay, (packageName, price) =>
            new CreateZaloPayPaymentRequest
            {
                Amount = price,
                Description = $"Mua {packageName}",
                ReturnUrl = frontendUrlBuilder.BuildUrl(request.ReturnPath),
                AppUser = user.GetUserId().ToString(),
                ExpiredAt = DateTime.UtcNow.AddMinutes(
                    BusinessRuleConstants.Candidates.ViewJobStats.PaymentExpiresInMinutes),
                Items =
                [
                    new CreateZaloPayPaymentRequest.PaymentItem { Name = packageName, Price = price, Quantity = 1 }
                ]
            });

    public async Task ProcessOrderPaymentAsync(Guid transactionId)
    {
        var transaction = await context.PaymentTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            logger.LogError("Không tìm thấy giao dịch với ID: {TransactionId}", transactionId);
            return;
        }

        if (transaction.Status != PaymentTransactionStatus.Pending &&
            transaction.Status != PaymentTransactionStatus.Expired)
        {
            logger.LogError("Giao dịch {TransactionId} đã được xử lý trước đó với trạng thái {Status}", transactionId,
                transaction.Status);
            await notificationsService.CreateNotificationAsync(transaction.UserId,
                "Cộng lượt xem không thành công",
                $"Giao dịch {transactionId} đã thanh toán thành công nhưng trạng thái giao dịch không hợp lệ ({transaction.Status}). Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
            return;
        }

        if (transaction.Order is not CandidateViewJobStatsOrder order)
        {
            logger.LogError("Không tìm thấy đơn hàng Mua lượt xem cho giao dịch {TransactionId}", transactionId);
            await notificationsService.CreateNotificationAsync(transaction.UserId,
                "Cộng lượt xem không thành công",
                $"Không tìm thấy đơn hàng Mua lượt xem cho giao dịch {transactionId}. Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
            return;
        }

        if (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.Expired)
        {
            logger.LogError("Đơn hàng {OrderId} đã được xử lý trước đó với trạng thái {Status}", order.Id,
                order.Status);
            await notificationsService.CreateNotificationAsync(order.UserId,
                "Cộng lượt xem không thành công",
                $"Đơn hàng #{order.Id} đã thanh toán thành công nhưng trạng thái đơn hàng không hợp lệ ({order.Status}). Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
            return;
        }

        var now = DateTime.UtcNow;
        transaction.Status = PaymentTransactionStatus.Paid;
        transaction.PaidAt = now;
        order.Status = OrderStatus.Completed;
        order.CompletedAt = now;

        var candidate = await context.Candidates.FirstOrDefaultAsync(c => c.UserId == order.UserId);
        if (candidate == null)
        {
            logger.LogError("Không tìm thấy thông tin ứng viên cho đơn hàng {OrderId} (UserId: {UserId})",
                order.Id, order.UserId);
            await notificationsService.CreateNotificationAsync(transaction.UserId,
                "Cộng lượt xem không thành công",
                $"Đơn hàng #{order.Id} đã thanh toán thành công ({transaction.Amount:N0} đ) nhưng không tìm thấy thông tin ứng viên. Vui lòng liên hệ bộ phận hỗ trợ để được trợ giúp.");
            return;
        }

        candidate.ViewJobStatsBalance += order.ViewCredits;
        await context.SaveChangesAsync();

        await notificationsService.CreateNotificationAsync(
            candidate.UserId,
            "Mua lượt xem thành công",
            $"Bạn đã mua thành công gói '{order.PackageName}'. Tài khoản được cộng {order.ViewCredits} lượt xem số lượng đơn ứng tuyển. Số dư hiện tại: {candidate.ViewJobStatsBalance} lượt.");
    }

    private async Task<CandidateViewJobStatsCheckoutResponseDto> CreateCheckoutBaseAsync(
        ClaimsPrincipal user,
        CandidateViewCheckoutRequestBase request,
        PaymentProvider provider,
        Func<string, decimal, CreatePaymentRequestBase> buildPaymentRequest)
    {
        var userId = user.GetUserId();

        var candidate = await context.Candidates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (candidate == null)
        {
            throw new ForbiddenException("Chỉ ứng viên mới có thể mua gói lượt xem.");
        }

        var package = await context.CandidateViewJobStatsPackages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PackageId && p.IsActive);
        if (package == null)
        {
            throw new NotFoundException("Gói lượt xem không tồn tại hoặc đã ngừng cung cấp.");
        }

        var newPaymentRequest = buildPaymentRequest(package.Name, package.Price);
        var newPaymentResponse = await paymentServiceFactory.Get(provider).CreatePaymentAsync(newPaymentRequest);

        var order = new CandidateViewJobStatsOrder
        {
            UserId = userId,
            TotalAmount = package.Price,
            Description = newPaymentRequest.Description,
            PackageId = package.Id,
            PackageName = package.Name,
            ViewCredits = package.ViewCredits,
            ExpiredAt = newPaymentRequest.ExpiredAt
        };

        context.PaymentTransactions.Add(new PaymentTransaction
        {
            Order = order,
            UserId = userId,
            Provider = provider,
            PaymentReferenceId = newPaymentResponse.PaymentReferenceId,
            PaymentUrl = newPaymentResponse.PaymentUrl,
            Amount = package.Price,
            ExpiredAt = newPaymentRequest.ExpiredAt
        });

        await context.SaveChangesAsync();

        return new CandidateViewJobStatsCheckoutResponseDto
        {
            PaymentUrl = newPaymentResponse.PaymentUrl
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class CandidateViewJobStatsMapper
{
    public static partial IQueryable<CandidateViewJobStatsPackageResponseDto> ProjectToDto(
        this IQueryable<CandidateViewJobStatsPackage> packages);

    public static partial CandidateJobStatsSnapshotDto MapToDto(this CandidateViewJobStatsSnapshot snapshot);
}