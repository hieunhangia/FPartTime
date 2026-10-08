using System.Text.Json;
using System.Text.Json.Serialization;
using Frontend.Constants;
using Frontend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace Frontend.Pages.Admin.SystemHealth;

[Authorize(Roles = Role.Admin)]
public class IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, IMemoryCache memoryCache)
    : PageModel
{
    private const string CacheKey = "BetterStack_SystemHealth_Cache";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(3);
    public SystemHealthData HealthData { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (memoryCache.TryGetValue(CacheKey, out SystemHealthData? cached) && cached != null)
        {
            HealthData = cached;
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("BetterStack");

            using var monitorsResponse = await httpClient.GetAsync("monitors");
            if (!monitorsResponse.IsSuccessStatusCode)
            {
                TempData.SetErrorMessage(
                    $"Không thể tải dữ liệu giám sát hệ thống (HTTP {(int)monitorsResponse.StatusCode}).");
                HealthData = new SystemHealthData();
            }

            var monitorsData =
                await monitorsResponse.Content.ReadFromJsonAsync<BetterStackListResponse<MonitorAttributes>>();
            var rawMonitors = monitorsData?.Data ?? [];

            // Chạy song song: Tất cả monitors (SLA + Độ trễ) và danh sách Incidents đồng thời
            var monitorTasks = rawMonitors.Select(m => FetchMonitorItemAsync(httpClient, m)).ToList();
            var incidentsTask = FetchIncidentsAsync(httpClient);

            await Task.WhenAll(Task.WhenAll(monitorTasks), incidentsTask);

            var monitors = monitorTasks.Select(t => t.Result).ToList();
            var incidents = incidentsTask.Result;

            // Tính toán toàn bộ chỉ số trong 1 lần duyệt duy nhất
            var upCount = 0;
            var downCount = 0;
            double slaSum = 0;
            var slaCount = 0;
            var hasDegraded = false;

            foreach (var m in monitors)
            {
                if (m.Status.Equals("up", StringComparison.OrdinalIgnoreCase))
                {
                    upCount++;
                }
                else if (m.Status.Equals("down", StringComparison.OrdinalIgnoreCase))
                {
                    downCount++;
                }
                else if (!m.Status.Equals("paused", StringComparison.OrdinalIgnoreCase))
                {
                    hasDegraded = true;
                }

                if (m.SlaAvailability.HasValue)
                {
                    slaSum += m.SlaAvailability.Value;
                    slaCount++;
                }
            }

            var result = new SystemHealthData
            {
                IsConfigured = true,
                OverallStatus = downCount > 0 ? "outage" : hasDegraded ? "degraded" : "operational",
                TotalMonitors = monitors.Count,
                UpMonitors = upCount,
                DownMonitors = downCount,
                AverageSla = slaCount > 0 ? Math.Round(slaSum / slaCount, 2) : null,
                Monitors = monitors,
                Incidents = incidents
            };

            memoryCache.Set(CacheKey, result, CacheDuration);
            HealthData = result;
        }
        catch
        {
            TempData.SetErrorMessage("Đã xảy ra lỗi khi kết nối đến dịch vụ giám sát.");
            HealthData = new SystemHealthData();
        }

        return Page();

        async Task<List<IncidentItem>> FetchIncidentsAsync(HttpClient httpClient)
        {
            try
            {
                return (await httpClient.GetFromJsonAsync<BetterStackListResponse<IncidentAttributes>>("incidents"))
                    ?.Data
                    .Select(i => new IncidentItem
                    {
                        Id = i.Id,
                        Name = i.Attributes.Name ?? $"Sự cố #{i.Id}",
                        Cause = i.Attributes.Cause,
                        Url = i.Attributes.Url,
                        StartedAt = i.Attributes.StartedAt,
                        ResolvedAt = i.Attributes.ResolvedAt
                    })
                    .OrderByDescending(i => i.StartedAt)
                    .ToList() ?? [];
            }
            catch
            {
                return [];
            }
        }

        async Task<MonitorItem> FetchMonitorItemAsync(HttpClient httpClient, BetterStackResource<MonitorAttributes> m)
        {
            var slaTask = FetchSlaAsync();
            var rtTask = FetchResponseTimeAsync();

            await Task.WhenAll(slaTask, rtTask);

            var rawStatus = m.Attributes.Status ?? "unknown";
            return new MonitorItem
            {
                Id = m.Id,
                Name = m.Attributes.PronounceableName ?? m.Attributes.Url ?? $"Dịch vụ #{m.Id}",
                Url = m.Attributes.Url ?? string.Empty,
                Status = rawStatus,
                StatusText = rawStatus switch
                {
                    "up" => "Hoạt động",
                    "down" => "Gặp sự cố",
                    "paused" => "Tạm dừng",
                    "validating" => "Đang xác thực",
                    "maintenance" => "Bảo trì",
                    _ => "Không rõ"
                },
                LastCheckedAt = m.Attributes.LastCheckedAt,
                SlaAvailability = slaTask.Result,
                AvgResponseTimeMs = rtTask.Result
            };

            async Task<double?> FetchSlaAsync()
            {
                try
                {
                    var slaData = await httpClient.GetFromJsonAsync<BetterStackItemResponse<MonitorSlaAttributes>>(
                        $"monitors/{m.Id}/sla");
                    if (slaData?.Data?.Attributes.Availability != null)
                    {
                        return Math.Round(slaData.Data.Attributes.Availability.Value, 2);
                    }
                }
                catch
                {
                    // ignored
                }

                return null;
            }

            async Task<double?> FetchResponseTimeAsync()
            {
                try
                {
                    var element = await httpClient.GetFromJsonAsync<JsonElement>($"monitors/{m.Id}/response-times");
                    var times = new List<double>();
                    ExtractResponseTimes(element, times);
                    if (times.Count > 0)
                    {
                        return Math.Round(times.Average(), 0);
                    }
                }
                catch
                {
                    // ignored
                }

                return null;
            }
        }
    }

    private static void ExtractResponseTimes(JsonElement element, List<double> times)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.NameEquals("response_time"))
                    {
                        if (prop.Value.TryGetDouble(out var val) && val > 0)
                        {
                            var ms = val < 10.0 ? val * 1000.0 : val;
                            times.Add(ms);
                        }
                    }
                    else
                    {
                        ExtractResponseTimes(prop.Value, times);
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    ExtractResponseTimes(item, times);
                }

                break;
        }
    }
}

public class SystemHealthData
{
    public bool IsConfigured { get; init; } = true;
    public string OverallStatus { get; init; } = "operational";

    public int TotalMonitors { get; init; }
    public int UpMonitors { get; init; }
    public int DownMonitors { get; init; }
    public double? AverageSla { get; init; }

    public IReadOnlyList<MonitorItem> Monitors { get; init; } = [];
    public IReadOnlyList<IncidentItem> Incidents { get; init; } = [];
}

public class MonitorItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string Status { get; init; }
    public required string StatusText { get; init; }

    public DateTime? LastCheckedAt { get; init; }
    public double? SlaAvailability { get; init; }
    public double? AvgResponseTimeMs { get; init; }
}

public class IncidentItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Cause { get; init; }
    public string? Url { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public TimeSpan? Duration => ResolvedAt.HasValue && StartedAt.HasValue ? ResolvedAt.Value - StartedAt.Value : null;
    public bool IsResolved => ResolvedAt.HasValue;
}

public class BetterStackListResponse<T>
{
    [JsonPropertyName("data")] public List<BetterStackResource<T>> Data { get; set; } = [];
}

public class BetterStackItemResponse<T>
{
    [JsonPropertyName("data")] public BetterStackResource<T>? Data { get; set; }
}

public class BetterStackResource<T>
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("attributes")] public T Attributes { get; set; } = default!;
}

public class MonitorAttributes
{
    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("pronounceable_name")]
    public string? PronounceableName { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("last_checked_at")] public DateTime? LastCheckedAt { get; set; }
}

public class MonitorSlaAttributes
{
    [JsonPropertyName("availability")] public double? Availability { get; set; }
}

public class IncidentAttributes
{
    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("cause")] public string? Cause { get; set; }

    [JsonPropertyName("started_at")] public DateTime? StartedAt { get; set; }

    [JsonPropertyName("resolved_at")] public DateTime? ResolvedAt { get; set; }
}