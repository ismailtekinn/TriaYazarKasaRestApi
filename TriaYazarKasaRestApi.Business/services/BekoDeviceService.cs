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
        private readonly IAutoConnectionStore _autoConnectionStore;

        public BekoDeviceService(
            IBekoConnectionManager connectionManager,
            IAutoConnectionStore autoConnectionStore)
        {
            _connectionManager = connectionManager;
            _autoConnectionStore = autoConnectionStore;
        }

        public async Task<BekoConnectionResponseDto> ConnectAsync(BekoConnectRequestDto request)
        {
            try
            {
                var adapter = new BekoAdapter(request.Token);
                var result = await adapter.ConnectAsync();
                var id = Guid.NewGuid();

                if (result.Success)
                {
                    _connectionManager.Add(new BekoActiveConnection
                    {
                        ConnectionId = id,
                        Adapter = adapter,
                        ConnectedAt = DateTime.UtcNow
                    });
                    _autoConnectionStore.SetBeko(id);
                }

                return new BekoConnectionResponseDto
                {
                    ConnectionId = id,
                    IsConnected = result.Success,
                    Status = result.Success ? PosConnectionStatus.Connected : PosConnectionStatus.Error,
                    Message = result.Message,
                    ConnectedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new BekoConnectionResponseDto
                {
                    ConnectionId = Guid.Empty,
                    IsConnected = false,
                    Status = PosConnectionStatus.Error,
                    Message = $"Beko baglantisi kurulamadi. Detay: {ex.Message}",
                    ConnectedAt = DateTime.UtcNow
                };
            }
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

            if (_autoConnectionStore.BekoConnectionId == connectionId)
            {
                _autoConnectionStore.SetBeko(null);
            }

            return new BekoOperationResponseDto
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data,
                ErrorCode = result.ResultCode
            };
        }

        public Task<BekoOperationResponseDto> GetStatusAsync(Guid connectionId)
            => ExecuteWithoutLockAsync(connectionId, x => x.GetStatusAsync());

        public Task<BekoOperationResponseDto> GetDeviceInfoAsync(Guid connectionId)
            => ExecuteWithoutLockAsync(connectionId, x => x.GetDeviceInfoAsync());

        public Task<BekoOperationResponseDto> SendBasketAsync(Guid connectionId, BekoBasketRequestDto request)
            => ExecuteAsync(connectionId, x => x.SendBasketAsync(request));

        public Task<BekoOperationResponseDto> SendPaymentAsync(Guid connectionId, BekoPaymentRequestDto request)
            => ExecuteAsync(connectionId, x => x.SendPaymentAsync(request));

        public Task<BekoOperationResponseDto> VoidReceiptAsync(Guid connectionId)
            => ExecuteAsync(connectionId, x => x.VoidReceiptAsync());

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
            catch (Exception ex)
            {
                return new BekoOperationResponseDto
                {
                    Success = false,
                    Message = $"Beko cihaz islemi sirasinda beklenmeyen hata olustu. Detay: {ex.Message}"
                };
            }
            finally
            {
                connection.Semaphore.Release();
            }
        }

        private async Task<BekoOperationResponseDto> ExecuteWithoutLockAsync(Guid connectionId, Func<IBekoAdapter, Task<PosOperationResult>> op)
        {
            var connection = _connectionManager.Get(connectionId);
            if (connection == null)
                return new BekoOperationResponseDto { Success = false, Message = "Baglanti bulunamadi" };

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
            catch (Exception ex)
            {
                return new BekoOperationResponseDto
                {
                    Success = false,
                    Message = $"Beko cihaz sorgusu sirasinda beklenmeyen hata olustu. Detay: {ex.Message}"
                };
            }
        }
    }
}
