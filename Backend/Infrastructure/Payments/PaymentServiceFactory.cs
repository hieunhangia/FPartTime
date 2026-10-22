using Contracts.External.Payments;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;

namespace Infrastructure.Payments;

public class PaymentServiceFactory(IEnumerable<IPaymentService> paymentServices) : IPaymentServiceFactory
{
    public IPaymentService Get(PaymentProvider provider) =>
        paymentServices.FirstOrDefault(s => s.Provider == provider)
        ?? throw new BadRequestException($"Cổng thanh toán {provider} chưa được hỗ trợ.");
}
