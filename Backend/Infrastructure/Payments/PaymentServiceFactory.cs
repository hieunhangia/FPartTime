using Contracts.External.Payment;
using Domain.Shared.Enums;
using Domain.Shared.Exceptions.HttpErrorExceptions;

namespace Infrastructure.Payments;

public class PaymentServiceFactory : IPaymentServiceFactory
{
    private readonly Dictionary<PaymentProvider, IPaymentService> _services = [];

    public PaymentServiceFactory(IEnumerable<IPaymentService> paymentServices)
    {
        foreach (var service in paymentServices)
        {
            _services[service.Provider] = service;
        }
    }

    public IPaymentService Get(PaymentProvider provider) =>
        _services.GetValueOrDefault(provider) ??
        throw new BadRequestException($"Phương thức thanh toán '{provider}' hiện chưa được hỗ trợ.");
}