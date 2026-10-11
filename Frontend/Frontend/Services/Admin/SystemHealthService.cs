using System.Text.Json;
using System.Text.Json.Serialization;

namespace Frontend.Services.Admin;

public class SystemHealthService(IHttpClientFactory httpClientFactory)
{
    public const string BetterStackClientName = "BetterStack";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<SystemHealthData> GetSystemHealthDataAsync(CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(BetterStackClientName);
        using var monitorsResponse = await httpClient.GetAsync("monitors", cancellationToken);
        monitorsResponse.EnsureSuccessStatusCode();

        var listResponse = await monitorsResponse.Content
            .ReadFromJsonAsync<BetterStackListResponse<MonitorAttributes>>(JsonOptions, cancellationToken);

        var monitorTasks = listResponse?.Data?
            .Select(m => FetchMonitorItemAsync(httpClient, m, cancellationToken))
            .ToList() ?? [];

        var incidentsTask = FetchIncidentsAsync(httpClient, cancellationToken);

        await Task.WhenAll(Task.WhenAll(monitorTasks), incidentsTask);

        var monitors = monitorTasks.Select(t => t.Result).ToList();
        var upCount = 0;
        var downCount = 0;
        double slaSum = 0;
        var slaCount = 0;
        var hasDegraded = false;

        foreach (var m in monitors)
        {
            switch (m.Status)
            {
                case MonitorStatus.Up:
                    upCount++;
                    break;
                case MonitorStatus.Down:
                    downCount++;
                    break;
                default:
                    hasDegraded = true;
                    break;
            }

            if (m.SlaAvailability.HasValue)
            {
                slaSum += m.SlaAvailability.Value;
                slaCount++;
            }
        }

        return new SystemHealthData
        {
            OverallStatus = downCount > 0 ? OverallStatus.Outage :
                hasDegraded ? OverallStatus.Degraded : OverallStatus.Operational,
            TotalMonitors = monitors.Count,
            UpMonitors = upCount,
            DownMonitors = downCount,
            AverageSla = slaCount > 0 ? Math.Round(slaSum / slaCount, 2) : null,
            Monitors = monitors,
            Incidents = incidentsTask.Result
        };
    }

    public async Task<MonitorDetailData?> GetMonitorDetailAsync(string id, string? fromDate = null,
        string? toDate = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var httpClient = httpClientFactory.CreateClient(BetterStackClientName);
        using var monitorResponse = await httpClient.GetAsync($"monitors/{id}", cancellationToken);
        if (!monitorResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var monitorAttr = (await monitorResponse.Content
            .ReadFromJsonAsync<BetterStackItemResponse<DetailMonitorAttributes>>(JsonOptions, cancellationToken))?.Data?.Attributes;
        if (monitorAttr == null)
        {
            return null;
        }

        var rtTask = FetchResponseTimesAsync(httpClient, id, cancellationToken);
        var incidentsTask = FetchMonitorIncidentsAsync(httpClient, id, cancellationToken);
        var todaySlaTask = FetchPeriodSlaAsync(httpClient, id, "Hôm nay",
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"), cancellationToken);
        var weekSlaTask = FetchPeriodSlaAsync(httpClient, id, "7 ngày qua",
            DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd"),
            DateTime.UtcNow.ToString("yyyy-MM-dd"), cancellationToken);
        var monthSlaTask = FetchPeriodSlaAsync(httpClient, id, "30 ngày qua",
            DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
            DateTime.UtcNow.ToString("yyyy-MM-dd"), cancellationToken);
        var yearSlaTask = FetchPeriodSlaAsync(httpClient, id, "365 ngày qua",
            DateTime.UtcNow.AddDays(-365).ToString("yyyy-MM-dd"),
            DateTime.UtcNow.ToString("yyyy-MM-dd"), cancellationToken);

        var allTimeSlaTask = FetchPeriodSlaAsync(httpClient, id, "Toàn bộ thời gian", null, null, cancellationToken);

        Task<SlaPeriodItem?>? customSlaTask = null;
        if (!string.IsNullOrWhiteSpace(fromDate) && !string.IsNullOrWhiteSpace(toDate))
        {
            customSlaTask = FetchPeriodSlaAsync(httpClient, id, $"Tùy chọn ({fromDate} đến {toDate})",
                fromDate, toDate, cancellationToken);
        }

        await Task.WhenAll(rtTask, incidentsTask, todaySlaTask, weekSlaTask, monthSlaTask, yearSlaTask,
            allTimeSlaTask, customSlaTask ?? Task.CompletedTask);

        var incidents = incidentsTask.Result;
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

        return new MonitorDetailData
        {
            MonitorId = id,
            Name = monitorAttr.PronounceableName ?? monitorAttr.Url ?? $"Dịch vụ #{id}",
            Url = monitorAttr.Url ?? string.Empty,
            Status = rawStatus,
            StatusText = MapStatusText(rawStatus),
            CurrentlyUpFor = CalculateCurrentlyUpFor(monitorAttr.Status, monitorAttr.CreatedAt, incidents),
            LastCheckedAgo = FormatRelativeTime(monitorAttr.LastCheckedAt),
            IncidentsCount = incidents.Count,
            ChartPoints = rtTask.Result,
            SlaPeriods = slaItems
        };
    }

    private static string MapStatusText(string? status) => status switch
    {
        MonitorStatus.Up => "Hoạt động",
        MonitorStatus.Down => "Gặp sự cố",
        MonitorStatus.Pending => "Đang kiểm tra",
        MonitorStatus.Paused => "Tạm dừng",
        MonitorStatus.Validating => "Đang xác thực",
        MonitorStatus.Maintenance => "Bảo trì",
        _ => "Không xác định"
    };

    private static async Task<List<IncidentItem>> FetchIncidentsAsync(HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<BetterStackListResponse<IncidentAttributes>>(
                "incidents", JsonOptions, cancellationToken);
            return response?.Data?.Select(i => new IncidentItem
                {
                    Name = i.Attributes?.Name ?? $"Sự cố #{i.Id}",
                    Cause = i.Attributes?.Cause,
                    Url = i.Attributes?.Url,
                    StartedAt = i.Attributes?.StartedAt,
                    ResolvedAt = i.Attributes?.ResolvedAt
                })
                .OrderByDescending(i => i.StartedAt)
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task<MonitorItem> FetchMonitorItemAsync(HttpClient httpClient,
        BetterStackResource<MonitorAttributes> m,
        CancellationToken cancellationToken)
    {
        var monitorId = m.Id ?? string.Empty;
        var slaTask = FetchSlaAsync(httpClient, monitorId, cancellationToken);
        var rtTask = FetchResponseTimeAsync(httpClient, monitorId, cancellationToken);

        await Task.WhenAll(slaTask, rtTask);

        var rawStatus = m.Attributes?.Status ?? "unknown";
        return new MonitorItem
        {
            Id = monitorId,
            Name = m.Attributes?.PronounceableName ?? m.Attributes?.Url ?? $"Dịch vụ #{monitorId}",
            Url = m.Attributes?.Url ?? string.Empty,
            Status = rawStatus,
            StatusText = MapStatusText(rawStatus),
            LastCheckedAt = m.Attributes?.LastCheckedAt,
            SlaAvailability = slaTask.Result,
            AvgResponseTimeMs = rtTask.Result
        };
    }

    private static async Task<double?> FetchSlaAsync(HttpClient httpClient, string monitorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(monitorId)) return null;
        try
        {
            var slaData = await httpClient.GetFromJsonAsync<BetterStackItemResponse<MonitorSlaAttributes>>(
                $"monitors/{monitorId}/sla", JsonOptions, cancellationToken);
            if (slaData?.Data?.Attributes?.Availability != null)
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

    private static async Task<double?> FetchResponseTimeAsync(
        HttpClient httpClient,
        string monitorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(monitorId)) return null;
        try
        {
            var element = await httpClient.GetFromJsonAsync<JsonElement>(
                $"monitors/{monitorId}/response-times", cancellationToken);
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

    private static async Task<SlaPeriodItem?> FetchPeriodSlaAsync(HttpClient httpClient, string monitorId,
        string periodName, string? from, string? to, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"monitors/{monitorId}/sla";
            if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
            {
                url += $"?from={from}&to={to}";
            }

            var slaData = await httpClient.GetFromJsonAsync<BetterStackItemResponse<DetailMonitorSlaAttributes>>(
                url, JsonOptions, cancellationToken);
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
        catch
        {
            // ignored
        }

        return null;
    }

    private static async Task<List<DetailIncidentItem>> FetchMonitorIncidentsAsync(HttpClient httpClient,
        string monitorId, CancellationToken cancellationToken)
    {
        try
        {
            var incidentsData = await httpClient.GetFromJsonAsync<BetterStackListResponse<DetailIncidentAttributes>>(
                $"incidents?monitor_id={monitorId}", JsonOptions, cancellationToken);

            return incidentsData?.Data?
                .Select(i => new DetailIncidentItem
                {
                    Id = i.Id ?? string.Empty,
                    StartedAt = i.Attributes?.StartedAt,
                    ResolvedAt = i.Attributes?.ResolvedAt
                }).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task<List<ResponseTimePoint>> FetchResponseTimesAsync(HttpClient httpClient, string monitorId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync($"monitors/{monitorId}/response-times", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var element = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var result = new List<ResponseTimePoint>();

            FindAndParsePoints(element, result);

            return result.OrderBy(p => p.Timestamp).ToList();
        }
        catch
        {
            return [];
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

        static bool TryExtractAt(JsonElement target, out DateTime dt)
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

        static bool HasTimingData(JsonElement target)
        {
            return target.TryGetProperty("response_time", out _) ||
                   target.TryGetProperty("data_transfer_time", out _) ||
                   target.TryGetProperty("connection_time", out _);
        }
    }

    private static void AddPoint(JsonElement target, DateTime at, List<ResponseTimePoint> result)
    {
        static double GetMs(JsonElement element, string propName)
        {
            if (element.TryGetProperty(propName, out var p) && p.TryGetDouble(out var sec))
            {
                return sec < 10.0 ? Math.Round(sec * 1000.0, 1) : Math.Round(sec, 1);
            }

            return 0;
        }

        var total = GetMs(target, "response_time");
        var lookup = GetMs(target, "name_lookup_time");
        var conn = GetMs(target, "connection_time");
        var tls = GetMs(target, "tls_handshake_time");
        var data = GetMs(target, "data_transfer_time");

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

    private static string CalculateCurrentlyUpFor(string? status, DateTime? createdAt,
        List<DetailIncidentItem> incidents)
    {
        if (status != MonitorStatus.Up)
        {
            return "Đang gặp sự cố";
        }

        var lastResolved = incidents
            .Where(i => i.ResolvedAt.HasValue)
            .OrderByDescending(i => i.ResolvedAt)
            .FirstOrDefault();

        var diff = DateTime.UtcNow - (lastResolved?.ResolvedAt ?? createdAt ?? DateTime.UtcNow);
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
        AvailabilityText = "Không có dữ liệu",
        DowntimeText = "Không có",
        IncidentsCount = 0,
        LongestIncidentText = "Không có",
        AvgIncidentText = "Không có"
    };
}

public class SystemHealthData
{
    public OverallStatus OverallStatus { get; init; }
    public int TotalMonitors { get; init; }
    public int UpMonitors { get; init; }
    public int DownMonitors { get; init; }
    public double? AverageSla { get; init; }
    public required IReadOnlyList<MonitorItem> Monitors { get; init; }
    public required IReadOnlyList<IncidentItem> Incidents { get; init; }
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
    public required string Name { get; init; }
    public string? Cause { get; init; }
    public string? Url { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public class MonitorDetailData
{
    public required string MonitorId { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string Status { get; init; }
    public required string StatusText { get; init; }

    public required string CurrentlyUpFor { get; init; }
    public required string LastCheckedAgo { get; init; }
    public int IncidentsCount { get; init; }

    public required IReadOnlyList<ResponseTimePoint> ChartPoints { get; init; }
    public required IReadOnlyList<SlaPeriodItem> SlaPeriods { get; init; }
}

public class ResponseTimePoint
{
    [JsonIgnore] public DateTime Timestamp { get; init; }

    public long EpochMs { get; init; }
    public required string At { get; init; }
    public double NameLookupMs { get; init; }
    public double ConnectionMs { get; init; }
    public double TlsHandshakeMs { get; init; }
    public double DataTransferMs { get; init; }
    public double TotalMs { get; init; }
}

public class SlaPeriodItem
{
    public required string PeriodName { get; init; }
    public required string AvailabilityText { get; init; }
    public required string DowntimeText { get; init; }
    public int IncidentsCount { get; init; }
    public required string LongestIncidentText { get; init; }
    public required string AvgIncidentText { get; init; }
}

public class DetailIncidentItem
{
    public required string Id { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public enum OverallStatus
{
    Operational,
    Outage,
    Degraded
}

public static class MonitorStatus
{
    public const string Up = "up";
    public const string Down = "down";
    public const string Pending = "pending";
    public const string Paused = "paused";
    public const string Validating = "validating";
    public const string Maintenance = "maintenance";
}

public class BetterStackListResponse<T>
{
    public List<BetterStackResource<T>>? Data { get; set; }
}

public class BetterStackItemResponse<T>
{
    public BetterStackResource<T>? Data { get; set; }
}

public class BetterStackResource<T>
{
    public string? Id { get; set; }
    public T? Attributes { get; set; }
}

public class MonitorAttributes
{
    public string? Url { get; set; }
    public string? PronounceableName { get; set; }
    public string? Status { get; set; }
    public DateTime? LastCheckedAt { get; set; }
}

public class MonitorSlaAttributes
{
    public double? Availability { get; set; }
}

public class IncidentAttributes
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? Cause { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class DetailMonitorAttributes
{
    public string? Url { get; set; }
    public string? PronounceableName { get; set; }
    public string? Status { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class DetailMonitorSlaAttributes
{
    public double? Availability { get; set; }
    public double? TotalDowntime { get; set; }
    public int? NumberOfIncidents { get; set; }
    public double? LongestIncident { get; set; }
    public double? AverageIncident { get; set; }
}

public class DetailIncidentAttributes
{
    public DateTime? StartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}