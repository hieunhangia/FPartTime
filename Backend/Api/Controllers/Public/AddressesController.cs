using Contracts.Application.Public;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class AddressesController(IAddressService addressService) : ControllerBase
{
    [HttpGet]
    [Route("provinces")]
    [ProducesResponseType<List<ProvinceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProvinces() => Ok(await addressService.GetProvincesAsync());

    [HttpGet]
    [Route("provinces/{code}/communes")]
    [ProducesResponseType<List<CommuneDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCommunesByProvinceCode(string code) =>
        Ok(await addressService.GetCommunesByProvinceCodeAsync(code));
}