using Contracts.Application.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class IndustriesController(IIndustryService industryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<IndustryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllIndustries() => Ok(await industryService.GetAllIndustriesAsync());

    [HttpGet("popular")]
    [AllowAnonymous]
    [ProducesResponseType<List<PopularIndustryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPopularIndustries([FromQuery] GetPopularIndustriesRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await industryService.GetPopularIndustriesAsync(request.Limit, cancellationToken));
}
