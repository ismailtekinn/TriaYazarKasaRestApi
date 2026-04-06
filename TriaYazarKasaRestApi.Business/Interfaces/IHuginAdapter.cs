using System.Threading.Tasks;
using TriaYazarKasaRestApi.Business.Models;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.Interfaces
{
    public interface IHuginAdapter
    {
        bool IsConnected { get; }

        Task<PosOperationResult> ConnectAsync(AdapterConnectionInfo connectionInfo);
        Task<PosOperationResult> DisconnectAsync();
        Task<PosOperationResult> GetStatusAsync();
        Task<PosOperationResult> GetDeviceInfoAsync();
        Task<PosOperationResult> GetXReportAsync();
        Task<PosOperationResult> GetZReportAsync();
        Task<PosOperationResult> SendJsonDocumentAsync(HuginJsonDocumentRequestDto request);

        Task<PosOperationResult> AddCashPaymentAsync(HuginCashPaymentRequestDto request);
        Task<PosOperationResult> AddCardPaymentAsync(HuginCardPaymentRequestDto request);
        Task<PosOperationResult> CloseReceiptAsync();
    }
}