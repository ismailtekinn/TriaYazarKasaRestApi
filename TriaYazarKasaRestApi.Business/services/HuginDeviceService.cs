using TriaYazarKasaRestApi.Business.Adapters;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.Models;
using TriaYazarKasaRestApi.Entities.DTOs;
using TriaYazarKasaRestApi.Entities.Enums;

namespace TriaYazarKasaRestApi.Business.services
{
    public class HuginDeviceService : IHuginDeviceService
    {
        private readonly IHuginConnectionManager _connectionManager;

        public HuginDeviceService(IHuginConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public async Task<HuginConnectionResponseDto> ConnectAsync(HuginConnectRequestDto request)
        {
            var adapter = new HuginAdapter();

            var result = await adapter.ConnectAsync(new AdapterConnectionInfo
            {
                IpAddress = request.IpAddress,
                Port = request.Port,
                SerialPort = request.SerialPort,
                UseTcp = request.UseTcp,
                FiscalId = request.FiscalId
            });

            var connectionId = Guid.NewGuid();

            if (result.Success)
            {
                _connectionManager.Add(new HuginActiveConnection
                {
                    ConnectionId = connectionId,
                    Adapter = adapter,
                    ConnectedAt = DateTime.UtcNow
                });
            }

            return new HuginConnectionResponseDto
            {
                ConnectionId = connectionId,
                IsConnected = result.Success,
                Status = result.Success ? PosConnectionStatus.Connected : PosConnectionStatus.Error,
                Message = result.Message,
                ConnectedAt = DateTime.UtcNow
            };
        }

        public async Task<HuginOperationResponseDto> DisconnectAsync(Guid connectionId)
        {
            var connection = _connectionManager.Get(connectionId);
            if (connection == null)
            {
                return new HuginOperationResponseDto
                {
                    Success = false,
                    Message = "Baglanti bulunamadi"
                };
            }

            var result = await connection.Adapter.DisconnectAsync();
            _connectionManager.Remove(connectionId);

            return new HuginOperationResponseDto
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data,
                ErrorCode = result.ResultCode
            };
        }

        public Task<HuginOperationResponseDto> GetStatusAsync(Guid connectionId)
            => ExecuteAsync(connectionId, x => x.GetStatusAsync());

        public Task<HuginOperationResponseDto> GetDeviceInfoAsync(Guid connectionId)
            => ExecuteAsync(connectionId, x => x.GetDeviceInfoAsync());

        public Task<HuginOperationResponseDto> GetXReportAsync(Guid connectionId)
            => ExecuteAsync(connectionId, x => x.GetXReportAsync());

        public Task<HuginOperationResponseDto> GetZReportAsync(Guid connectionId)
            => ExecuteAsync(connectionId, x => x.GetZReportAsync());

        public Task<HuginOperationResponseDto> SendJsonDocumentAsync(Guid connectionId, HuginJsonDocumentRequestDto request)
            => ExecuteAsync(connectionId, x => x.SendJsonDocumentAsync(request));


        public Task<HuginOperationResponseDto> AddCashPaymentAsync(Guid connectionId, HuginCashPaymentRequestDto request)
            => ExecuteAsync(connectionId, x => x.AddCashPaymentAsync(request));

        public Task<HuginOperationResponseDto> AddCardPaymentAsync(Guid connectionId, HuginCardPaymentRequestDto request)
            => ExecuteAsync(connectionId, x => x.AddCardPaymentAsync(request));

        public Task<HuginOperationResponseDto> CloseReceiptAsync(Guid connectionId)
            => ExecuteAsync(connectionId, x => x.CloseReceiptAsync());
        private async Task<HuginOperationResponseDto> ExecuteAsync(
            Guid connectionId,
            Func<IHuginAdapter, Task<PosOperationResult>> operation)
        {
            var connection = _connectionManager.Get(connectionId);
            if (connection == null)
            {
                return new HuginOperationResponseDto
                {
                    Success = false,
                    Message = "Baglanti bulunamadi"
                };
            }

            await connection.Semaphore.WaitAsync();
            try
            {
                var result = await operation(connection.Adapter);

                return new HuginOperationResponseDto
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data,
                    ErrorCode = result.ResultCode
                };
            }
            finally
            {
                connection.Semaphore.Release();
            }
        }
    }
}
