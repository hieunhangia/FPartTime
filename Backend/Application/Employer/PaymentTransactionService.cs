using System.Security.Claims;
using Contracts.Application.Employer;
using Domain;
using Domain.Models.Orders;
using Domain.Shared.Enums;
using Domain.Shared.Extensions;
using Riok.Mapperly.Abstractions;

namespace Application.Employer;

public class PaymentTransactionService(ApplicationDbContext context) : IPaymentTransactionService
{
    public IQueryable<PaymentTransactionResponseDto> GetMyPaymentTransactionQueryable(ClaimsPrincipal user) =>
        context.PaymentTransactions
            .Where(x => x.UserId == user.GetUserId())
            .ProjectToDto();
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class PaymentTransactionMapper
{
    [MapProperty(nameof(PaymentTransaction), nameof(PaymentTransactionResponseDto.PaymentUrl),
        Use = nameof(MapToPaymentUrl))]
    private static partial PaymentTransactionResponseDto MapToDto(PaymentTransaction paymentTransaction);

    public static partial IQueryable<PaymentTransactionResponseDto> ProjectToDto(
        this IQueryable<PaymentTransaction> paymentTransactions);

    private static string? MapToPaymentUrl(PaymentTransaction transaction) =>
        transaction.Status != PaymentTransactionStatus.Pending ? null : transaction.PaymentUrl;
}