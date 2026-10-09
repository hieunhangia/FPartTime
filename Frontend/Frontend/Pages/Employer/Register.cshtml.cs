using System.ComponentModel.DataAnnotations;
using ApiSdk;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Employer;

[Authorize(Roles = Role.Candidate)]
public class RegisterModel(ApiClient apiClient) : PageModel
{
    [BindProperty] public EmployerRegistrationFormModel Input { get; set; } = new();

    public async Task<IActionResult> OnGet()
    {
        var registerList = await apiClient.Api.EmployerRegistrations.GetAsync() ?? [];
        var activeRegistration = registerList.FirstOrDefault(x => x.Status is "Pending" or "Approved");
        if (activeRegistration is not null)
        {
            if (activeRegistration.Status == "Pending")
            {
                TempData.SetWarningMessage("Bạn đang có một yêu cầu đăng ký trở thành nhà tuyển dụng đang chờ duyệt.");
            }
            else
            {
                TempData.SetInfoMessage("Yêu cầu đăng ký trở thành nhà tuyển dụng của bạn đã được duyệt.");
            }

            return RedirectToPage("./MyRegistrations");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateUploadedFile(Input.DocumentFile, "Tài liệu xác thực doanh nghiệp",
            BusinessRuleConstants.FileUpload.AllowedDocumentExtensions);
        ValidateUploadedFile(Input.CCCDFrontImage, "Ảnh CCCD mặt trước",
            BusinessRuleConstants.FileUpload.AllowedImageExtensions);
        ValidateUploadedFile(Input.CCCDBackImage, "Ảnh CCCD mặt sau",
            BusinessRuleConstants.FileUpload.AllowedImageExtensions);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var body = new MultipartBody();
            body.AddOrReplacePart("CompanyName", "text/plain", Input.CompanyName);
            body.AddOrReplacePart("TaxCode", "text/plain", Input.TaxCode);
            body.AddOrReplacePart("FullName", "text/plain", Input.FullName);
            body.AddOrReplacePart("DateOfBirth", "text/plain", Input.DateOfBirth!.Value.ToString("yyyy-MM-dd"));
            body.AddOrReplacePart("PhoneNumber", "text/plain", Input.PhoneNumber);
            body.AddOrReplacePart("CCCDNumber", "text/plain", Input.CCCDNumber);
            body.AddOrReplacePart("DocumentFile", GetContentType(Input.DocumentFile!),
                await ReadFileBytesAsync(Input.DocumentFile!), Input.DocumentFile!.FileName);
            body.AddOrReplacePart("CCCDFrontImage", GetContentType(Input.CCCDFrontImage!),
                await ReadFileBytesAsync(Input.CCCDFrontImage!), Input.CCCDFrontImage!.FileName);
            body.AddOrReplacePart("CCCDBackImage", GetContentType(Input.CCCDBackImage!),
                await ReadFileBytesAsync(Input.CCCDBackImage!), Input.CCCDBackImage!.FileName);

            var response = await apiClient.Api.EmployerRegistrations.PostAsync(body);
            if (response?.Id is null)
            {
                TempData.SetErrorMessage("Đăng ký thất bại. Vui lòng kiểm tra lại thông tin và thử lại.");
                return Page();
            }

            TempData.SetSuccessMessage("Đã gửi yêu cầu đăng ký trở thành nhà tuyển dụng.");
            return RedirectToPage("MyRegistrations");
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            return Page();
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi trong quá trình gửi yêu cầu đăng ký.");
            return Page();
        }
    }

    private void ValidateUploadedFile(IFormFile? file, string fieldLabel, string[] allowedExtensions)
    {
        if (file == null || file.Length == 0)
        {
            return;
        }

        var propertyName = GetPropertyName(fieldLabel);

        if (file.Length > BusinessRuleConstants.FileUpload.MaxFileSizeInBytes)
        {
            ModelState.AddModelError(propertyName, $"{fieldLabel} không được vượt quá 5MB.");
            return;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(propertyName,
                $"{fieldLabel} phải có định dạng {string.Join(", ", allowedExtensions)}.");
        }
    }

    private static string GetPropertyName(string fieldLabel) => fieldLabel switch
    {
        "Tài liệu xác thực doanh nghiệp" => nameof(EmployerRegistrationFormModel.DocumentFile),
        "Ảnh CCCD mặt trước" => nameof(EmployerRegistrationFormModel.CCCDFrontImage),
        "Ảnh CCCD mặt sau" => nameof(EmployerRegistrationFormModel.CCCDBackImage),
        _ => string.Empty
    };

    private static string GetContentType(IFormFile file) =>
        string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

    private static async Task<byte[]> ReadFileBytesAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    public class EmployerRegistrationFormModel
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Tên công ty không được vượt quá 100 ký tự.")]
        [Display(Name = "Tên công ty")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã số thuế là bắt buộc.")]
        [MaxLength(50, ErrorMessage = "Mã số thuế không được vượt quá 50 ký tự.")]
        [Display(Name = "Mã số thuế")]
        public string TaxCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên người đại diện")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.PhoneNumberRegex, ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số CCCD là bắt buộc.")]
        [RegularExpression(BusinessRuleConstants.CCCDNumberRegex, ErrorMessage = "Số CCCD phải gồm đúng 12 chữ số.")]
        [Display(Name = "Số CCCD")]
        public string CCCDNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tài liệu xác thực doanh nghiệp là bắt buộc.")]
        [Display(Name = "Tài liệu xác thực doanh nghiệp")]
        public IFormFile? DocumentFile { get; set; }

        [Required(ErrorMessage = "Ảnh CCCD mặt trước là bắt buộc.")]
        [Display(Name = "Ảnh CCCD mặt trước")]
        public IFormFile? CCCDFrontImage { get; set; }

        [Required(ErrorMessage = "Ảnh CCCD mặt sau là bắt buộc.")]
        [Display(Name = "Ảnh CCCD mặt sau")]
        public IFormFile? CCCDBackImage { get; set; }
    }
}
