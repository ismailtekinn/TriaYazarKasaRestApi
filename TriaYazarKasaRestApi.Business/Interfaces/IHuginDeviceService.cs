using System;
using System.Threading.Tasks;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.Interfaces
{
    public interface IHuginDeviceService
    {
        Task<HuginConnectionResponseDto> ConnectAsync(HuginConnectRequestDto request);
        Task<HuginOperationResponseDto> DisconnectAsync(Guid connectionId);
        Task<HuginOperationResponseDto> GetStatusAsync(Guid connectionId);
        Task<HuginOperationResponseDto> GetDeviceInfoAsync(Guid connectionId);
        Task<HuginOperationResponseDto> GetXReportAsync(Guid connectionId);
        Task<HuginOperationResponseDto> GetZReportAsync(Guid connectionId);
        Task<HuginOperationResponseDto> SendJsonDocumentAsync(Guid connectionId, HuginJsonDocumentRequestDto request);

        Task<HuginOperationResponseDto> AddCashPaymentAsync(Guid connectionId, HuginCashPaymentRequestDto request);
        Task<HuginOperationResponseDto> AddCardPaymentAsync(Guid connectionId, HuginCardPaymentRequestDto request);
        Task<HuginOperationResponseDto> CloseReceiptAsync(Guid connectionId);
    }
}