using System.ComponentModel.DataAnnotations;
using Contracts.Common;

namespace Contracts.Application.Admin;

public interface IUserService
{
    Task<PagedResponseDto<UserDetailResponseDto>> GetUsersAsync(UserListRequestDto request,
        CancellationToken cancellationToken = default);
    Task<UserDetailResponseDto> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDetailResponseDto> UpdateRolesAsync(Guid id, UpdateUserRolesRequestDto request,
        CancellationToken cancellationToken = default);
    Task<UserDetailResponseDto> LockUserAsync(Guid id, LockUserRequestDto request,
        CancellationToken cancellationToken = default);
    Task<UserDetailResponseDto> UnlockUserAsync(Guid id, CancellationToken cancellationToken = default);
}

public class UserListRequestDto : PaginationRequestDto
{
    public string? Role { get; set; }

    [MaxLength(10, ErrorMessage = "Số điện thoại tìm kiếm không được vượt quá 10 ký tự.")]
    public string? PhoneNumberPrefix { get; set; }
}

public class UpdateUserRolesRequestDto
{
    [Required(ErrorMessage = "Danh sách quyền là bắt buộc.")]
    public List<string> Roles { get; set; } = [];
}

public class LockUserRequestDto
{
    [Required(ErrorMessage = "Thời điểm hết khóa là bắt buộc.")]
    public DateTimeOffset? LockoutEndUtc { get; set; }
}

public class UserDetailResponseDto
{
    public required Guid Id { get; set; }
    public string? PhoneNumber { get; set; }
    public required IReadOnlyList<string> Roles { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEndUtc { get; set; }
}
