using Contracts.Application.Admin;
using Contracts.Common;
using Domain;
using Domain.Models.Users;
using Domain.Shared.Constants;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Admin;

public class UserService(ApplicationDbContext context, UserManager<User> userManager) : IUserService
{
    private static readonly string[] ManagedRoles = [Role.Censor, Role.Employer, Role.Candidate];
    private static readonly string[] EditableRoles = [Role.Censor, Role.Candidate];

    public async Task<PagedResponseDto<UserDetailResponseDto>> GetUsersAsync(UserListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var roleFilter = request.Role is null
            ? null
            : ManagedRoles.FirstOrDefault(role => string.Equals(role, request.Role, StringComparison.OrdinalIgnoreCase));
        if (request.Role is not null && roleFilter is null)
            throw new BadRequestException("Chỉ có thể lọc theo quyền Censor, Employer hoặc Candidate.");

        var managedRoleIds = context.Roles.Where(role => ManagedRoles.Contains(role.Name!)).Select(role => role.Id);
        var query = context.Users.AsNoTracking().Where(user =>
            context.UserRoles.Any(userRole => userRole.UserId == user.Id &&
                managedRoleIds.Contains(userRole.RoleId)) &&
            !context.UserRoles.Any(userRole => userRole.UserId == user.Id &&
                !managedRoleIds.Contains(userRole.RoleId)));

        if (roleFilter is not null)
            query = query.Where(user => context.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && context.Roles.Any(role =>
                    role.Id == userRole.RoleId && role.Name == roleFilter)));

        if (!string.IsNullOrWhiteSpace(request.PhoneNumberPrefix))
        {
            var prefix = request.PhoneNumberPrefix.Trim();
            query = query.Where(user => user.PhoneNumber != null && user.PhoneNumber.StartsWith(prefix));
        }

        var totalCount = await query.LongCountAsync(cancellationToken);
        var skip = (long)(request.PageNumber - 1) * request.PageSize;
        if (skip > int.MaxValue)
            return new PagedResponseDto<UserDetailResponseDto>
            {
                TotalCount = totalCount,
                Items = []
            };

        var users = await query.OrderBy(user => user.PhoneNumber).ThenBy(user => user.Id)
            .Skip((int)skip).Take(request.PageSize).ToListAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToArray();
        var userRoles = await (from userRole in context.UserRoles.AsNoTracking()
            join role in context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, role.Name }).ToListAsync(cancellationToken);
        var rolesByUser = userRoles.ToLookup(item => item.UserId, item => item.Name!);

        return new PagedResponseDto<UserDetailResponseDto>
        {
            TotalCount = totalCount,
            Items = users.Select(user => new UserDetailResponseDto
            {
                Id = user.Id,
                PhoneNumber = user.PhoneNumber,
                Roles = rolesByUser[user.Id].ToArray(),
                IsLockedOut = user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow,
                LockoutEndUtc = user.LockoutEnd?.ToUniversalTime()
            }).ToArray()
        };
    }

    public async Task<UserDetailResponseDto> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetManagedUserAsync(id, cancellationToken);
        return await ToResponseAsync(user);
    }

    public async Task<UserDetailResponseDto> UpdateRolesAsync(Guid id, UpdateUserRolesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Roles is null)
            throw new BadRequestException("Danh sách quyền là bắt buộc.");

        var roles = request.Roles.Select(role =>
            EditableRoles.FirstOrDefault(allowed => string.Equals(allowed, role, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        if (roles.Any(role => role is null) ||
            roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() != roles.Length)
            throw new BadRequestException("Chỉ chấp nhận các quyền Censor, Candidate và không được trùng lặp.");

        var requestedRoles = roles.Select(role => role!).ToArray();
        var user = await GetManagedUserAsync(id, cancellationToken);
        var currentRoles = await userManager.GetRolesAsync(user);

        var preservedRoles = currentRoles.Except(EditableRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (requestedRoles.Length == 0 && preservedRoles.Length == 0)
            throw new BadRequestException("Tài khoản phải có ít nhất một quyền.");

        var rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToRemove = currentRoles.Intersect(EditableRoles, StringComparer.OrdinalIgnoreCase)
            .Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToArray();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (requestedRoles.Contains(Role.Censor) &&
            !await context.Censors.AnyAsync(profile => profile.UserId == id, cancellationToken))
            context.Censors.Add(new CensorProfile { UserId = id });

        if (requestedRoles.Contains(Role.Candidate) &&
            !await context.Candidates.AnyAsync(profile => profile.UserId == id, cancellationToken))
            context.Candidates.Add(new CandidateProfile
            {
                UserId = id,
                AvatarFilePath = BusinessRuleConstants.DefaultAvatarFilePath
            });

        await context.SaveChangesAsync(cancellationToken);
        if (rolesToAdd.Length > 0)
            ThrowIfIdentityFailed(await userManager.AddToRolesAsync(user, rolesToAdd));
        if (rolesToRemove.Length > 0)
            ThrowIfIdentityFailed(await userManager.RemoveFromRolesAsync(user, rolesToRemove));
        await transaction.CommitAsync(cancellationToken);

        return await ToResponseAsync(user);
    }

    public async Task<UserDetailResponseDto> LockUserAsync(Guid id, LockUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.LockoutEndUtc is not { } end || end.UtcDateTime <= DateTime.UtcNow)
            throw new BadRequestException("Thời điểm hết khóa phải ở tương lai.");

        var user = await GetManagedUserAsync(id, cancellationToken);
        user.LockoutEnabled = true;
        user.LockoutEnd = end.ToUniversalTime();
        ThrowIfIdentityFailed(await userManager.UpdateAsync(user));
        return await ToResponseAsync(user);
    }

    public async Task<UserDetailResponseDto> UnlockUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetManagedUserAsync(id, cancellationToken);
        user.LockoutEnd = null;
        ThrowIfIdentityFailed(await userManager.UpdateAsync(user));
        return await ToResponseAsync(user);
    }

    private async Task<User> GetManagedUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
                   ?? throw new NotFoundException("Không tìm thấy tài khoản.");
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count == 0 || roles.Any(role => !ManagedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
            throw new ForbiddenException("Chỉ có thể quản lý tài khoản Censor, Employer hoặc Candidate.");
        return user;
    }

    private async Task<UserDetailResponseDto> ToResponseAsync(User user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserDetailResponseDto
        {
            Id = user.Id,
            PhoneNumber = user.PhoneNumber,
            Roles = roles.ToArray(),
            IsLockedOut = user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow,
            LockoutEndUtc = user.LockoutEnd?.ToUniversalTime()
        };
    }

    private static void ThrowIfIdentityFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new ConflictException(string.Join(". ", result.Errors.Select(error => error.Description)));
    }
}
