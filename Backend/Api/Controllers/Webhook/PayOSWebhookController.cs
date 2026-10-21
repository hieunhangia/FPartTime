using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Contracts.Application;
using Microsoft.AspNetCore.Mvc;
using PayOS;
using PayOS.Exceptions;

namespace Api.Controllers.Webhook;

[ApiController]
[Route("api/[controller]")]
public class PayOSWebhookController(
    IIpnDispatcher ipnDispatcher,
    IConfiguration configuration,
    ILogger<PayOSWebhookController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessWebhook([FromBody] PayOS.Models.Webhooks.Webhook request)
    {
        var rawKey = configuration["PayOS:ChecksumKey"] ?? "";
        var checksumKey = rawKey.Trim();

        logger.LogWarning(
            "==================== [PAYOS WEBHOOK NHẬN ĐƯỢC] ====================\n" +
            "Key length: {Length} | Preview: {KeyStart}...{KeyEnd}{Warning}\n" +
            "OrderCode: {OrderCode} | Amount: {Amount}\n" +
            "PayOS Signature: {Signature}\n" +
            "Payload JSON: {PayloadJson}",
            checksumKey.Length,
            checksumKey.Length >= 8 ? $"{checksumKey[..4]}" : "N/A",
            checksumKey.Length >= 8 ? $"{checksumKey[^4..]}" : "N/A",
            rawKey != checksumKey ? " [CẢNH BÁO: KEY BỊ DÍNH KHOẢNG TRẮNG ĐÃ ĐƯỢC TRIM!]" : "",
            request.Data?.OrderCode,
            request.Data?.Amount,
            request.Signature,
            JsonSerializer.Serialize(request));

        if (request.Data == null)
        {
            logger.LogError("[PayOS Webhook] Request Data bị null!");
            return BadRequest(new { error = -1, message = "Request Data is null" });
        }

        // 1. Tính toán chữ ký thủ công theo chuẩn PayOS để đối chiếu
        var (queryString, calculatedSig) = CalculatePayOSSignature(request.Data, checksumKey);
        var isManualMatch = string.Equals(calculatedSig, request.Signature, StringComparison.OrdinalIgnoreCase);

        logger.LogWarning(
            "==================== [KẾT QUẢ ĐỐI SOÁT CHỮ KÝ] ====================\n" +
            "- Chữ ký từ PayOS      : {PayOSSig}\n" +
            "- Chữ ký tính toán     : {CalcSig}\n" +
            "- Kết quả so sánh      : {MatchResult}\n" +
            "- Chuỗi dữ liệu ký     : {QueryString}",
            request.Signature,
            calculatedSig,
            isManualMatch ? "✅ KHỚP HOÀN TOÀN" : "❌ KHÔNG KHỚP",
            queryString);

        // 2. Thử xác thực qua SDK
        bool sdkSuccess = false;
        try
        {
            var client = new PayOSClient(new PayOSOptions
            {
                ClientId = configuration["PayOS:ClientId"],
                ApiKey = configuration["PayOS:ApiKey"],
                ChecksumKey = checksumKey
            });
            var verifiedData = await client.Webhooks.VerifyAsync(request);
            logger.LogWarning("[PayOS Webhook] SDK VerifyAsync: ✅ THÀNH CÔNG cho OrderCode={OrderCode}", verifiedData.OrderCode);
            sdkSuccess = true;
        }
        catch (WebhookException we)
        {
            logger.LogError(we, "[PayOS Webhook] SDK VerifyAsync: ❌ THẤT BẠI ({Message})", we.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PayOS Webhook] SDK VerifyAsync gặp lỗi ngoại lệ: {Message}", ex.Message);
        }

        // 3. Nếu chữ ký không khớp ở cả 2 cách, từ chối và in cảnh báo
        if (!isManualMatch && !sdkSuccess)
        {
            logger.LogError(
                "[PayOS Webhook] Từ chối Webhook vì chữ ký không hợp lệ! " +
                "Vui lòng kiểm tra: 1) ChecksumKey đúng kênh thanh toán chưa? 2) Có phải request test tự bắn không?");
            return BadRequest(new
            {
                error = -1,
                message = "Data not integrity",
                receivedSignature = request.Signature,
                calculatedSignature = calculatedSig,
                canonicalString = queryString
            });
        }

        // 4. Chữ ký hợp lệ -> Dispatch IPN để xử lý logic đơn hàng
        var paymentRefId = request.Data.OrderCode.ToString();
        logger.LogWarning("[PayOS Webhook] Bắt đầu Dispatch IPN cho PaymentRefId={RefId}", paymentRefId);
        await ipnDispatcher.DispatchAsync(paymentRefId);
        logger.LogWarning("[PayOS Webhook] Dispatch IPN THÀNH CÔNG cho PaymentRefId={RefId}", paymentRefId);

        return Ok(new { error = 0, message = "success" });
    }

    private static (string QueryString, string Signature) CalculatePayOSSignature(
        PayOS.Models.Webhooks.WebhookData data, string checksumKey)
    {
        var dict = new SortedDictionary<string, string?>(StringComparer.Ordinal)
        {
            ["accountNumber"] = data.AccountNumber,
            ["amount"] = data.Amount.ToString(),
            ["code"] = data.Code,
            ["counterAccountBankId"] = data.CounterAccountBankId,
            ["counterAccountBankName"] = data.CounterAccountBankName,
            ["counterAccountName"] = data.CounterAccountName,
            ["counterAccountNumber"] = data.CounterAccountNumber,
            ["currency"] = data.Currency,
            ["desc"] = data.Description2,
            ["description"] = data.Description,
            ["orderCode"] = data.OrderCode.ToString(),
            ["paymentLinkId"] = data.PaymentLinkId,
            ["reference"] = data.Reference,
            ["transactionDateTime"] = data.TransactionDateTime,
            ["virtualAccountName"] = data.VirtualAccountName,
            ["virtualAccountNumber"] = data.VirtualAccountNumber
        };

        var queryString = string.Join("&", dict
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{kv.Key}={kv.Value}"));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(checksumKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        return (queryString, Convert.ToHexStringLower(hashBytes));
    }
}