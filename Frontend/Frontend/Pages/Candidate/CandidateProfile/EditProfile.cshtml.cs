using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Candidate.CandidateProfile
{
    [Authorize(Roles = Role.Candidate)]
    public class EditProfileModel(ApiClient apiClient) : PageModel
    {
        [BindProperty]
        public EditProfileFormModel Input { get; set; } = new();
        public string? CurrentAvatarUrl { get; set; }
        public List<string> fileValidate = [".jpg", ".jpeg", ".png"];
        //public List<ProvinceDto> Provinces { get; private set; } = [];
        //public List<CommuneDto> Communes { get; private set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            //await LoadDropdownDataAsync();
            try
            {
                var profile = await apiClient.Api.CandidateProfiles.GetAsync();
                if (profile != null)
                {
                    Input.FullName = profile.FullName;
                    Input.Bio = profile.Bio;
                    Input.DateOfBirth = profile.DateOfBirth.HasValue
                  ? new DateTime(profile.DateOfBirth.Value.Year,
                                 profile.DateOfBirth.Value.Month,
                                 profile.DateOfBirth.Value.Day) : null;
                    Input.Gender = profile.Gender?.ToString();
                    Input.PreferredWorkCommuneCode = profile.PreferredWorkCommuneCode;
                    CurrentAvatarUrl = profile.AvatarFilePath;
                }
            }
            catch
            {

            }
            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            //await LoadDropdownDataAsync();
            if (Input.AvatarFile != null && Input.AvatarFile.Length > 0)
            {
                if (Input.AvatarFile != null && Input.AvatarFile.Length > 0)
                {
                    var ext = Path.GetExtension(Input.AvatarFile.FileName);
                    if (!fileValidate.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError(nameof(Input.AvatarFile), "Chỉ chấp nhận các định dạng .jpg, .jpeg, .png");
                    }
                    if (Input.AvatarFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError(nameof(Input.AvatarFile), "Ảnh không được vượt quá 5MB");
                    }
                }
            }
            if (Input.DateOfBirth.HasValue)
            {
                var today = DateTime.Today;
                if (Input.DateOfBirth.Value > today.AddYears(-18))
                {
                    ModelState.AddModelError(nameof(Input.DateOfBirth), "Bạn phải ít nhất 18 tuổi.");
                }
                if (Input.DateOfBirth.Value < today.AddYears(-100))
                {
                    ModelState.AddModelError(nameof(Input.DateOfBirth), "Ngày sinh không hợp lệ.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var body = new MultipartBody();
                body.AddOrReplacePart("FullName", "text/plain", Input.FullName ?? "");
                body.AddOrReplacePart("Bio", "text/plain", Input.Bio ?? "");
                if (Input.DateOfBirth.HasValue)
                    body.AddOrReplacePart("DateOfBirth", "text/plain",
                        Input.DateOfBirth.Value.ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(Input.Gender))
                    body.AddOrReplacePart("Gender", "text/plain", Input.Gender);
                if (!string.IsNullOrEmpty(Input.PreferredWorkCommuneCode))
                    body.AddOrReplacePart("PreferredWorkCommuneCode", "text/plain",
                        Input.PreferredWorkCommuneCode);
                if (Input.AvatarFile != null && Input.AvatarFile.Length > 0)
                {
                    var bytes = await ReadFileBytesAsync(Input.AvatarFile);
                    body.AddOrReplacePart("AvatarFilePath",
                        string.IsNullOrEmpty(Input.AvatarFile.ContentType)
                            ? "application/octet-stream" : Input.AvatarFile.ContentType,
                        bytes, Input.AvatarFile.FileName);


                }
                await apiClient.Api.CandidateProfiles.PutAsync(body);

                TempData.SetSuccessMessage("Cập nhật hồ sơ thành công");
                return RedirectToPage("./EditProfile");
            }
            catch (ApiException e)
            {
                TempData.SetErrorMessage(e.ToFriendlyErrorMessage());
            }
           

            return Page();

        }

        private static async Task<byte[]> ReadFileBytesAsync(IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        //public async Task LoadDropdownDataAsync()
        //{
        //    try
        //    {
        //        Provinces = await apiClient.Api.Addresses.Provinces.GetAsync();

        //    }
        //    catch
        //    {
        //        Provinces = [];
        //    }
        //    if (!string.IsNullOrWhiteSpace(Input.ProvinceCode))
        //    {
        //        try
        //        {
        //            Communes = await apiClient.Api.Addresses.Provinces[Input.ProvinceCode].Communes.GetAsync() ?? [];
        //        }
        //        catch
        //        {
        //            Communes = [];
        //        }
        //    }
        //}

        //public async Task<IActionResult> OnGetCommunesAsync(string provinceCode)
        //{
        //    if (string.IsNullOrWhiteSpace(provinceCode))
        //        return new JsonResult(new List<object>());
        //    try
        //    {
        //        var communes = await apiClient.Api.Addresses.Provinces[provinceCode].Communes.GetAsync();
        //        return new JsonResult(communes?.Select(c => new { code = c.Code, name = c.Name }) ?? []);
        //    }
        //    catch { return new JsonResult(new List<object>()); }
        //}


        public class EditProfileFormModel
        {
            public IFormFile? AvatarFile { get; set; }
            public string? Bio { get; set; }
            public string? FullName { get; set; }
            [DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
            public string? Gender { get; set; }
            //public string? ProvinceCode { get; set; }
            public string? PreferredWorkCommuneCode { get; set; }
        }
    }
}
