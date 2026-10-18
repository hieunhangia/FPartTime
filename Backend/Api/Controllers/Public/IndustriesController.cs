using Contracts.Application.Public;
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
}