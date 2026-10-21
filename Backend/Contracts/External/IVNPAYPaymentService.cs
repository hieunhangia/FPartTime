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
    public string? vnp_TmnCode { get; set; }
    public long? vnp_Amount { get; set; }
    public string? vnp_BankCode { get; set; }
    public string? vnp_BankTranNo { get; set; }
    public string? vnp_CardType { get; set; }
    public string? vnp_PayDate { get; set; }
    public string? vnp_OrderInfo { get; set; }
    public string? vnp_TransactionNo { get; set; }
    public string? vnp_ResponseCode { get; set; }
    public string? vnp_TransactionStatus { get; set; }
    public string? vnp_TxnRef { get; set; }
    public string? vnp_SecureHashType { get; set; }
    public string? vnp_SecureHash { get; set; }
    public IDictionary<string, string>? QueryParameters { get; set; }

    public Dictionary<string, string> ToParameterDictionary()
    {
        var dict = QueryParameters is { Count: > 0 }
            ? new Dictionary<string, string>(QueryParameters, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        AddIfNotEmpty("vnp_TmnCode", vnp_TmnCode);
        if (vnp_Amount.HasValue)
        {
            AddIfNotEmpty("vnp_Amount", vnp_Amount.Value.ToString());
        }

        AddIfNotEmpty("vnp_BankCode", vnp_BankCode);
        AddIfNotEmpty("vnp_BankTranNo", vnp_BankTranNo);
        AddIfNotEmpty("vnp_CardType", vnp_CardType);
        AddIfNotEmpty("vnp_PayDate", vnp_PayDate);
        AddIfNotEmpty("vnp_OrderInfo", vnp_OrderInfo);
        AddIfNotEmpty("vnp_TransactionNo", vnp_TransactionNo);
        AddIfNotEmpty("vnp_ResponseCode", vnp_ResponseCode);
        AddIfNotEmpty("vnp_TransactionStatus", vnp_TransactionStatus);
        AddIfNotEmpty("vnp_TxnRef", vnp_TxnRef);
        AddIfNotEmpty("vnp_SecureHashType", vnp_SecureHashType);
        AddIfNotEmpty("vnp_SecureHash", vnp_SecureHash);

        return dict;

        void AddIfNotEmpty(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                dict.TryAdd(key, value);
            }
        }
    }
}

public class VNPAYIpnProcessingResult
{
    public required string PaymentReferenceId { get; set; }
    public bool IsSuccess { get; set; }
    public string RspCode { get; set; } = "00";
    public string Message { get; set; } = "Confirm Success";
    public string? ResponseCode { get; set; }
    public string? TransactionStatus { get; set; }
    public string? TransactionNo { get; set; }
}

public class VNPAYIpnResponseDto
{
    public required string RspCode { get; set; }
    public required string Message { get; set; }
}