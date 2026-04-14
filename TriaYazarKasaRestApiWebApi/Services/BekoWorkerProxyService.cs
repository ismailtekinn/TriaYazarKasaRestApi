using System.Net.Http.Json;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;
using TriaYazarKasaRestApi.Entities.Enums;

namespace TriaYazarKasaRestApiWebApi.Services
{
    public class BekoWorkerProxyService : IBekoDeviceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BekoWorkerProxyService> _logger;

        public BekoWorkerProxyService(HttpClient httpClient, ILogger<BekoWorkerProxyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<BekoConnectionResponseDto> ConnectAsync(BekoConnectRequestDto request)
            => PostAsync<BekoConnectRequestDto, BekoConnectionResponseDto>("api/beko/connect", request, CreateDisconnectedResponse);

        public Task<BekoOperationResponseDto> DisconnectAsync(Guid connectionId)
            => DeleteAsync($"api/beko/{connectionId}/disconnect");

        public Task<BekoOperationResponseDto> GetStatusAsync(Guid connectionId)
            => GetAsync($"api/beko/{connectionId}/status");

        public Task<BekoOperationResponseDto> GetDeviceInfoAsync(Guid connectionId)
            => GetAsync($"api/beko/{connectionId}/device-info");

        public Task<BekoOperationResponseDto> SendBasketAsync(Guid connectionId, BekoBasketRequestDto request)
            => PostAsync<BekoBasketRequestDto, BekoOperationResponseDto>($"api/beko/{connectionId}/basket", request, CreateOperationFailure);

        public Task<BekoOperationResponseDto> SendBasketAsync2(Guid connectionId, BekoBasketRequestDto request)
            => PostAsync<BekoBasketRequestDto, BekoOperationResponseDto>($"api/beko/{connectionId}/basket2", request, CreateOperationFailure);

        public Task<BekoOperationResponseDto> GetBasketOperationStatusAsync(Guid connectionId, string basketId)
            => GetAsync($"api/beko/{connectionId}/basket2/{basketId}");

        public Task<BekoOperationResponseDto> SendPaymentAsync(Guid connectionId, BekoPaymentRequestDto request)
            => PostAsync<BekoPaymentRequestDto, BekoOperationResponseDto>($"api/beko/{connectionId}/payment", request, CreateOperationFailure);

        public Task<BekoOperationResponseDto> VoidReceiptAsync(Guid connectionId)
            => PostAsync<object, BekoOperationResponseDto>($"api/beko/{connectionId}/void", new { }, CreateOperationFailure);

        private async Task<BekoOperationResponseDto> GetAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BekoOperationResponseDto>() ?? CreateOperationFailure("Beko worker yanit dondurmedi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beko worker GET cagrisi basarisiz oldu. Url: {Url}", url);
                return CreateOperationFailure($"Beko worker erisilemedi. Detay: {ex.Message}");
            }
        }

        private async Task<BekoOperationResponseDto> DeleteAsync(string url)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BekoOperationResponseDto>() ?? CreateOperationFailure("Beko worker yanit dondurmedi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beko worker DELETE cagrisi basarisiz oldu. Url: {Url}", url);
                return CreateOperationFailure($"Beko worker erisilemedi. Detay: {ex.Message}");
            }
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest request, Func<string, TResponse> failureFactory)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TResponse>() ?? failureFactory("Beko worker yanit dondurmedi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beko worker POST cagrisi basarisiz oldu. Url: {Url}", url);
                return failureFactory($"Beko worker erisilemedi. Detay: {ex.Message}");
            }
        }

        private static BekoOperationResponseDto CreateOperationFailure(string message)
            => new()
            {
                Success = false,
                Message = message
            };

        private static BekoConnectionResponseDto CreateDisconnectedResponse(string message)
            => new()
            {
                ConnectionId = Guid.Empty,
                IsConnected = false,
                Status = PosConnectionStatus.Error,
                Message = message,
                ConnectedAt = DateTime.UtcNow
            };
    }
}
