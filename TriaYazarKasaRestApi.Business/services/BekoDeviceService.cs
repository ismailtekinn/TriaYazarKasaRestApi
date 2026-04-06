using TriaYazarKasaRestApi.Business.Adapters;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.Models;
using TriaYazarKasaRestApi.Entities.DTOs;
using TriaYazarKasaRestApi.Entities.Enums;

namespace TriaYazarKasaRestApi.Business.services
{
    public class BekoDeviceService : IBekoDeviceService
    {
        private readonly IBekoConnectionManager _connectionManager;

        public BekoDeviceService(IBekoConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public async Task<BekoConnectionResponseDto> ConnectAsync(BekoConnectRequestDto request)
        {
            var adapter = new BekoAdapter(request.Token);
            var result = await adapter.ConnectAsync();
            var id = Guid.NewGuid();

            if (result.Success)
                _connectionManager.Add(new BekoActiveConnection { ConnectionId = id, Adapter = adapter, ConnectedAt = DateTime.UtcNow });

            return new BekoConnectionResponseDto
            {
                ConnectionId = id,
                IsConnected = result.Success,
                Status = result.Success ? PosConnectionStatus.Connected : PosConnectionStatus.Error,
                Message = result.Message,
                ConnectedAt = DateTime.UtcNow
            };
        }

        public async Task<BekoOperationResponseDto> DisconnectAsync(Guid connectionId)
        {
            var connection = _connectionManager.Get(connectionId);
            if (connection == null)
            {
                return new BekoOperationResponseDto
                {
                    Success = false,
                    Message = "Baglanti bulunamadi"
                };
            }

            var result = await connection.Adapter.DisconnectAsync();
            _connectionManager.Remove(connectionId);

            return new BekoOperationResponseDto
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data,
                ErrorCode = result.ResultCode
            };
        }
        public Task<BekoOperationResponseDto> GetStatusAsync(Guid connectionId) => ExecuteAsync(connectionId, x => x.GetStatusAsync());
        public Task<BekoOperationResponseDto> GetDeviceInfoAsync(Guid connectionId) => ExecuteAsync(connectionId, x => x.GetDeviceInfoAsync());
        public Task<BekoOperationResponseDto> SendBasketAsync(Guid connectionId, BekoBasketRequestDto request) => ExecuteAsync(connectionId, x => x.SendBasketAsync(request));
        public Task<BekoOperationResponseDto> SendPaymentAsync(Guid connectionId, BekoPaymentRequestDto request) => ExecuteAsync(connectionId, x => x.SendPaymentAsync(request));
        public Task<BekoOperationResponseDto> VoidReceiptAsync(Guid connectionId) => ExecuteAsync(connectionId, x => x.VoidReceiptAsync());

        private async Task<BekoOperationResponseDto> ExecuteAsync(Guid connectionId, Func<IBekoAdapter, Task<PosOperationResult>> op)
        {
            var connection = _connectionManager.Get(connectionId);
            if (connection == null)
                return new BekoOperationResponseDto { Success = false, Message = "Baglanti bulunamadi" };

            await connection.Semaphore.WaitAsync();
            try
            {
                var result = await op(connection.Adapter);
                return new BekoOperationResponseDto
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
