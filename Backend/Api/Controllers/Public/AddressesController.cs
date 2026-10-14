using Microsoft.AspNetCore.Mvc;
using Service.ApplicationServices.Public;

namespace Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class AddressesController(AddressService addressService) : ControllerBase
{
    [HttpGet]
    [Route("provinces")]
    [ProducesResponseType(typeof(List<ProvinceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProvinces() => Ok(await addressService.GetProvincesAsync());

    [HttpGet]
    [Route("provinces/{code}/communes")]
    [ProducesResponseType(typeof(List<CommuneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCommunesByProvinceCode(string code) =>
        Ok(await addressService.GetCommunesByProvinceCodeAsync(code));
}