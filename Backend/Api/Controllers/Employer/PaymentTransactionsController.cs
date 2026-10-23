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
public class PaymentTransactionsController(IPaymentTransactionService paymentTransactionService) : ControllerBase
{
    [HttpGet]
    [ODataEnableQuery(OutputType.Paged)]
    [ProducesResponseType<PagedResponseDto<PaymentTransactionResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public IQueryable<PaymentTransactionResponseDto> GetMyTransactions() =>
        paymentTransactionService.GetMyPaymentTransactionQueryable(User);

    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType<PaymentTransactionResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelTransaction(Guid id) =>
        Ok(await paymentTransactionService.CancelPaymentTransactionAsync(User, id));
}