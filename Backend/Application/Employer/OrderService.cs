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

public class OrderService(ApplicationDbContext context) : IOrderService
{
    public IQueryable<OrderSummaryResponseDto> GetMyOrdersQueryable(ClaimsPrincipal user) =>
        context.Orders
            .Where(o => o.UserId == user.GetUserId())
            .ProjectToSummaryDto();

    public async Task<OrderDetailResponseDto> GetOrderDetailAsync(ClaimsPrincipal user, Guid orderId) =>
        await context.Orders
            .Include(o => o.PaymentTransactions)
            .Where(o => o.Id == orderId && o.UserId == user.GetUserId())
            .ProjectToDetailDto()
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Không tìm thấy đơn hàng.");
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class OrderMapper
{
    private static partial OrderSummaryResponseDto MapToSummaryDto(OrderBase order);
    public static partial IQueryable<OrderSummaryResponseDto> ProjectToSummaryDto(this IQueryable<OrderBase> orders);

    [MapProperty(nameof(PaymentTransaction), nameof(OrderDetailResponseDto.PaymentTransactionResponseDto.PaymentUrl),
        Use = nameof(MapToPaymentUrl))]
    private static partial OrderDetailResponseDto.PaymentTransactionResponseDto MapToPaymentTransactionDto(
        PaymentTransaction transaction);

    private static partial OrderDetailResponseDto MapToDetailDto(OrderBase order);
    public static partial IQueryable<OrderDetailResponseDto> ProjectToDetailDto(this IQueryable<OrderBase> orders);

    private static string? MapToPaymentUrl(PaymentTransaction transaction) =>
        transaction.Status != PaymentTransactionStatus.Pending ? null : transaction.PaymentUrl;
}