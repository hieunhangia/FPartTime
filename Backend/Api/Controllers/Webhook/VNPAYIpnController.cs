using Contracts.Application;
using Contracts.External;
using Domain;
using Domain.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers.Webhook;

[ApiController]
[Route("api/[controller]")]
public class VNPAYIpnController(
    IVNPAYPaymentService vnpayPaymentService,
    IIpnDispatcher ipnDispatcher,
    ApplicationDbContext context,
    ILogger<VNPAYIpnController> logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ProcessIpnGet([FromQuery] VNPAYIpnRequestDto request) =>
        await HandleIpnAsync(request);

    [HttpPost]
    public async Task<IActionResult> ProcessIpnPost([FromQuery] VNPAYIpnRequestDto request) =>
        await HandleIpnAsync(request);

    private async Task<IActionResult> HandleIpnAsync(VNPAYIpnRequestDto request)
    {
        try
        {
            if (request.QueryParameters == null || request.QueryParameters.Count == 0)
            {
                if (Request.Query.Count > 0)
                {
                    request.QueryParameters = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
                }
                else if (Request.HasFormContentType && Request.Form.Count > 0)
                {
                    request.QueryParameters = Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString());
                }
            }

            var processResult = await vnpayPaymentService.ProcessIpnAsync(request);
            if (processResult is { IsSuccess: false, RspCode: "97" })
            {
                logger.LogWarning("VNPAY IPN: Chữ ký không hợp lệ.");
                return Ok(new VNPAYIpnResponseDto
                {
                    RspCode = "97",
                    Message = "Invalid Checksum"
                });
            }

            var paymentReferenceId = !string.IsNullOrWhiteSpace(processResult.PaymentReferenceId)
                ? processResult.PaymentReferenceId
                : request.vnp_TxnRef ?? string.Empty;

            var transaction = await context.PaymentTransactions
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.PaymentReferenceId == paymentReferenceId);

            if (transaction == null)
            {
                logger.LogWarning("VNPAY IPN: Không tìm thấy giao dịch với TxnRef {TxnRef}", paymentReferenceId);
                return Ok(new VNPAYIpnResponseDto
                {
                    RspCode = "01",
                    Message = "Order not found"
                });
            }

            // Kiểm tra số tiền thanh toán (vnp_Amount = transaction.Amount * 100)
            if (request.vnp_Amount.HasValue)
            {
                var expectedAmount = decimal.ToInt64(transaction.Amount * 100);
                if (request.vnp_Amount.Value != expectedAmount)
                {
                    logger.LogWarning(
                        "VNPAY IPN: Số tiền không khớp cho giao dịch {TxnRef}. Thực nhận: {Actual}, Kỳ vọng: {Expected}",
                        paymentReferenceId, request.vnp_Amount.Value, expectedAmount);
                    return Ok(new VNPAYIpnResponseDto
                    {
                        RspCode = "04",
                        Message = "Invalid amount"
                    });
                }
            }

            // Kiểm tra trạng thái đơn hàng đã được xử lý trước đó hay chưa
            if (transaction.Status != PaymentTransactionStatus.Pending)
            {
                logger.LogInformation("VNPAY IPN: Giao dịch {TxnRef} đã được xử lý trước đó với trạng thái {Status}",
                    paymentReferenceId, transaction.Status);
                return Ok(new VNPAYIpnResponseDto
                {
                    RspCode = "02",
                    Message = "Order already confirmed"
                });
            }

            if (processResult.IsSuccess)
            {
                logger.LogInformation("VNPAY IPN: Giao dịch {TxnRef} thanh toán thành công.", paymentReferenceId);
                await ipnDispatcher.DispatchAsync(paymentReferenceId);
            }
            else
            {
                logger.LogWarning(
                    "VNPAY IPN: Giao dịch {TxnRef} thất bại/bị hủy. ResponseCode: {ResponseCode}, Status: {Status}",
                    paymentReferenceId, processResult.ResponseCode, processResult.TransactionStatus);

                transaction.Status = processResult.ResponseCode == "24"
                    ? PaymentTransactionStatus.Cancelled
                    : PaymentTransactionStatus.Failed;
                await context.SaveChangesAsync();
            }

            return Ok(new VNPAYIpnResponseDto
            {
                RspCode = "00",
                Message = "Confirm Success"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VNPAY IPN: Lỗi không xác định khi xử lý IPN");
            return Ok(new VNPAYIpnResponseDto
            {
                RspCode = "99",
                Message = "Unknown error"
            });
        }
    }
}