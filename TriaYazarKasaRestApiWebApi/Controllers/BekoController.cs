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

        [HttpDelete("{connectionIdB}/disconnect")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> Disconnect(Guid connectionIdB)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.DisconnectAsync(connectionIdB)));

        [HttpGet("{connectionIdB}/status")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> Status(Guid connectionIdB)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.GetStatusAsync(connectionIdB)));

        [HttpGet("{connectionIdB}/device-info")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> DeviceInfo(Guid connectionIdB)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.GetDeviceInfoAsync(connectionIdB)));

        [HttpPost("{connectionIdB}/basket")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> SendBasket(Guid connectionIdB, [FromBody] BekoBasketRequestDto request)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.SendBasketAsync(connectionIdB, request)));

        [HttpPost("{connectionIdB}/payment")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> SendPayment(Guid connectionIdB, [FromBody] BekoPaymentRequestDto request)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.SendPaymentAsync(connectionIdB, request)));

        [HttpPost("{connectionIdB}/void")]
        public async Task<ActionResult<ApiResponseDto<BekoOperationResponseDto>>> VoidReceipt(Guid connectionIdB)
            => Ok(ApiResponseDto<BekoOperationResponseDto>.Ok(await _bekoDeviceService.VoidReceiptAsync(connectionIdB)));
    }
}
