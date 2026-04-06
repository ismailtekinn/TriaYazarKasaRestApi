using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;


namespace TriaYazarKasaRestApiWebApi.Controllers
{
    [ApiController]
    [Route("api/hugin")]
    public class HuginController : ControllerBase
    {
        private readonly IHuginDeviceService _huginDeviceService;

        public HuginController(IHuginDeviceService huginDeviceService)
        {
            _huginDeviceService = huginDeviceService;
        }

        [HttpPost("connect")]
        public async Task<ActionResult<ApiResponseDto<HuginConnectionResponseDto>>> Connect([FromBody] HuginConnectRequestDto request)
        {
            var result = await _huginDeviceService.ConnectAsync(request);
            return Ok(ApiResponseDto<HuginConnectionResponseDto>.Ok(result, result.Message));
        }

        [HttpDelete("{connectionId}/disconnect")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> Disconnect(Guid connectionId)
        {
            var result = await _huginDeviceService.DisconnectAsync(connectionId);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }

        [HttpGet("{connectionId}/status")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> Status(Guid connectionId)
        {
            var result = await _huginDeviceService.GetStatusAsync(connectionId);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }

        [HttpGet("{connectionId}/device-info")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> DeviceInfo(Guid connectionId)
        {
            var result = await _huginDeviceService.GetDeviceInfoAsync(connectionId);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }

        [HttpPost("{connectionId}/x-report")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> XReport(Guid connectionId)
        {
            var result = await _huginDeviceService.GetXReportAsync(connectionId);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }

        [HttpPost("{connectionId}/z-report")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> ZReport(Guid connectionId)
        {
            var result = await _huginDeviceService.GetZReportAsync(connectionId);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }

        [HttpPost("{connectionId}/json-document")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> SendJsonDocument(
            Guid connectionId,
            [FromBody] HuginJsonDocumentRequestDto request)
        {
            var result = await _huginDeviceService.SendJsonDocumentAsync(connectionId, request);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }

        [HttpPost("{connectionId}/payment/cash")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> AddCashPayment(
            Guid connectionId,
            [FromBody] HuginCashPaymentRequestDto request)
        {
            var result = await _huginDeviceService.AddCashPaymentAsync(connectionId, request);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }
        [HttpPost("{connectionId}/payment/card")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> AddCardPayment(
            Guid connectionId,
            [FromBody] HuginCardPaymentRequestDto request)
        {
            var result = await _huginDeviceService.AddCardPaymentAsync(connectionId, request);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }

        [HttpPost("{connectionId}/close-receipt")]
        public async Task<ActionResult<ApiResponseDto<HuginOperationResponseDto>>> CloseReceipt(Guid connectionId)
        {
            var result = await _huginDeviceService.CloseReceiptAsync(connectionId);
            return Ok(ApiResponseDto<HuginOperationResponseDto>.Ok(result, result.Message));
        }

    }
}