using System.ComponentModel.DataAnnotations;

namespace Contracts.External;

public interface IVNPAYPaymentService
{
    Task<CreateVNPAYPaymentResponse> CreatePaymentAsync(CreateVNPAYPaymentRequest request);
    Task<VNPAYIpnProcessingResult> ProcessIpnAsync(VNPAYIpnRequestDto request);
}

public class CreateVNPAYPaymentRequest
{
    public required decimal Amount { get; set; }
    public required string Description { get; set; }
    public required string ReturnUrl { get; set; }
    public required string IpAddress { get; set; }
    public required DateTime ExpiredAt { get; set; }
    public string? OrderType { get; set; }
}

public class CreateVNPAYPaymentResponse
{
    public required string PaymentReferenceId { get; set; }
    public required string PaymentUrl { get; set; }
}

public class VNPAYIpnRequestDto
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

public class VNPAYIpnProcessingResult
{
    public required string PaymentReferenceId { get; set; }
    public required bool IsValidSignature { get; set; }
    public required bool IsPaymentSuccess { get; set; }
}

public class VNPAYIpnResponseDto
{
    public required string RspCode { get; set; }
    public required string Message { get; set; }
}