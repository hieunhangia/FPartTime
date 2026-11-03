using System.ComponentModel.DataAnnotations;
using ApiSdk.Models;
using Frontend.Constants;

namespace Frontend.ViewModels.Manager;

public class ViewJobStatsPackageFormInput
{
    [Required(ErrorMessage = "Tên gói lượt xem là bắt buộc.")]
    [MaxLength(BusinessRuleConstants.Models.CandidateViewJobStatsPackage.NameMaxLength,
        ErrorMessage = "Tên gói lượt xem không được vượt quá {1} ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số lượt xem là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượt xem phải lớn hơn 0.")]
    public int? ViewCredits { get; set; }

    [Required(ErrorMessage = "Giá gói là bắt buộc.")]
    [Range(1L, BusinessRuleConstants.Models.CandidateViewJobStatsPackage.MaxPrice,
        ErrorMessage = "Giá gói phải lớn hơn 0 và nằm trong giới hạn cho phép.")]
    public long? Price { get; set; }

    [MaxLength(BusinessRuleConstants.Models.CandidateViewJobStatsPackage.DescriptionMaxLength,
        ErrorMessage = "Mô tả gói không được vượt quá {1} ký tự.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public CreateUpdateViewJobStatsPackageRequestDto ToRequest() => new()
    {
        Name = Name.Trim(),
        ViewCredits = ViewCredits!.Value,
        Price = Price!.Value,
        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        IsActive = IsActive
    };

    public static ViewJobStatsPackageFormInput FromResponse(ManagedViewJobStatsPackageResponseDto package) => new()
    {
        Name = package.Name ?? string.Empty,
        ViewCredits = package.ViewCredits,
        Price = package.Price is { } price ? (long)price : null,
        Description = package.Description,
        IsActive = package.IsActive ?? false
    };
}
