using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Candidate.CandidateApplication
{
    public class DetailModel(ApiClient apiClient) : PageModel
    {
        public JobApplicationResponseDto? Application { get; private set; }
        public JobResponseDto? Job { get; private set; }
        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                var result = await apiClient.Api.Candidate.JobApplications
                    .GetPagedWithODataAsync<JobApplicationResponseDto, PagedResponseDtoOfJobApplicationResponseDto>(q =>
                    {
                        q.Filter(a => a.Id == id);
                    });
                Application = result?.Items?.FirstOrDefault();
                if (Application == null)
                {
                    TempData.SetErrorMessage("Không tìm thấy đơn ứng tuyển.");
                    return RedirectToPage("./ApplicationHistory");
                }
                if (Application.JobId.HasValue)
                {
                    Job = await apiClient.Api.Jobs[Application.JobId.Value].GetAsync();
                }
                return Page();
            }
            catch (ApiException ex)
            {
                TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
                return RedirectToPage("./ApplicationHistory");
            }
            catch
            {
                TempData.SetErrorMessage("Đã xảy ra lỗi khi tải thông tin đơn ứng tuyển.");
                return RedirectToPage("./ApplicationHistory");
            }
        }
        public async Task<IActionResult> OnPostWithdrawAsync(Guid id)
        {
            try
            {
                await apiClient.Api.Candidate.JobApplications[id].DeleteAsync();
                TempData.SetSuccessMessage("Hủy đơn ứng tuyển thành công.");
                return RedirectToPage("./ApplicationHistory");
            }
            catch (ApiException ex)
            {
                TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
                return RedirectToPage(new { id });
            }
            catch
            {
                TempData.SetErrorMessage("Đã xảy ra lỗi khi hủy đơn ứng tuyển.");
                return RedirectToPage(new { id });
            }
        }
        public static string FormatSalary(JobSalaryResponseDto salary)
        {
            var amountStr = (salary.Amount ?? 0).ToString("N0") + " ₫";
            var unitStr = salary.TimeUnit switch
            {
                JobSalaryTimeUnit.Hour => "giờ",
                JobSalaryTimeUnit.Session => "ca",
                JobSalaryTimeUnit.Day => "ngày",
                JobSalaryTimeUnit.Week => "tuần",
                JobSalaryTimeUnit.Month => "tháng",
                JobSalaryTimeUnit.Project => "dự án",
                _ => "lần"
            };

            if (salary.TimeQuantity is > 1)
            {
                return $"{amountStr} / {salary.TimeQuantity} {unitStr}";
            }

            return $"{amountStr} / {unitStr}";
        }

        public static string FormatFullAddress(JobResponseDto job)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(job.DetailAddress)) parts.Add(job.DetailAddress.Trim());
            if (!string.IsNullOrWhiteSpace(job.CommuneName)) parts.Add(job.CommuneName.Trim());
            if (!string.IsNullOrWhiteSpace(job.ProvinceName)) parts.Add(job.ProvinceName.Trim());
            return parts.Count > 0 ? string.Join(", ", parts) : "Chưa cập nhật";
        }

        public static string FormatAgeRequirement(int? minAge, int? maxAge)
        {
            if (minAge.HasValue && maxAge.HasValue) return $"{minAge.Value} - {maxAge.Value} tuổi";
            if (minAge.HasValue) return $"Từ {minAge.Value} tuổi trở lên";
            if (maxAge.HasValue) return $"Tối đa {maxAge.Value} tuổi";
            return "Không yêu cầu độ tuổi";
        }

        public static string FormatGenderRequirement(Gender? gender) => gender switch
        {
            Gender.Male => "Nam",
            Gender.Female => "Nữ",
            _ => "Không yêu cầu"
        };

        public (string Label, string CssClass) GetStatusDisplay(JobApplicationStatus? status) => status switch
        {
            JobApplicationStatus.Pending => ("Chờ duyệt", "bg-warning text-dark"),
            JobApplicationStatus.Accepted => ("Đã nhận", "bg-success"),
            JobApplicationStatus.Rejected => ("Đã từ chối", "bg-danger"),
            _ => (status?.ToString() ?? "-", "bg-secondary")
        };
    }
}

