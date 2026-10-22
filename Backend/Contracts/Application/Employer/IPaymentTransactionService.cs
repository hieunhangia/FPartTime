using System.Security.Claims;
using Domain.Shared.Enums;

namespace Contracts.Application.Employer;

public interface IPaymentTransactionService
{
    IQueryable<PaymentTransactionResponseDto> GetMyPaymentTransactionQueryable(ClaimsPrincipal user);
}

public class PaymentTransactionResponseDto
{
    public required Guid Id { get; set; }
    public required PaymentProvider Provider { get; set; }
    public required string PaymentReferenceId { get; set; }
    public required decimal Amount { get; set; }
    public required PaymentTransactionStatus Status { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime ExpiredAt { get; set; }
    public required DateTime? PaidAt { get; set; }
    public string? PaymentUrl { get; set; }
}