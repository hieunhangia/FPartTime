using System.ComponentModel.DataAnnotations;
using ApiSdk;
using BackendApiClient.Extensions;
using Frontend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Employer.EmployerProfile
{
    public class EditProfileModel(ApiClient apiClient) : PageModel
    {
        public HashSet<string> AllowedImageExtensions = [".jpg", ".jpeg", ".png"];

        private const long MaxFileSizeInBytes = 5 * 1024 * 1024;
        [BindProperty]
        public EditEmployerProfileFormModel Input { get; set; } = new();

        public string? CurrentLogoUrl { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var profile = await apiClient.Api.EmployerProfiles.GetAsync();
                if (profile != null)
                {
                    Input.Description = profile.Description;
                    Input.ContactPhone = profile.ContactPhone;
                    CurrentLogoUrl = profile.LogoFilePath;
                }
            }
            catch (ApiException ex)
            {
                TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Validate File Logo
            if (Input.LogoFile != null && Input.LogoFile.Length > 0)
            {
                var ext = Path.GetExtension(Input.LogoFile.FileName);
                if (!AllowedImageExtensions.Contains(ext))
                {
                    ModelState.AddModelError("Input.LogoFile", "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, .png");
                }
                if (Input.LogoFile.Length > MaxFileSizeInBytes)
                {
                    ModelState.AddModelError("Input.LogoFile", "Ảnh logo không được vượt quá 5MB");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var body = new MultipartBody();

                body.AddOrReplacePart("Description", "text/plain", Input.Description?.Trim() ?? string.Empty);
                body.AddOrReplacePart("ContactPhone", "text/plain", Input.ContactPhone?.Trim() ?? string.Empty);

                if (Input.LogoFile != null && Input.LogoFile.Length > 0)
                {
                    var bytes = await ReadFileBytesAsync(Input.LogoFile);
                    var contentType = string.IsNullOrEmpty(Input.LogoFile.ContentType)
                        ? "application/octet-stream"
                        : Input.LogoFile.ContentType;

                    body.AddOrReplacePart("LogoFile", contentType, bytes, Input.LogoFile.FileName);
                }

                await apiClient.Api.EmployerProfiles.PutAsync(body);

                TempData.SetSuccessMessage("Cập nhật thông tin công ty thành công");
                return RedirectToPage("./EditProfile");
            }
            catch (ApiException e)
            {
                TempData.SetErrorMessage(e.ToFriendlyErrorMessage());
                return Page();
            }
        }

        private static async Task<byte[]> ReadFileBytesAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        public class EditEmployerProfileFormModel
        {
            public IFormFile? LogoFile { get; set; }

            [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
            public string? Description { get; set; }

            [RegularExpression(@"^(0)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Số điện thoại liên hệ không hợp lệ (gồm 10 chữ số)")]
            public string? ContactPhone { get; set; }
        }
    }
}
