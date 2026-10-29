using Contracts.Application.Manager;
using Domain;
using Domain.Models.EmployerSubscriptions;
using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Application.Manager;

public class SubscriptionPlanManagementService(ApplicationDbContext context) : ISubscriptionPlanManagementService
{
    public async Task<List<ManagedSubscriptionPlanResponseDto>> GetPlansAsync(
        CancellationToken cancellationToken = default) =>
        await context.EmployerSubscriptionPlans.AsNoTracking()
            .OrderBy(plan => plan.Tier)
            .ThenBy(plan => plan.Duration)
            .ProjectToManagedPlanDto()
            .ToListAsync(cancellationToken);

    public async Task<ManagedSubscriptionPlanResponseDto> GetPlanAsync(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration, CancellationToken cancellationToken = default)
    {
        ValidateKey(tier, duration);
        return await context.EmployerSubscriptionPlans.AsNoTracking()
            .Where(plan => plan.Tier == tier && plan.Duration == duration)
            .ProjectToManagedPlanDto()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy gói đăng ký.");
    }

    public async Task<ManagedSubscriptionPlanResponseDto> CreatePlanAsync(CreateSubscriptionPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Tier is null || request.Duration is null)
        {
            throw new BadRequestException("Hạng gói và kỳ hạn gói là bắt buộc.");
        }

        ValidateKey(request.Tier.Value, request.Duration.Value);
        var name = ValidateName(request.Name);
        ValidateValues(request);

        if (await context.EmployerSubscriptionPlans.AnyAsync(
                plan => plan.Tier == request.Tier.Value && plan.Duration == request.Duration.Value,
                cancellationToken))
        {
            throw new ConflictException("Gói đăng ký với hạng và kỳ hạn này đã tồn tại.");
        }

        var plan = new EmployerSubscriptionPlan
        {
            Tier = request.Tier.Value,
            Duration = request.Duration.Value,
            Name = name,
            Description = NormalizeDescription(request.Description),
            Price = request.Price,
            DurationDays = request.DurationDays,
            ExtraActivePosts = request.ExtraActivePosts,
            FreeBumps = request.FreeBumps,
            CanRecommendCandidates = request.CanRecommendCandidates,
            CanQueryCandidates = request.CanQueryCandidates,
            IsActive = request.IsActive
        };
        context.EmployerSubscriptionPlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
        return plan.MapToManagedPlanDto();
    }

    public async Task<ManagedSubscriptionPlanResponseDto> UpdatePlanAsync(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration, UpdateSubscriptionPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(tier, duration);
        var name = ValidateName(request.Name);
        ValidateValues(request);

        var plan = await context.EmployerSubscriptionPlans.FindAsync([tier, duration], cancellationToken)
                   ?? throw new NotFoundException("Không tìm thấy gói đăng ký cần cập nhật.");

        plan.Name = name;
        plan.Description = NormalizeDescription(request.Description);
        plan.Price = request.Price;
        plan.DurationDays = request.DurationDays;
        plan.ExtraActivePosts = request.ExtraActivePosts;
        plan.FreeBumps = request.FreeBumps;
        plan.CanRecommendCandidates = request.CanRecommendCandidates;
        plan.CanQueryCandidates = request.CanQueryCandidates;
        plan.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return plan.MapToManagedPlanDto();
    }

    public async Task DeactivatePlanAsync(EmployerSubscriptionTier tier,
        EmployerSubscriptionDuration duration, CancellationToken cancellationToken = default)
    {
        ValidateKey(tier, duration);
        var plan = await context.EmployerSubscriptionPlans.FindAsync([tier, duration], cancellationToken)
                   ?? throw new NotFoundException("Không tìm thấy gói đăng ký cần ngừng cung cấp.");

        if (!plan.IsActive) return;
        plan.IsActive = false;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateKey(EmployerSubscriptionTier tier, EmployerSubscriptionDuration duration)
    {
        if (!Enum.IsDefined(tier) || !Enum.IsDefined(duration))
        {
            throw new BadRequestException("Hạng gói hoặc kỳ hạn gói không hợp lệ.");
        }
    }

    private static string ValidateName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new BadRequestException("Tên gói là bắt buộc.");
        }

        if (trimmed.Length > BusinessRuleConstants.Models.EmployerSubscriptionPlan.NameMaxLength)
        {
            throw new BadRequestException("Tên gói quá dài.");
        }

        return trimmed;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void ValidateValues(UpdateSubscriptionPlanRequestDto request)
    {
        if (request.Price <= 0 || request.Price > 9999999999999999.99m ||
            request.DurationDays is <= 0 or > BusinessRuleConstants.Models.EmployerSubscriptionPlan.MaxDurationDays ||
            request.ExtraActivePosts < 0 ||
            request.ExtraActivePosts > int.MaxValue - BusinessRuleConstants.Employers.DefaultMaxActivePosts ||
            request.FreeBumps < 0 ||
            request.Description?.Length > BusinessRuleConstants.Models.EmployerSubscriptionPlan.DescriptionMaxLength)
        {
            throw new BadRequestException("Thông tin giá, thời hạn hoặc quyền lợi gói không hợp lệ.");
        }
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ManagedSubscriptionPlanMapper
{
    public static partial IQueryable<ManagedSubscriptionPlanResponseDto> ProjectToManagedPlanDto(
        this IQueryable<EmployerSubscriptionPlan> plans);
    public static partial ManagedSubscriptionPlanResponseDto MapToManagedPlanDto(this EmployerSubscriptionPlan plan);
}
