using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Domain.Shared.Enums;

namespace Contracts.External.Payments;

public interface IPaymentService
{
    PaymentProvider Provider { get; }
    Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequestBase request);
    Task<ProcessIpnResponse> ProcessIpnAsync(ProcessIpnRequestBase request);
}

public abstract class CreatePaymentRequestBase
{
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required string ReturnUrl { get; set; }
    public required DateTime ExpiredAt { get; set; }
}

public class CreatePaymentResponse
{
    public required string PaymentReferenceId { get; set; }
    public required string PaymentUrl { get; set; }
}

public abstract class ProcessIpnRequestBase;

public class ProcessIpnResponse
{
    public string? PaymentReferenceId { get; set; }
    public required bool IsValidSignature { get; set; }
    public required bool IsPaymentSuccess { get; set; }
}

public class CreatePayOSPaymentRequest : CreatePaymentRequestBase
{
    public required string CancelUrl { get; set; }
    public required List<PaymentItem> Items { get; set; } = [];

    public class PaymentItem
    {
        public required string Name { get; set; }
        public required decimal Price { get; set; }
        public required int Quantity { get; set; }
    }
}

public class CreateVNPAYPaymentRequest : CreatePaymentRequestBase
{
    public required string IpAddress { get; set; }
    public string? OrderType { get; set; }
}

public class CreateZaloPayPaymentRequest : CreatePaymentRequestBase
{
    public required string AppUser { get; set; }
    public required List<PaymentItem> Items { get; set; }

    public class PaymentItem
    {
        public required string Name { get; set; }
        public required decimal Price { get; set; }
        public required int Quantity { get; set; }
    }
}

public class PayOSWebhookRequestDto : ProcessIpnRequestBase
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

public class VNPAYIpnRequestDto : ProcessIpnRequestBase
{
    [Required] public string vnp_TmnCode { get; set; } = string.Empty;
    [Required] public long vnp_Amount { get; set; }
    [Required] public string vnp_BankCode { get; set; } = string.Empty;
    public string? vnp_BankTranNo { get; set; }
    public string? vnp_CardType { get; set; }
    public string? vnp_PayDate { get; set; }
    [Required] public string vnp_OrderInfo { get; set; } = string.Empty;
    [Required] public string vnp_TransactionNo { get; set; } = string.Empty;
    [Required] public string vnp_ResponseCode { get; set; } = string.Empty;
    [Required] public string vnp_TransactionStatus { get; set; } = string.Empty;
    [Required] public string vnp_TxnRef { get; set; } = string.Empty;
    [Required] public string vnp_SecureHash { get; set; } = string.Empty;

    public Dictionary<string, string> ToParameterDictionary()
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Amount"] = vnp_Amount.ToString()
        };
        AddIfNotEmpty("vnp_TmnCode", vnp_TmnCode);
        AddIfNotEmpty("vnp_BankCode", vnp_BankCode);
        AddIfNotEmpty("vnp_BankTranNo", vnp_BankTranNo);
        AddIfNotEmpty("vnp_CardType", vnp_CardType);
        AddIfNotEmpty("vnp_PayDate", vnp_PayDate);
        AddIfNotEmpty("vnp_OrderInfo", vnp_OrderInfo);
        AddIfNotEmpty("vnp_TransactionNo", vnp_TransactionNo);
        AddIfNotEmpty("vnp_ResponseCode", vnp_ResponseCode);
        AddIfNotEmpty("vnp_TransactionStatus", vnp_TransactionStatus);
        AddIfNotEmpty("vnp_TxnRef", vnp_TxnRef);

        return dict;

        void AddIfNotEmpty(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value)) dict[key] = value;
        }
    }
}

public class VNPAYIpnResponseDto
{
    public required string RspCode { get; set; }
    public required string Message { get; set; }
}

public class ZaloPayCallbackRequestDto : ProcessIpnRequestBase
{
    [Required] public string Data { get; set; } = string.Empty;
    [Required] public string Mac { get; set; } = string.Empty;
    public int Type { get; set; }

    public class ZaloPayCallbackDataDto
    {
        [JsonPropertyName("app_id")] public int AppId { get; set; }
        [JsonPropertyName("app_trans_id")] public string AppTransId { get; set; } = string.Empty;
        [JsonPropertyName("app_time")] public long AppTime { get; set; }
        [JsonPropertyName("app_user")] public string AppUser { get; set; } = string.Empty;
        public long Amount { get; set; }
        [JsonPropertyName("embed_data")] public string EmbedData { get; set; } = string.Empty;
        public string Item { get; set; } = string.Empty;
        [JsonPropertyName("zp_trans_id")] public long ZpTransId { get; set; }
        [JsonPropertyName("server_time")] public long ServerTime { get; set; }
        public int Channel { get; set; }
        [JsonPropertyName("merchant_user_id")] public string? MerchantUserId { get; set; }
        [JsonPropertyName("user_fee_amount")] public long UserFeeAmount { get; set; }
        [JsonPropertyName("discount_amount")] public long DiscountAmount { get; set; }
    }
}

public class ZaloPayCallbackResponseDto
{
    [JsonPropertyName("return_code")] public required int ReturnCode { get; set; }
    [JsonPropertyName("return_message")] public required string ReturnMessage { get; set; }
}