using Microsoft.AspNetCore.Mvc;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApiWebApi.Controllers
{
    [ApiController]
    [Route("api/beko")]
    public class BekoController : ControllerBase
    {
        private readonly IBekoDeviceService _bekoDeviceService;

        public BekoController(IBekoDeviceService bekoDeviceService)
        {
            _bekoDeviceService = bekoDeviceService;
        }

        [HttpPost("connect")]
        public async Task<ActionResult<ApiResponseDto<BekoConnectionResponseDto>>> Connect([FromBody] BekoConnectRequestDto request)
            => Ok(ApiResponseDto<BekoConnectionResponseDto>.Ok(await _bekoDeviceService.ConnectAsync(request)));

        [HttpDelete("{connectionId}/disconnect")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> Disconnect(Guid connectionId)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.DisconnectAsync(connectionId)));

        [HttpGet("{connectionId}/status")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> Status(Guid connectionId)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.GetStatusAsync(connectionId)));

        [HttpGet("{connectionId}/device-info")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> DeviceInfo(Guid connectionId)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.GetDeviceInfoAsync(connectionId)));

        [HttpPost("{connectionId}/basket")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> SendBasket(Guid connectionId, [FromBody] BekoBasketRequestDto request)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.SendBasketAsync(connectionId, request)));

        [HttpPost("{connectionId}/payment")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> SendPayment(Guid connectionId, [FromBody] BekoPaymentRequestDto request)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.SendPaymentAsync(connectionId, request)));

        [HttpPost("{connectionId}/void")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> VoidReceipt(Guid connectionId)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.VoidReceiptAsync(connectionId)));
    }
}
