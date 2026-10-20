using Api.Attributes;
using Contracts.Application.Employer;
using Contracts.Common;
using Domain.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Employer;

[ApiController]
[Route("api/employer/[controller]")]
[Authorize(Roles = Role.Employer)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public class IndustriesController(IIndustryService industryService) : ControllerBase
{
    [HttpGet]
    [ODataEnableQuery(OutputType.Paged)]
    [ProducesResponseType<PagedResponseDto<IndustryDetailResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IQueryable<IndustryDetailResponseDto> GetIndustries() => industryService.GetIndustriesQueryable();

    [HttpGet("{id:guid}")]
    [ProducesResponseType<IndustryDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIndustryDetail(Guid id, CancellationToken cancellationToken) =>
        Ok(await industryService.GetIndustryDetailAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<IndustryDetailResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateIndustry([FromBody] CreateUpdateIndustryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await industryService.CreateIndustryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetIndustryDetail), new { result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<IndustryDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateIndustry(Guid id, [FromBody] CreateUpdateIndustryRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await industryService.UpdateIndustryAsync(id, request, cancellationToken));

}
