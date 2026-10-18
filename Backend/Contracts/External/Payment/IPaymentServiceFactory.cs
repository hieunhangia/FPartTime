using Domain.Shared.Enums;

namespace Contracts.External.Payment;

public interface IPaymentServiceFactory
{
    IPaymentService Get(PaymentProvider provider);
}
