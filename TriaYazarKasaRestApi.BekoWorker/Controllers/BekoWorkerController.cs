using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.BekoWorker.Controllers
{
    [ApiController]
    [Route("api/beko")]
    public class BekoWorkerController : ControllerBase
    {
        private readonly IBekoDeviceService _bekoDeviceService;

        public BekoWorkerController(IBekoDeviceService bekoDeviceService)
        {
            _bekoDeviceService = bekoDeviceService;
        }

        [HttpPost("connect")]
        public Task<BekoConnectionResponseDto> Connect([FromBody] BekoConnectRequestDto request)
            => _bekoDeviceService.ConnectAsync(request);

        [HttpDelete("{connectionId}/disconnect")]
        public Task<BekoOperationResponseDto> Disconnect(Guid connectionId)
            => _bekoDeviceService.DisconnectAsync(connectionId);

        [HttpGet("{connectionId}/status")]
        public Task<BekoOperationResponseDto> Status(Guid connectionId)
            => _bekoDeviceService.GetStatusAsync(connectionId);

        [HttpGet("{connectionId}/device-info")]
        public Task<BekoOperationResponseDto> DeviceInfo(Guid connectionId)
            => _bekoDeviceService.GetDeviceInfoAsync(connectionId);

        [HttpPost("{connectionId}/basket")]
        public Task<BekoOperationResponseDto> SendBasket(Guid connectionId, [FromBody] BekoBasketRequestDto request)
            => _bekoDeviceService.SendBasketAsync(connectionId, request);

        [HttpPost("{connectionId}/basket2")]
        public Task<BekoOperationResponseDto> SendBasket2(Guid connectionId, [FromBody] BekoBasketRequestDto request)
            => _bekoDeviceService.SendBasketAsync2(connectionId, request);

        [HttpGet("{connectionId}/basket2/{basketId}")]
        public Task<BekoOperationResponseDto> GetBasketStatus2(Guid connectionId, string basketId)
            => _bekoDeviceService.GetBasketOperationStatusAsync(connectionId, basketId);

        [HttpPost("{connectionId}/payment")]
        public Task<BekoOperationResponseDto> SendPayment(Guid connectionId, [FromBody] BekoPaymentRequestDto request)
            => _bekoDeviceService.SendPaymentAsync(connectionId, request);

        [HttpPost("{connectionId}/void")]
        public Task<BekoOperationResponseDto> VoidReceipt(Guid connectionId)
            => _bekoDeviceService.VoidReceiptAsync(connectionId);

        [HttpGet("debug/usb-devices")]
        public async Task<IActionResult> ListUsbDevices()
        {
            var command = "Get-CimInstance Win32_PnPEntity | Where-Object { $_.Name -like '*Beko*' -or $_.Name -like '*Yazar*' -or $_.PNPDeviceID -like 'USB*' } | Select-Object Name, PNPDeviceID, Status | ConvertTo-Json -Depth 3";
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "USB cihazlari listelenemedi.",
                    error
                });
            }

            return Content(string.IsNullOrWhiteSpace(output) ? "[]" : output, "application/json");
        }
    }
}
