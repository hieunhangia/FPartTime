namespace Contracts;

public interface IPaymentService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request);
}

public class CreatePaymentRequest
{
    public required Guid OrderId { get; set; }
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required string CancelUrl { get; set; }
    public required string ReturnUrl { get; set; }
    public required List<PaymentItem> Items { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public IDictionary<string, object>? AdditionalData { get; set; }
}

public class PaymentItem
{
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public required int Quantity { get; set; }
}

public class CreatePaymentResponse
{
    public required string PaymentUrl { get; set; }
    public IDictionary<string, object>? AdditionalData { get; set; }
}