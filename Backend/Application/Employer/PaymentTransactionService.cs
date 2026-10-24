using System.Security.Claims;
using Contracts.Application.Employer;
using Domain;
using Domain.Models.Orders;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Domain.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Application.Employer;

public class PaymentTransactionService(ApplicationDbContext context) : IPaymentTransactionService
{
    public IQueryable<PaymentTransactionResponseDto> GetMyPaymentTransactionQueryable(ClaimsPrincipal user) =>
        context.PaymentTransactions
            .Where(x => x.UserId == user.GetUserId())
            .ProjectToDto();

    public async Task<PaymentTransactionResponseDto> CancelPaymentTransactionAsync(ClaimsPrincipal user,
        Guid transactionId)
    {
        var transaction = await context.PaymentTransactions
                              .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserId == user.GetUserId())
                          ?? throw new NotFoundException("Không tìm thấy giao dịch thanh toán.");

        if (transaction.Status != PaymentTransactionStatus.Pending)
        {
            throw new BadRequestException("Chỉ có thể hủy các giao dịch thanh toán đang chờ.");
        }

        transaction.Status = PaymentTransactionStatus.Cancelled;
        await context.SaveChangesAsync();
        return transaction.MapToDto();
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class PaymentTransactionMapper
{
    [MapProperty(nameof(PaymentTransaction), nameof(PaymentTransactionResponseDto.PaymentUrl),
        Use = nameof(MapToPaymentUrl))]
    public static partial PaymentTransactionResponseDto MapToDto(this PaymentTransaction paymentTransaction);

    public static partial IQueryable<PaymentTransactionResponseDto> ProjectToDto(
        this IQueryable<PaymentTransaction> paymentTransactions);

    private static string? MapToPaymentUrl(PaymentTransaction transaction) =>
        transaction.Status != PaymentTransactionStatus.Pending ? null : transaction.PaymentUrl;
}