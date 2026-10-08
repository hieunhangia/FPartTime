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
public class DetailModel(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache memoryCache,
    ILogger<DetailModel> logger) : PageModel
{
    public MonitorDetailData DetailData { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string? FromDate { get; set; }

    [BindProperty(SupportsGet = true)] public string? ToDate { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToPage("./Index");
        }

        var cacheKey = $"BetterStack_Monitor_{id}_Detail";
        if (string.IsNullOrEmpty(FromDate) && string.IsNullOrEmpty(ToDate) &&
            memoryCache.TryGetValue(cacheKey, out MonitorDetailData? cached) && cached != null)
        {
            DetailData = cached;
            return Page();
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient("BetterStack");

            // 1. Lấy thông tin Monitor
            var monitorResponse = await httpClient.GetAsync($"monitors/{id}");
            if (!monitorResponse.IsSuccessStatusCode)
            {
                TempData.SetErrorMessage(
                    $"Không tìm thấy dịch vụ hoặc lỗi API (HTTP {(int)monitorResponse.StatusCode}).");
                DetailData = new MonitorDetailData { IsConfigured = false };
                return Page();
            }

            var monitorItem = await monitorResponse.Content
                .ReadFromJsonAsync<BetterStackItemResponse<DetailMonitorAttributes>>();
            var monitorAttr = monitorItem?.Data?.Attributes;
            if (monitorAttr == null)
            {
                TempData.SetErrorMessage("Dữ liệu dịch vụ không hợp lệ.");
                DetailData = new MonitorDetailData { IsConfigured = false };
                return Page();
            }

            // 2. Chạy song song: Response Times, Incidents và các chu kỳ SLA
            var rtTask = FetchResponseTimesAsync(httpClient, id);
            var incidentsTask = FetchMonitorIncidentsAsync(httpClient, id);

            var todaySlaTask = FetchPeriodSlaAsync(httpClient, id, "Hôm nay",
                DateTime.UtcNow.ToString("yyyy-MM-dd"), DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"));

            var weekSlaTask = FetchPeriodSlaAsync(httpClient, id, "7 ngày qua",
                DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"));

            var monthSlaTask = FetchPeriodSlaAsync(httpClient, id, "30 ngày qua",
                DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"));

            var yearSlaTask = FetchPeriodSlaAsync(httpClient, id, "365 ngày qua",
                DateTime.UtcNow.AddDays(-365).ToString("yyyy-MM-dd"), DateTime.UtcNow.ToString("yyyy-MM-dd"));

            var allTimeSlaTask = FetchPeriodSlaAsync(httpClient, id, "Toàn bộ thời gian", null, null);

            Task<SlaPeriodItem?>? customSlaTask = null;
            if (!string.IsNullOrWhiteSpace(FromDate) && !string.IsNullOrWhiteSpace(ToDate))
            {
                customSlaTask = FetchPeriodSlaAsync(httpClient, id, $"Tùy chọn ({FromDate} đến {ToDate})", FromDate,
                    ToDate);
            }

            await Task.WhenAll(
                rtTask, incidentsTask, todaySlaTask, weekSlaTask, monthSlaTask, yearSlaTask, allTimeSlaTask,
                customSlaTask ?? Task.CompletedTask
            );

            var incidents = incidentsTask.Result;
            var chartPoints = rtTask.Result;

            // Tính "Thời gian hoạt động liên tục"
            var currentlyUpFor = CalculateCurrentlyUpFor(monitorAttr.Status, monitorAttr.CreatedAt, incidents);
            var lastCheckedAtAgo = FormatRelativeTime(monitorAttr.LastCheckedAt);
            var rawStatus = monitorAttr.Status ?? "unknown";

            var slaItems = new List<SlaPeriodItem>
            {
                todaySlaTask.Result ?? CreateFallbackSla("Hôm nay"),
                weekSlaTask.Result ?? CreateFallbackSla("7 ngày qua"),
                monthSlaTask.Result ?? CreateFallbackSla("30 ngày qua"),
                yearSlaTask.Result ?? CreateFallbackSla("365 ngày qua"),
                allTimeSlaTask.Result ?? CreateFallbackSla("Toàn bộ thời gian")
            };

            if (customSlaTask?.Result != null)
            {
                slaItems.Add(customSlaTask.Result);
            }

            DetailData = new MonitorDetailData
            {
                IsConfigured = true,
                MonitorId = id,
                Name = monitorAttr.PronounceableName ?? monitorAttr.Url ?? $"Dịch vụ #{id}",
                Url = monitorAttr.Url ?? string.Empty,
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
                CurrentlyUpFor = currentlyUpFor,
                LastCheckedAgo = lastCheckedAtAgo,
                IncidentsCount = incidents.Count,
                ChartPoints = chartPoints,
                SlaPeriods = slaItems
            };

            if (string.IsNullOrEmpty(FromDate) && string.IsNullOrEmpty(ToDate))
            {
                memoryCache.Set(cacheKey, DetailData, TimeSpan.FromMinutes(3));
            }

            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading monitor detail for {Id}", id);
            TempData.SetErrorMessage("Đã xảy ra lỗi khi kết nối đến dịch vụ giám sát.");
            DetailData = new MonitorDetailData { IsConfigured = false };
            return Page();
        }
    }

    private static string CalculateCurrentlyUpFor(string? status, DateTime? createdAt,
        List<DetailIncidentItem> incidents)
    {
        if (!string.Equals(status, "up", StringComparison.OrdinalIgnoreCase))
        {
            return "Đang gặp sự cố";
        }

        var lastResolved = incidents.Where(i => i.ResolvedAt.HasValue).OrderByDescending(i => i.ResolvedAt)
            .FirstOrDefault();
        var startTime = lastResolved?.ResolvedAt ?? createdAt ?? DateTime.UtcNow;
        var diff = DateTime.UtcNow - startTime;
        if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;

        if (diff.TotalDays >= 1)
        {
            return $"{(int)diff.TotalDays} ngày {diff.Hours} giờ";
        }

        if (diff.TotalHours >= 1)
        {
            return $"{(int)diff.TotalHours} giờ {diff.Minutes} phút";
        }

        return $"{Math.Max(1, (int)diff.TotalMinutes)} phút";
    }

    private static string FormatRelativeTime(DateTime? time)
    {
        if (!time.HasValue) return "—";
        var diff = DateTime.UtcNow - time.Value;
        if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;

        if (diff.TotalSeconds < 60)
        {
            return $"{(int)diff.TotalSeconds} giây trước";
        }

        if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes} phút trước";
        }

        if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours} giờ trước";
        }

        return $"{(int)diff.TotalDays} ngày trước";
    }

    private static string FormatDuration(double seconds)
    {
        if (seconds <= 0) return "Không có";
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalMinutes < 1) return $"{ts.Seconds} giây";
        if (ts.TotalHours < 1) return $"{(int)ts.TotalMinutes} phút {ts.Seconds} giây";
        if (ts.TotalDays < 1) return $"{(int)ts.TotalHours} giờ {ts.Minutes} phút";
        return $"{(int)ts.TotalDays} ngày {ts.Hours} giờ";
    }

    private static SlaPeriodItem CreateFallbackSla(string period) => new()
    {
        PeriodName = period,
        AvailabilityText = "100.0000%",
        DowntimeText = "Không có",
        IncidentsCount = 0,
        LongestIncidentText = "Không có",
        AvgIncidentText = "Không có"
    };

    private async Task<SlaPeriodItem?> FetchPeriodSlaAsync(
        HttpClient httpClient, string monitorId, string periodName, string? from, string? to)
    {
        try
        {
            var url = $"monitors/{monitorId}/sla";
            if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            {
                url += $"?from={from}&to={to}";
            }

            var slaData = await httpClient.GetFromJsonAsync<BetterStackItemResponse<DetailMonitorSlaAttributes>>(url);
            var attr = slaData?.Data?.Attributes;
            if (attr != null)
            {
                var availability = attr.Availability ?? 100.0;
                return new SlaPeriodItem
                {
                    PeriodName = periodName,
                    AvailabilityText = $"{availability:F4}%",
                    DowntimeText = FormatDuration(attr.TotalDowntime ?? 0),
                    IncidentsCount = attr.NumberOfIncidents ?? 0,
                    LongestIncidentText = FormatDuration(attr.LongestIncident ?? 0),
                    AvgIncidentText = FormatDuration(attr.AverageIncident ?? 0)
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to fetch SLA period {Period} for monitor {Id}", periodName, monitorId);
        }

        return null;
    }

    private async Task<List<ResponseTimePoint>> FetchResponseTimesAsync(HttpClient httpClient, string monitorId)
    {
        try
        {
            using var response = await httpClient.GetAsync($"monitors/{monitorId}/response-times");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Better Stack response-times HTTP {StatusCode} for monitor {Id}", response.StatusCode,
                    monitorId);
                return [];
            }

            var element = await response.Content.ReadFromJsonAsync<JsonElement>();
            var result = new List<ResponseTimePoint>();

            FindAndParsePoints(element, result);

            return result.OrderBy(p => p.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch response times for monitor {Id}", monitorId);
            return [];
        }
    }

    private static void FindAndParsePoints(JsonElement element, List<ResponseTimePoint> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    FindAndParsePoints(item, result);
                }

                break;

            case JsonValueKind.Object:
                var target = element.TryGetProperty("attributes", out var attr) &&
                             attr.ValueKind == JsonValueKind.Object
                    ? attr
                    : element;

                if (TryExtractAt(target, out var dt) && HasTimingData(target))
                {
                    AddPoint(target, dt, result);
                }
                else
                {
                    foreach (var prop in element.EnumerateObject())
                    {
                        FindAndParsePoints(prop.Value, result);
                    }
                }

                break;
        }
    }

    private static bool TryExtractAt(JsonElement target, out DateTime dt)
    {
        dt = default;
        foreach (var name in new[] { "at", "timestamp", "created_at", "time" })
        {
            if (target.TryGetProperty(name, out var atProp))
            {
                if (atProp.TryGetDateTime(out dt)) return true;
                if (atProp.ValueKind == JsonValueKind.String && DateTime.TryParse(atProp.GetString(), out dt))
                    return true;
                if (atProp.TryGetInt64(out var unixTime))
                {
                    dt = unixTime > 1_000_000_000_000
                        ? DateTimeOffset.FromUnixTimeMilliseconds(unixTime).UtcDateTime
                        : DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasTimingData(JsonElement target)
    {
        return target.TryGetProperty("response_time", out _) ||
               target.TryGetProperty("data_transfer_time", out _) ||
               target.TryGetProperty("connection_time", out _);
    }

    private static void AddPoint(JsonElement target, DateTime at, List<ResponseTimePoint> result)
    {
        double GetMs(string propName)
        {
            if (target.TryGetProperty(propName, out var p) && p.TryGetDouble(out var sec))
            {
                return sec < 10.0 ? Math.Round(sec * 1000.0, 1) : Math.Round(sec, 1);
            }

            return 0;
        }

        var total = GetMs("response_time");
        var lookup = GetMs("name_lookup_time");
        var conn = GetMs("connection_time");
        var tls = GetMs("tls_handshake_time");
        var data = GetMs("data_transfer_time");

        if (lookup == 0 && conn == 0 && tls == 0 && data == 0 && total > 0)
        {
            data = total;
        }

        result.Add(new ResponseTimePoint
        {
            Timestamp = at,
            EpochMs = (at.Kind == DateTimeKind.Utc ? new DateTimeOffset(at) : new DateTimeOffset(at, TimeSpan.Zero))
                .ToUnixTimeMilliseconds(),
            At = at.ToLocalTime().ToString("HH:mm:ss"),
            NameLookupMs = lookup,
            ConnectionMs = conn,
            TlsHandshakeMs = tls,
            DataTransferMs = data,
            TotalMs = total
        });
    }

    private async Task<List<DetailIncidentItem>> FetchMonitorIncidentsAsync(HttpClient httpClient, string monitorId)
    {
        try
        {
            var incidentsData = await httpClient.GetFromJsonAsync<BetterStackListResponse<DetailIncidentAttributes>>(
                $"incidents?monitor_id={monitorId}");

            return incidentsData?.Data.Select(i => new DetailIncidentItem
            {
                Id = i.Id,
                StartedAt = i.Attributes.StartedAt,
                ResolvedAt = i.Attributes.ResolvedAt
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to fetch incidents for monitor {Id}", monitorId);
            return [];
        }
    }
}

public class MonitorDetailData
{
    public bool IsConfigured { get; init; } = true;
    public string MonitorId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Status { get; init; } = "unknown";
    public string StatusText { get; init; } = "Không rõ";

    public string CurrentlyUpFor { get; init; } = "—";
    public string LastCheckedAgo { get; init; } = "—";
    public int IncidentsCount { get; init; }

    public IReadOnlyList<ResponseTimePoint> ChartPoints { get; init; } = [];
    public IReadOnlyList<SlaPeriodItem> SlaPeriods { get; init; } = [];
}

public class ResponseTimePoint
{
    [JsonIgnore] public DateTime Timestamp { get; init; }

    public long EpochMs { get; init; }
    public string At { get; init; } = string.Empty;
    public double NameLookupMs { get; init; }
    public double ConnectionMs { get; init; }
    public double TlsHandshakeMs { get; init; }
    public double DataTransferMs { get; init; }
    public double TotalMs { get; init; }
}

public class SlaPeriodItem
{
    public string PeriodName { get; init; } = string.Empty;
    public string AvailabilityText { get; init; } = "100.0000%";
    public string DowntimeText { get; init; } = "Không có";
    public int IncidentsCount { get; init; }
    public string LongestIncidentText { get; init; } = "Không có";
    public string AvgIncidentText { get; init; } = "Không có";
}

public class DetailMonitorAttributes
{
    [JsonPropertyName("url")] public string? Url { get; set; }

    [JsonPropertyName("pronounceable_name")]
    public string? PronounceableName { get; set; }

    [JsonPropertyName("status")] public string? Status { get; set; }

    [JsonPropertyName("last_checked_at")] public DateTime? LastCheckedAt { get; set; }

    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
}

public class DetailMonitorSlaAttributes
{
    [JsonPropertyName("availability")] public double? Availability { get; set; }

    [JsonPropertyName("total_downtime")] public double? TotalDowntime { get; set; }

    [JsonPropertyName("number_of_incidents")]
    public int? NumberOfIncidents { get; set; }

    [JsonPropertyName("longest_incident")] public double? LongestIncident { get; set; }

    [JsonPropertyName("average_incident")] public double? AverageIncident { get; set; }
}

public class DetailIncidentItem
{
    public string Id { get; init; } = string.Empty;
    public DateTime? StartedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public class DetailIncidentAttributes
{
    [JsonPropertyName("started_at")] public DateTime? StartedAt { get; set; }

    [JsonPropertyName("resolved_at")] public DateTime? ResolvedAt { get; set; }
}