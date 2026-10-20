using System.Text.Json.Serialization;
using Domain.Shared.Enums;

namespace Contracts.External.Payment;

public interface IPaymentService
{
    PaymentProvider Provider { get; }
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request);
    Task CancelPaymentAsync(string paymentReferenceId);
    Task<IpnResponse> ProcessIpnAsync(IpnRequest request);
}

public abstract class CreatePaymentRequest
{
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required string CancelUrl { get; set; }
    public required string ReturnUrl { get; set; }
    public DateTime? ExpiredAt { get; set; }
}

public abstract class CreatePaymentResponse
{
    public required string PaymentReferenceId { get; set; }
    public required string PaymentUrl { get; set; }
}

public class PayOSCreatePaymentRequest : CreatePaymentRequest
{
    public required long OrderCode { get; set; }
    public required List<PayOSPaymentItem> Items { get; set; }
    public string? BuyerPhone { get; set; }
}

public class PayOSPaymentItem
{
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public required int Quantity { get; set; }
}

public class PayOSCreatePaymentResponse : CreatePaymentResponse;

public abstract class IpnRequest;

public abstract class IpnResponse
{
    public string PaymentReferenceId { get; set; } = string.Empty;
}

public class PayOSIpnRequest : IpnRequest
{
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("desc")] public string Description { get; set; } = string.Empty;

    public bool Success { get; set; }

    public PayOsIpnResponse Data { get; set; } = new();

    public string Signature { get; set; } = string.Empty;
}

public class PayOsIpnResponse : IpnResponse
{
    public long OrderCode { get; set; }

    public long Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public string TransactionDateTime { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public string PaymentLinkId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("desc")] public string Description2 { get; set; } = string.Empty;

    public string? CounterAccountBankId { get; set; }

    public string? CounterAccountBankName { get; set; }

    public string? CounterAccountName { get; set; }

    public string? CounterAccountNumber { get; set; }

    public string? VirtualAccountName { get; set; }

    public string? VirtualAccountNumber { get; set; }
}