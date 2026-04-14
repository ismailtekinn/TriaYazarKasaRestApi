using TriaYazarKasaRestApi.Business.Models;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.Interfaces
{

    public interface IBekoAdapter
    {
        bool IsConnected { get; }
        Task<PosOperationResult> ConnectAsync();
        Task<PosOperationResult> DisconnectAsync();
        Task<PosOperationResult> GetStatusAsync();
        Task<PosOperationResult> GetDeviceInfoAsync();
        Task<PosOperationResult> SendBasketAsync(BekoBasketRequestDto request);
        Task<PosOperationResult> SendBasketAsync2(BekoBasketRequestDto request);
        Task<PosOperationResult> GetBasketOperationStatusAsync(string basketId);
        Task<PosOperationResult> SendPaymentAsync(BekoPaymentRequestDto request);
        Task<PosOperationResult> VoidReceiptAsync();
    }
}
