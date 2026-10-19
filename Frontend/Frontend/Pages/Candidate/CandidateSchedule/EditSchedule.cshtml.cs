using ApiSdk;
using ApiSdk.Models;
using BackendApiClient.Extensions;
using Frontend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Kiota.Abstractions;

namespace Frontend.Pages.Candidate.CandidateSchedule
{
    public class EditScheduleModel(ApiClient apiClient) : PageModel
    {
        public static readonly DayOfWeek[] DisplayDays =
            [
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
            ];

        public static readonly Shift[] DisplayShifts =
    [
        Shift.Morning,
        Shift.Afternoon,
        Shift.Evening,
        Shift.Night
    ];

        [BindProperty]
        public List<string> SelectedSlots { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var mySchedules = await apiClient.Api.CandidateSchedules.GetAsync();
                if (mySchedules != null)
                {
                    SelectedSlots = mySchedules
                        .Where(s => s.DayOfWeek.HasValue && s.Shift.HasValue)
                        .Select(s => $"{s.DayOfWeek}:{s.Shift}")
                        .ToList();
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
            try
            {
                var scheduleSlots = new List<CandidateScheduleDto>();

                foreach (var slot in SelectedSlots)
                {
                    var parts = slot.Split(':');
                    if (parts.Length == 2 &&
                        Enum.TryParse<ApiSdk.Models.DayOfWeekObject>(parts[0], out var day) &&
                        Enum.TryParse<ApiSdk.Models.Shift>(parts[1], out var shift))
                    {
                        scheduleSlots.Add(new CandidateScheduleDto
                        {
                            DayOfWeek = day,
                            Shift = shift
                        });
                    }
                }

                var requestBody = new UpdateCandidateScheduleDto
                {
                    Schedules = scheduleSlots
                };

                await apiClient.Api.CandidateSchedules.PutAsync(requestBody);

                TempData.SetSuccessMessage("Cập nhật lịch rảnh thành công!");
                return RedirectToPage("./EditSchedule");
            }
            catch (ApiException ex)
            {
                TempData.SetErrorMessage(ex.ToFriendlyErrorMessage());
                return Page();
            }
        }

        public static string GetDayName(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => "Thứ 2",
            DayOfWeek.Tuesday => "Thứ 3",
            DayOfWeek.Wednesday => "Thứ 4",
            DayOfWeek.Thursday => "Thứ 5",
            DayOfWeek.Friday => "Thứ 6",
            DayOfWeek.Saturday => "Thứ 7",
            DayOfWeek.Sunday => "Chủ Nhật",
            _ => day.ToString()
        };

        public static string GetShiftName(Shift shift) => shift switch
        {
            Shift.Morning => "Sáng",
            Shift.Afternoon => "Chiều",
            Shift.Evening => "Tối",
            Shift.Night => "Đêm",
            _ => shift.ToString()
        };
    }
}
