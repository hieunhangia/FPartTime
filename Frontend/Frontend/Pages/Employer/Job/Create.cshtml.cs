using System.ComponentModel.DataAnnotations;
using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Employer.Job;

[Authorize(Roles = Role.Employer)]
public class CreateModel(ApiClient apiClient) : PageModel
{
    [BindProperty] public CreateJobFormModel Form { get; set; } = new();

    public List<IndustryResponseDto> Industries { get; private set; } = [];
    public List<ProvinceDto> Provinces { get; private set; } = [];
    public List<CommuneDto> Communes { get; private set; } = [];
    public PostsQuotaResponseDto? PostsQuota { get; private set; }

    public bool IsQuotaExceeded =>
        PostsQuota != null && PostsQuota.CurrentActivePosts >= PostsQuota.MaxActivePosts;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadInitialDataAsync();

        if (Form.JobSchedules.Count == 0)
        {
            Form.JobSchedules.Add(new JobScheduleInputModel
            {
                DayOfWeek = DayOfWeekObject.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(12, 0)
            });
        }

        if (Form.JobSalaries.Count == 0)
        {
            Form.JobSalaries.Add(new JobSalaryInputModel
            {
                Amount = 30000,
                TimeQuantity = 1,
                TimeUnit = JobSalaryTimeUnit.Hour
            });
        }

        return Page();
    }

    public async Task<IActionResult> OnGetCommunesAsync(string provinceCode)
    {
        if (string.IsNullOrWhiteSpace(provinceCode))
        {
            return new JsonResult(new List<object>());
        }

        try
        {
            var communes = await apiClient.Api.Addresses.Provinces[provinceCode].Communes.GetAsync();
            return new JsonResult(communes?.Select(c => new { code = c.Code, name = c.Name }) ?? []);
        }
        catch
        {
            return new JsonResult(new List<object>());
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadQuotaAsync();

        if (IsQuotaExceeded)
        {
            ModelState.AddModelError(string.Empty,
                $"Bạn đã đạt giới hạn số lượng bài đăng công việc đang hoạt động ({PostsQuota?.MaxActivePosts}). Vui lòng đóng một bài đăng đang hoạt động trước khi tạo bài mới.");
        }

        ValidateBusinessRules();

        if (!ModelState.IsValid)
        {
            await LoadDropdownDataAsync();
            return Page();
        }

        try
        {
            var body = new CreateJobRequestDto
            {
                Title = Form.Title.Trim(),
                Description = Form.Description.Trim(),
                IndustryId = Form.IndustryId,
                VacancyCount = Form.VacancyCount,
                GenderRequirement = Form.GenderRequirement,
                MinAgeRequirement = Form.MinAgeRequirement,
                MaxAgeRequirement = Form.MaxAgeRequirement,
                ExpiryAt = Form.ExpiryAt.HasValue
                    ? new Date(Form.ExpiryAt.Value.Year, Form.ExpiryAt.Value.Month, Form.ExpiryAt.Value.Day)
                    : null,
                CommuneCode = Form.CommuneCode,
                DetailAddress = Form.DetailAddress.Trim(),
                JobSchedules = Form.JobSchedules.Select(s => new JobScheduleRequestDto
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = new Time(s.StartTime.Hour, s.StartTime.Minute, 0),
                    EndTime = new Time(s.EndTime.Hour, s.EndTime.Minute, 0)
                }).ToList(),
                JobSalaries = Form.JobSalaries.Select(s => new JobSalaryRequestDto
                {
                    Amount = (double)s.Amount,
                    TimeQuantity = s.TimeQuantity,
                    TimeUnit = s.TimeUnit
                }).ToList()
            };

            var createdJob = await apiClient.Api.Jobs.PostAsync(body);
            if (createdJob?.Id is null)
            {
                TempData.SetErrorMessage("Không thể tạo công việc. Vui lòng kiểm tra lại thông tin và thử lại.");
                await LoadDropdownDataAsync();
                return Page();
            }

            TempData.SetSuccessMessage("Tạo bài đăng công việc mới thành công.");
            return RedirectToPage("Detail", new { id = createdJob.Id });
        }
        catch (ApiException ex)
        {
            TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
            await LoadDropdownDataAsync();
            return Page();
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi tạo công việc mới. Vui lòng thử lại sau.");
            await LoadDropdownDataAsync();
            return Page();
        }
    }

    private void ValidateBusinessRules()
    {
        // 1. Kiểm tra giới hạn tuổi
        if (Form is { MinAgeRequirement: not null, MaxAgeRequirement: not null } &&
            Form.MinAgeRequirement.Value > Form.MaxAgeRequirement.Value)
        {
            ModelState.AddModelError("Form.MinAgeRequirement", "Tuổi tối thiểu không được lớn hơn tuổi tối đa.");
        }

        // 2. Kiểm tra ngày hết hạn
        if (Form.ExpiryAt.HasValue && Form.ExpiryAt.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError("Form.ExpiryAt", "Ngày hết hạn phải lớn hơn ngày hiện tại.");
        }

        // 3. Kiểm tra lịch làm việc (JobSchedules)
        if (Form.JobSchedules.Count == 0)
        {
            ModelState.AddModelError("Form.JobSchedules", "Vui lòng thêm ít nhất một ca làm việc.");
        }
        else if (Form.JobSchedules.Count > BusinessRuleConstants.Jobs.MaxJobSchedulesPerJob)
        {
            ModelState.AddModelError("Form.JobSchedules",
                $"Số lượng lịch làm việc không được vượt quá {BusinessRuleConstants.Jobs.MaxJobSchedulesPerJob}.");
        }
        else
        {
            for (var i = 0; i < Form.JobSchedules.Count; i++)
            {
                var s = Form.JobSchedules[i];
                if (s.StartTime >= s.EndTime)
                {
                    ModelState.AddModelError($"Form.JobSchedules[{i}].EndTime",
                        $"Ca làm việc ngày {GetVietnameseDayOfWeek(s.DayOfWeek)} có thời gian kết thúc phải lớn hơn thời gian bắt đầu. Nếu ca kéo dài qua nửa đêm, vui lòng tách thành 2 ca riêng biệt.");
                }
                else if ((s.EndTime - s.StartTime) < TimeSpan.FromMinutes(30))
                {
                    ModelState.AddModelError($"Form.JobSchedules[{i}].EndTime",
                        $"Ca làm việc ngày {GetVietnameseDayOfWeek(s.DayOfWeek)} phải kéo dài tối thiểu 30 phút.");
                }
            }

            foreach (var group in Form.JobSchedules.GroupBy(s => s.DayOfWeek))
            {
                var orderedSchedules = group.OrderBy(s => s.StartTime).ToList();
                for (var i = 1; i < orderedSchedules.Count; i++)
                {
                    var cur = orderedSchedules[i];
                    var prev = orderedSchedules[i - 1];
                    if (cur.StartTime < prev.EndTime)
                    {
                        ModelState.AddModelError("Form.JobSchedules",
                            $"Các ca làm việc trong ngày {GetVietnameseDayOfWeek(cur.DayOfWeek)} bị trùng hoặc chồng chéo thời gian ({prev.StartTime:HH\\:mm}-{prev.EndTime:HH\\:mm} và {cur.StartTime:HH\\:mm}-{cur.EndTime:HH\\:mm}).");
                    }
                }
            }
        }

        // 4. Kiểm tra mức lương (JobSalaries)
        if (Form.JobSalaries.Count == 0)
        {
            ModelState.AddModelError("Form.JobSalaries", "Vui lòng thêm ít nhất một mức lương.");
        }
        else if (Form.JobSalaries.Count > BusinessRuleConstants.Jobs.MaxJobSalariesPerJob)
        {
            ModelState.AddModelError("Form.JobSalaries",
                $"Số lượng lương không được vượt quá {BusinessRuleConstants.Jobs.MaxJobSalariesPerJob}.");
        }
        else
        {
            if (Form.JobSalaries.GroupBy(s => new { s.TimeUnit, s.TimeQuantity }).Any(g => g.Count() > 1))
            {
                ModelState.AddModelError("Form.JobSalaries",
                    "Không được thiết lập trùng lặp đơn vị tính lương và thời gian.");
            }

            for (var i = 0; i < Form.JobSalaries.Count; i++)
            {
                var salary = Form.JobSalaries[i];
                if (salary.Amount % 1 != 0)
                {
                    ModelState.AddModelError($"Form.JobSalaries[{i}].Amount",
                        $"Số tiền lương ({salary.Amount:N0}) không được chứa số lẻ thập phân.");
                }

                switch (salary)
                {
                    case { TimeUnit: JobSalaryTimeUnit.Hour, TimeQuantity: > 24 }:
                        ModelState.AddModelError($"Form.JobSalaries[{i}].TimeQuantity",
                            "Số giờ cho một định mức lương không được vượt quá 24 giờ.");
                        break;
                    case { TimeUnit: JobSalaryTimeUnit.Day, TimeQuantity: > 31 }:
                        ModelState.AddModelError($"Form.JobSalaries[{i}].TimeQuantity",
                            "Số ngày cho một định mức lương không được vượt quá 31 ngày.");
                        break;
                    case { TimeUnit: JobSalaryTimeUnit.Month, TimeQuantity: > 12 }:
                        ModelState.AddModelError($"Form.JobSalaries[{i}].TimeQuantity",
                            "Số tháng cho một định mức lương không được vượt quá 12 tháng.");
                        break;
                    case { TimeUnit: JobSalaryTimeUnit.Week, TimeQuantity: > 52 }:
                        ModelState.AddModelError($"Form.JobSalaries[{i}].TimeQuantity",
                            "Số tuần cho một định mức lương không được vượt quá 52 tuần.");
                        break;
                    case { TimeUnit: JobSalaryTimeUnit.Session, TimeQuantity: > 2 }:
                        ModelState.AddModelError($"Form.JobSalaries[{i}].TimeQuantity",
                            "Số buổi cho một định mức lương không được vượt quá 2 buổi.");
                        break;
                    case { TimeUnit: JobSalaryTimeUnit.Project, TimeQuantity: > 1 }:
                        ModelState.AddModelError($"Form.JobSalaries[{i}].TimeQuantity",
                            "Số dự án cho một định mức lương không được vượt quá 1 dự án.");
                        break;
                }
            }
        }
    }

    private async Task LoadInitialDataAsync()
    {
        await LoadQuotaAsync();
        await LoadDropdownDataAsync();
    }

    private async Task LoadQuotaAsync()
    {
        try
        {
            PostsQuota = await apiClient.Api.Jobs.PostsQuota.GetAsync();
        }
        catch
        {
            PostsQuota = null;
        }
    }

    private async Task LoadDropdownDataAsync()
    {
        try
        {
            Industries = await apiClient.Api.Industries.GetAsync() ?? [];
        }
        catch
        {
            Industries = [];
        }

        try
        {
            Provinces = await apiClient.Api.Addresses.Provinces.GetAsync() ?? [];
        }
        catch
        {
            Provinces = [];
        }

        if (!string.IsNullOrWhiteSpace(Form.ProvinceCode))
        {
            try
            {
                Communes = await apiClient.Api.Addresses.Provinces[Form.ProvinceCode].Communes.GetAsync() ?? [];
            }
            catch
            {
                Communes = [];
            }
        }
    }

    private static string GetVietnameseDayOfWeek(DayOfWeekObject dayOfWeek) => dayOfWeek switch
    {
        DayOfWeekObject.Monday => "Thứ Hai",
        DayOfWeekObject.Tuesday => "Thứ Ba",
        DayOfWeekObject.Wednesday => "Thứ Tư",
        DayOfWeekObject.Thursday => "Thứ Năm",
        DayOfWeekObject.Friday => "Thứ Sáu",
        DayOfWeekObject.Saturday => "Thứ Bảy",
        DayOfWeekObject.Sunday => "Chủ Nhật",
        _ => dayOfWeek.ToString()
    };

    public class CreateJobFormModel
    {
        [Required(ErrorMessage = "Tiêu đề công việc là bắt buộc.")]
        [MaxLength(BusinessRuleConstants.Models.Job.TitleMaxLength,
            ErrorMessage = "Tiêu đề công việc không được vượt quá {1} ký tự.")]
        [Display(Name = "Tiêu đề công việc")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả công việc là bắt buộc.")]
        [MaxLength(BusinessRuleConstants.Models.Job.DescriptionMaxLength,
            ErrorMessage = "Mô tả công việc không được vượt quá {1} ký tự.")]
        [Display(Name = "Mô tả công việc")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngành nghề là bắt buộc.")]
        [Display(Name = "Ngành nghề")]
        public Guid? IndustryId { get; set; }

        [Range(1, BusinessRuleConstants.Jobs.MaxVacanciesPerJob,
            ErrorMessage = "Số lượng tuyển dụng phải từ 1 đến {1} người.")]
        [Display(Name = "Số lượng tuyển dụng")]
        public int? VacancyCount { get; set; }

        [Display(Name = "Yêu cầu giới tính")] public Gender? GenderRequirement { get; set; }

        [Range(BusinessRuleConstants.Jobs.MinAge, BusinessRuleConstants.Jobs.MaxAge,
            ErrorMessage = "Tuổi tối thiểu phải từ {1} đến {2}.")]
        [Display(Name = "Tuổi tối thiểu")]
        public int? MinAgeRequirement { get; set; }

        [Range(BusinessRuleConstants.Jobs.MinAge, BusinessRuleConstants.Jobs.MaxAge,
            ErrorMessage = "Tuổi tối đa phải từ {1} đến {2}.")]
        [Display(Name = "Tuổi tối đa")]
        public int? MaxAgeRequirement { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Hạn nhận hồ sơ")]
        public DateOnly? ExpiryAt { get; set; }

        [Required(ErrorMessage = "Tỉnh/Thành phố là bắt buộc.")]
        [Display(Name = "Tỉnh/Thành phố")]
        public string ProvinceCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xã/Phường là bắt buộc.")]
        [MaxLength(BusinessRuleConstants.Models.Commune.CodeMaxLength,
            ErrorMessage = "Mã xã/phường không được vượt quá {1} ký tự.")]
        [Display(Name = "Xã/Phường")]
        public string CommuneCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc.")]
        [MaxLength(BusinessRuleConstants.Models.Job.DetailAddressMaxLength,
            ErrorMessage = "Địa chỉ chi tiết không được vượt quá {1} ký tự.")]
        [Display(Name = "Địa chỉ chi tiết")]
        public string DetailAddress { get; set; } = string.Empty;

        public List<JobScheduleInputModel> JobSchedules { get; set; } = [];
        public List<JobSalaryInputModel> JobSalaries { get; set; } = [];
    }

    public class JobScheduleInputModel
    {
        [Required(ErrorMessage = "Ngày trong tuần là bắt buộc.")]
        public DayOfWeekObject DayOfWeek { get; set; } = DayOfWeekObject.Monday;

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc.")]
        public TimeOnly StartTime { get; set; } = new(8, 0);

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc.")]
        public TimeOnly EndTime { get; set; } = new(12, 0);
    }

    public class JobSalaryInputModel
    {
        [Required(ErrorMessage = "Số tiền là bắt buộc.")]
        [Range((double)BusinessRuleConstants.Jobs.MinSalaryAmount, (double)BusinessRuleConstants.Jobs.MaxSalaryAmount,
            ErrorMessage = "Số tiền phải từ 1,000 đến 500,000,000 VNĐ.")]
        public decimal Amount { get; set; } = 30000;

        [Required(ErrorMessage = "Số lượng thời gian là bắt buộc.")]
        [Range(1, 1000, ErrorMessage = "Số lượng thời gian phải từ 1 đến 1000.")]
        public int TimeQuantity { get; set; } = 1;

        [Required(ErrorMessage = "Đơn vị thời gian là bắt buộc.")]
        public JobSalaryTimeUnit TimeUnit { get; set; } = JobSalaryTimeUnit.Hour;
    }
}