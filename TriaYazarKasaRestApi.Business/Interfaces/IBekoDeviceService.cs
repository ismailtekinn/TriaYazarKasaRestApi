using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.Interfaces
{
    public interface IBekoDeviceService
    {
        Task<BekoConnectionResponseDto> ConnectAsync(BekoConnectRequestDto request);
        Task<BekoOperationResponseDto> DisconnectAsync(Guid connectionId);
        Task<BekoOperationResponseDto> GetStatusAsync(Guid connectionId);
        Task<BekoOperationResponseDto> GetDeviceInfoAsync(Guid connectionId);
        Task<BekoOperationResponseDto> SendBasketAsync(Guid connectionId, BekoBasketRequestDto request);
        Task<BekoOperationResponseDto> SendPaymentAsync(Guid connectionId, BekoPaymentRequestDto request);
        Task<BekoOperationResponseDto> VoidReceiptAsync(Guid connectionId);
    }
}
