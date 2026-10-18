namespace Contracts.External;

public interface IPaymentService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request);
}

public class CreatePaymentRequest
{
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required string CancelUrl { get; set; }
    public required string ReturnUrl { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public IPaymentRequestAdditionalData? AdditionalData { get; set; }
}

public class CreatePaymentResponse
{
    public required string PaymentUrl { get; set; }
}

public interface IPaymentRequestAdditionalData;

public class PayOSRequestAdditionalData : IPaymentRequestAdditionalData
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