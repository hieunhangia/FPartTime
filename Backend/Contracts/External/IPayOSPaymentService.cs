using System.ComponentModel.DataAnnotations;

namespace Contracts.External;

public interface IPayOSPaymentService
{
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request);
    Task CancelPaymentAsync(string paymentReferenceId);
    Task<WebhookProcessingResult> ProcessWebhookAsync(WebhookRequestDto request);
}

public class CreatePaymentRequest
{
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required string CancelUrl { get; set; }
    public required string ReturnUrl { get; set; }
    public required DateTime ExpiredAt { get; set; }
    public required List<PaymentItem> Items { get; set; }

    public class PaymentItem
    {
        public required string Name { get; set; }
        public required decimal Price { get; set; }
        public required int Quantity { get; set; }
    }
}

public class CreatePaymentResponse
{
    public required string PaymentReferenceId { get; set; }
    public required string PaymentUrl { get; set; }
}

public class WebhookRequestDto
{
    [Required] public string Code { get; set; } = string.Empty;
    [Required] public string Desc { get; set; } = string.Empty;
    [Required] public bool Success { get; set; }
    [Required] public RequestData Data { get; set; } = new();
    [Required] public string Signature { get; set; } = string.Empty;

    public class RequestData
    {
        [Required] public long OrderCode { get; set; }
        [Required] public long Amount { get; set; }
        [Required] public string Description { get; set; } = string.Empty;
        [Required] public string AccountNumber { get; set; } = string.Empty;
        [Required] public string Reference { get; set; } = string.Empty;
        [Required] public string TransactionDateTime { get; set; } = string.Empty;
        [Required] public string Currency { get; set; } = string.Empty;
        [Required] public string PaymentLinkId { get; set; } = string.Empty;
        [Required] public string Code { get; set; } = string.Empty;
        [Required] public string Desc { get; set; } = string.Empty;
        public string? CounterAccountBankId { get; set; }
        public string? CounterAccountBankName { get; set; }
        public string? CounterAccountName { get; set; }
        public string? CounterAccountNumber { get; set; }
        public string? VirtualAccountName { get; set; }
        public string? VirtualAccountNumber { get; set; }
    }
}

public class WebhookProcessingResult
{
    public required string PaymentReferenceId { get; set; }
}