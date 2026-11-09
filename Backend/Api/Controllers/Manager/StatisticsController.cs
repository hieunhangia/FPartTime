using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Manager;
using static Service.Manager.StatisticService;

namespace Api.Controllers.Manager
{
    [Route("api/manager/[controller]")]
    [ApiController]
    [Authorize(Roles = Role.Manager)]
    public class StatisticsController(StatisticService statisticService) : ControllerBase
    {
        
        [HttpGet("overview")]
        [ProducesResponseType<OverviewKpiResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetOverview([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var result = await statisticService.GetOverviewKpisAsync(fromDate, toDate);
            return Ok(result);
        }
       
        [HttpGet("revenue")]
        [ProducesResponseType<RevenueAnalyticsResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRevenue([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var result = await statisticService.GetRevenueAnalyticsAsync(fromDate, toDate);
            return Ok(result);
        }
       
        [HttpGet("marketplace")]
        [ProducesResponseType<MarketplaceAnalyticsResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMarketplace([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var result = await statisticService.GetMarketplaceAnalyticsAsync(fromDate, toDate);
            return Ok(result);
        }
       
        [HttpGet("quality-retention")]
        [ProducesResponseType<QualityAndRetentionResponseDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetQualityAndRetention()
        {
            var result = await statisticService.GetQualityAndRetentionAsync();
            return Ok(result);
        }

        [HttpGet("export-excel")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportExcel([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var fileBytes = await statisticService.ExportExcelAsync(fromDate, toDate);
            var fileName = $"BaoCao_FPartTime_{DateTime.UtcNow.AddHours(7):yyyyMMdd_HHmm}.xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}
