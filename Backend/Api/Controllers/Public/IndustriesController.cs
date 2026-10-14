using Microsoft.AspNetCore.Mvc;
using Service.ApplicationServices.Public;

namespace Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class IndustriesController(IndustryService industryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<IndustryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllIndustries() => Ok(await industryService.GetAllIndustriesAsync());
}