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
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    [ODataEnableQuery(OutputType.Paged)]
    [ProducesResponseType<PagedResponseDto<OrderSummaryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public IQueryable<OrderSummaryResponseDto> GetMyOrders() => orderService.GetMyOrdersQueryable(User);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderDetail(Guid id) =>
        Ok(await orderService.GetOrderDetailAsync(User, id));

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType<OrderDetailResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelOrder(Guid id) => Ok(await orderService.CancelOrderAsync(User, id));
}