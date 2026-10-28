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

namespace Frontend.Pages.Manager.Employers;

[Authorize(Roles = Role.Manager)]
public class DetailModel(ApiClient apiClient) : PageModel
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public ManagedEmployerDetailResponseDto? Employer { get; private set; }
    public List<ManagedEmployerSubscriptionResponseDto> Subscriptions { get; private set; } = [];
    public bool SubscriptionsLoaded { get; private set; }
    public ManagedEmployerSubscriptionResponseDto? ActiveSubscription => Subscriptions.FirstOrDefault(
        subscription => subscription.Status == EmployerSubscriptionStatus.Active &&
                        subscription.EndDate >= DateTimeOffset.UtcNow);

    [BindProperty]
    public SubscriptionFormInput Input { get; set; } = new();

    public class SubscriptionFormInput
    {
        [EnumDataType(typeof(EmployerSubscriptionTier), ErrorMessage = "Hạng gói không hợp lệ.")]
        public EmployerSubscriptionTier? Tier { get; set; }

        [EnumDataType(typeof(EmployerSubscriptionDuration), ErrorMessage = "Kỳ hạn gói không hợp lệ.")]
        public EmployerSubscriptionDuration? Duration { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? EndDateLocal { get; set; }
    }

    public Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken) =>
        LoadPageAsync(id, cancellationToken);

    public async Task<IActionResult> OnPostGrantAsync(Guid id, CancellationToken cancellationToken)
    {
        var page = await LoadPageAsync(id, cancellationToken);
        if (!SubscriptionsLoaded)
            return page;

        if (ActiveSubscription is not null)
        {
            ModelState.AddModelError(string.Empty,
                "Nhà tuyển dụng đã có gói đang hoạt động. Vui lòng cập nhật gói hiện tại.");
            return page;
        }

        ValidateChoice();
        if (!ModelState.IsValid)
            return page;

        return await SaveAsync(id, async () =>
        {
            await apiClient.Api.Manager.Employers[id].Subscriptions.PostAsync(
                new GrantEmployerSubscriptionRequestDto
                {
                    Tier = Input.Tier,
                    Duration = Input.Duration
                }, cancellationToken: cancellationToken);
        }, "Đã cấp gói cho nhà tuyển dụng.", cancellationToken);
    }

    public async Task<IActionResult> OnPostTierAsync(Guid id, Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        ValidateSubscriptionId(subscriptionId);
        ValidateChoice();
        if (!ModelState.IsValid)
            return await LoadPageAsync(id, cancellationToken);

        return await SaveAsync(id, async () =>
        {
            await apiClient.Api.Manager.Employers[id].Subscriptions[subscriptionId].Tier.PutAsync(
                new UpdateEmployerSubscriptionTierRequestDto
                {
                    Tier = Input.Tier,
                    Duration = Input.Duration
                }, cancellationToken: cancellationToken);
        }, "Đã cập nhật hạng gói.", cancellationToken);
    }

    public async Task<IActionResult> OnPostPeriodAsync(Guid id, Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        ValidateSubscriptionId(subscriptionId);
        if (Input.EndDateLocal is null)
            ModelState.AddModelError("Input.EndDateLocal", "Vui lòng nhập ngày hết hạn mới.");

        DateTimeOffset? endDate = null;
        if (ModelState.IsValid)
        {
            endDate = new DateTimeOffset(
                DateTime.SpecifyKind(Input.EndDateLocal!.Value, DateTimeKind.Unspecified), VietnamOffset);
            if (endDate <= DateTimeOffset.UtcNow)
                ModelState.AddModelError("Input.EndDateLocal", "Ngày hết hạn mới phải ở tương lai.");
        }

        if (!ModelState.IsValid)
            return await LoadPageAsync(id, cancellationToken);

        return await SaveAsync(id, async () =>
        {
            await apiClient.Api.Manager.Employers[id].Subscriptions[subscriptionId].Period.PatchAsync(
                new UpdateEmployerSubscriptionPeriodRequestDto { EndDate = endDate },
                cancellationToken: cancellationToken);
        }, "Đã cập nhật ngày hết hạn gói.", cancellationToken);
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid id, Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        ValidateSubscriptionId(subscriptionId);
        if (!ModelState.IsValid)
            return await LoadPageAsync(id, cancellationToken);

        return await SaveAsync(id, async () =>
        {
            await apiClient.Api.Manager.Employers[id].Subscriptions[subscriptionId]
                .DeleteAsync(cancellationToken: cancellationToken);
        }, "Đã thu hồi gói của nhà tuyển dụng.", cancellationToken);
    }

    public static string FormatDate(DateTimeOffset? date) =>
        date?.ToOffset(VietnamOffset).ToString("dd/MM/yyyy HH:mm") ?? "—";

    public static string StatusLabel(EmployerSubscriptionStatus? status, DateTimeOffset? endDate) => status switch
    {
        EmployerSubscriptionStatus.Active when endDate < DateTimeOffset.UtcNow => "Đã quá hạn",
        EmployerSubscriptionStatus.Active => "Đang hoạt động",
        EmployerSubscriptionStatus.Expired => "Hết hạn",
        EmployerSubscriptionStatus.Cancelled => "Đã hủy",
        _ => "Không xác định"
    };

    private void ValidateChoice()
    {
        if (Input.Tier is null)
            ModelState.AddModelError("Input.Tier", "Vui lòng chọn hạng gói.");
        if (Input.Duration is null)
            ModelState.AddModelError("Input.Duration", "Vui lòng chọn kỳ hạn gói.");
    }

    private void ValidateSubscriptionId(Guid subscriptionId)
    {
        if (subscriptionId == Guid.Empty)
            ModelState.AddModelError(string.Empty, "Không xác định được gói cần cập nhật.");
    }

    private async Task<IActionResult> SaveAsync(Guid id, Func<Task> action, string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await action();
            TempData.SetSuccessMessage(successMessage);
            return RedirectToPage(new { id });
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ gói. Vui lòng thử lại sau.");
        }

        return await LoadPageAsync(id, cancellationToken);
    }

    private async Task<IActionResult> LoadPageAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            Employer = await apiClient.Api.Manager.Employers[id].GetAsync(cancellationToken: cancellationToken);
            if (Employer is null)
            {
                TempData.SetErrorMessage("Không tìm thấy nhà tuyển dụng.");
                return RedirectToPage("./Index");
            }

            Subscriptions = await apiClient.Api.Manager.Employers[id].Subscriptions
                .GetAsync(cancellationToken: cancellationToken) ?? [];
            SubscriptionsLoaded = true;

            if (Input.EndDateLocal is null && ActiveSubscription?.EndDate is { } endDate)
                Input.EndDateLocal = endDate.ToOffset(VietnamOffset).DateTime;

            return Page();
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404 && Employer is null)
        {
            TempData.SetErrorMessage("Không tìm thấy nhà tuyển dụng.");
            return RedirectToPage("./Index");
        }
        catch (ApiException ex) when (ex.ResponseStatusCode != 401)
        {
            ModelState.AddModelError(string.Empty, ex.ToFriendlyErrorMessage());
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến dịch vụ nhà tuyển dụng. Vui lòng thử lại sau.");
        }

        return Page();
    }
}
