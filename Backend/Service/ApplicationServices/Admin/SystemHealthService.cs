using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Riok.Mapperly.Abstractions;

namespace Service.ApplicationServices.Admin;

public class SystemHealthService(HealthCheckService healthCheckService)
{
    public async Task<SystemHealthResponseDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        var uptimePartString = new List<string>();
        if (uptime.Days > 0) uptimePartString.Add($"{uptime.Days} ngày");
        if (uptime.Hours > 0) uptimePartString.Add($"{uptime.Hours} giờ");
        if (uptime.Minutes > 0) uptimePartString.Add($"{uptime.Minutes} phút");
        if (uptime.Seconds > 0) uptimePartString.Add($"{uptime.Seconds} giây");

        return new SystemHealthResponseDto
        {
            Status = report.Status,
            Uptime = string.Join(" ", uptimePartString),
            MemoryAllocatedMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2),
            Services = [.. report.Entries.Select(entry => entry.Value.MapToDto(entry.Key))]
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class SystemHealthMapper
{
    [MapProperty(nameof(HealthReportEntry.Duration), nameof(ServiceHealthDto.ResponseTimeMs),
        Use = nameof(MapDurationToMilliseconds))]
    [MapProperty(nameof(HealthReportEntry.Exception.StackTrace), nameof(ServiceHealthDto.ErrorStackTrace))]
    public static partial ServiceHealthDto MapToDto(this HealthReportEntry entry, string name);

    [UserMapping(Default = false)]
    private static double MapDurationToMilliseconds(TimeSpan duration) => Math.Round(duration.TotalMilliseconds, 2);
}

public class SystemHealthResponseDto
{
    public required HealthStatus Status { get; set; }
    public required string Uptime { get; set; }
    public required double MemoryAllocatedMB { get; set; }
    public required List<ServiceHealthDto> Services { get; set; }
}

public class ServiceHealthDto
{
    public required string Name { get; set; }
    public required HealthStatus Status { get; set; }
    public required double ResponseTimeMs { get; set; }
    public required string? Description { get; set; }
    public required string? ErrorStackTrace { get; set; }
}