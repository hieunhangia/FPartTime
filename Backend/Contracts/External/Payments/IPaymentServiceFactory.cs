using Domain.Shared.Enums;

namespace Contracts.External.Payments;

public interface IPaymentServiceFactory
{
    IPaymentService Get(PaymentProvider provider);
}
