using System.Runtime.InteropServices;
using System.Text.Json;
using IntegrationHub;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.Models;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.Adapters
{
    public class BekoAdapter : IBekoAdapter
    {
        private readonly POSCommunication _communication;
        private TaskCompletionSource<string>? _callbackWaiter;
        private volatile bool _isConnected;
        private string? _serialNo;
        private int? _lastDeviceIndex;
        private int? _lastCallbackType;
        private string? _lastCallbackValue;
        private DateTime? _lastCallbackAtUtc;
        private bool _isQuickSaleScreenAvailable = true;
        private volatile bool _hasActiveSale;
        private volatile bool _paymentInProgress;
        private string? _activeBasketId;
        private DateTime? _lastSaleActivityUtc;

        public bool IsConnected => _isConnected;

        public BekoAdapter(string token = "TOKEN FINTECH")
        {
            _communication = POSCommunication.getInstance(token);
            _communication.setSerialInCallback(SerialInCallback);
            _communication.setDeviceStateCallback(DeviceStateCallback);
        }

        public Task<PosOperationResult> ConnectAsync()
        {
            try
            {
                var deviceIndex = _communication.getActiveDeviceIndex();
                if (deviceIndex < 0)
                    return Task.FromResult(PosOperationResult.Fail("Beko cihaz bulunamadi."));

                _isConnected = true;
                _lastDeviceIndex = deviceIndex;
                return Task.FromResult(PosOperationResult.Ok("Beko baglantisi hazir.", new { deviceIndex }));
            }
            catch (Exception ex)
            {
                MarkCommunicationAsBroken();
                return Task.FromResult(CreateCommunicationErrorResult("Beko cihaz baglantisi kurulurken USB/driver hatasi olustu.", ex));
            }
        }

        public Task<PosOperationResult> DisconnectAsync()
        {
            _isConnected = false;
            ClearSaleState(true);
            return Task.FromResult(PosOperationResult.Ok("Beko baglantisi kapatildi."));
        }

        public Task<PosOperationResult> GetStatusAsync()
        {
            try
            {
                var deviceIndex = _communication.getActiveDeviceIndex();
                if (deviceIndex < 0)
                {
                    _isConnected = false;
                    ClearSaleState();
                    return Task.FromResult(PosOperationResult.Ok("Beko durumu alindi.", new
                    {
                        isConnected = false,
                        statusCode = "DISCONNECTED",
                        deviceState = "Cihaz bagli degil.",
                        isReadyForNewSale = false,
                        hasActiveSale = false,
                        paymentInProgress = false,
                        activeBasketId = (string?)null,
                        lastSaleActivityUtc = _lastSaleActivityUtc
                    }));
                }

                _isConnected = true;
                _lastDeviceIndex = deviceIndex;

                var fiscalInfoRaw = _communication.getFiscalInfo();
                if (string.IsNullOrWhiteSpace(fiscalInfoRaw))
                {
                    return Task.FromResult(PosOperationResult.Ok("Beko durumu alindi.", new
                    {
                        isConnected = true,
                        statusCode = "UNKNOWN",
                        deviceState = "Fiscal bilgi okunamadi.",
                        isReadyForNewSale = false,
                        hasActiveSale = _hasActiveSale,
                        paymentInProgress = _paymentInProgress,
                        activeBasketId = _activeBasketId,
                        lastSaleActivityUtc = _lastSaleActivityUtc
                    }));
                }

                _isQuickSaleScreenAvailable =
                    !fiscalInfoRaw.Contains("ECR HÃ„Â±zlÃ„Â± SatÃ„Â±Ã…Å¸ EkranÃ„Â±nda DeÃ„Å¸il.", StringComparison.OrdinalIgnoreCase) &&
                    !fiscalInfoRaw.Contains("ECR Hizli Satis Ekraninda Degil.", StringComparison.OrdinalIgnoreCase) &&
                    !fiscalInfoRaw.Contains("ECR HÃƒâ€Ã‚Â±zlÃƒâ€Ã‚Â± SatÃƒâ€Ã‚Â±Ãƒâ€¦Ã…Â¸ EkranÃƒâ€Ã‚Â±nda DeÃƒâ€Ã…Â¸il.", StringComparison.OrdinalIgnoreCase);

                if (!_isQuickSaleScreenAvailable)
                {
                    return Task.FromResult(PosOperationResult.Ok("Beko durum bilgisi alindi.", new
                    {
                        isConnected = true,
                        statusCode = "NOT_ON_QUICK_SALE_SCREEN",
                        deviceState = "Cihaz hizli satis ekraninda degil.",
                        isReadyForNewSale = false,
                        hasActiveSale = _hasActiveSale,
                        paymentInProgress = _paymentInProgress,
                        activeBasketId = _activeBasketId,
                        lastSaleActivityUtc = _lastSaleActivityUtc,
                        serialNo = _serialNo,
                        deviceIndex = _lastDeviceIndex
                    }));
                }

                if (_hasActiveSale || _paymentInProgress)
                {
                    return Task.FromResult(PosOperationResult.Ok("Beko durum bilgisi alindi.", new
                    {
                        isConnected = true,
                        statusCode = "BUSY",
                        deviceState = "Cihazda aktif satis veya odeme islemi var.",
                        isReadyForNewSale = false,
                        hasActiveSale = _hasActiveSale,
                        paymentInProgress = _paymentInProgress,
                        activeBasketId = _activeBasketId,
                        lastSaleActivityUtc = _lastSaleActivityUtc,
                        serialNo = _serialNo,
                        deviceIndex = _lastDeviceIndex
                    }));
                }

                return Task.FromResult(PosOperationResult.Ok("Beko durum bilgisi alindi.", new
                {
                    isConnected = true,
                    statusCode = "READY",
                    deviceState = "Cihaz bagli ve yeni satis icin hazir.",
                    isReadyForNewSale = true,
                    hasActiveSale = false,
                    paymentInProgress = false,
                    activeBasketId = _activeBasketId,
                    lastSaleActivityUtc = _lastSaleActivityUtc,
                    serialNo = _serialNo,
                    deviceIndex = _lastDeviceIndex
                }));
            }
            catch (Exception ex)
            {
                MarkCommunicationAsBroken();
                return Task.FromResult(CreateCommunicationErrorResult("Beko durum bilgisi alinirken USB/driver hatasi olustu.", ex));
            }
        }

        public Task<PosOperationResult> GetDeviceInfoAsync()
            => ExecuteCommunicationAsync(
                "Beko cihaz bilgisi alinirken USB/driver hatasi olustu.",
                () =>
                {
                    EnsureConnected();
                    var json = _communication.getFiscalInfo();
                    return PosOperationResult.Ok("Cihaz bilgisi alindi.", JsonSerializer.Deserialize<object>(json)!);
                });

        public async Task<PosOperationResult> SendBasketAsync(BekoBasketRequestDto request)
        {
            try
            {
                EnsureConnected();

                var payload = new
                {
                    basketID = request.BasketId,
                    documentType = request.DocumentType,
                    taxFreeAmount = request.TaxFreeAmount,
                    createInvoice = request.CreateInvoice,
                    isWayBill = request.IsWayBill,
                    note = request.Note,
                    items = request.Items.Select(x => new
                    {
                        barcode = x.Barcode,
                        name = x.Name,
                        pluNo = x.PluNo,
                        price = x.Price,
                        sectionNo = x.SectionNo,
                        taxPercent = x.TaxPercent,
                        type = x.Type,
                        unit = x.Unit,
                        vatID = x.VatId,
                        limit = x.Limit,
                        quantity = x.Quantity
                    }).ToList(),
                    paymentItems = request.PaymentItems.Select(x => new
                    {
                        description = x.Description,
                        amount = x.Amount,
                        type = x.Type
                    }).ToList()
                };

                _callbackWaiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _hasActiveSale = true;
                _paymentInProgress = true;
                _activeBasketId = request.BasketId;
                _lastSaleActivityUtc = DateTime.UtcNow;

                _communication.sendBasket(JsonSerializer.Serialize(payload));

                string callbackJson;
                try
                {
                    callbackJson = await WaitCallbackAsync();
                }
                catch (TaskCanceledException)
                {
                    return PosOperationResult.Fail("Beko ilk callback verisi zamaninda donmedi.");
                }

                var receiptResult = MapReceiptResult(callbackJson);
                if (receiptResult == null)
                    return PosOperationResult.Fail("Beko callback verisi cozumlenemedi.");

                UpdateSaleState(receiptResult);

                if (receiptResult.Status != 0 && !HasPaymentProgress(receiptResult))
                    return PosOperationResult.Fail(receiptResult.Message ?? "Beko satis islemi basarisiz.");

                return PosOperationResult.Ok("Sepet Beko cihaza gonderildi.", receiptResult);
            }
            catch (Exception ex)
            {
                MarkCommunicationAsBroken();
                return CreateCommunicationErrorResult("Beko sepet gonderimi sirasinda USB/driver hatasi olustu.", ex);
            }
        }

        public async Task<PosOperationResult> SendPaymentAsync(BekoPaymentRequestDto request)
        {
            try
            {
                EnsureConnected();

                _callbackWaiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
                var result = _communication.sendPayment(JsonSerializer.Serialize(new
                {
                    description = request.Description,
                    amount = request.Amount,
                    type = request.Type
                }));

                if (result != 1)
                    return PosOperationResult.Fail("Odeme istegi Beko cihaza gonderilemedi.", result);

                var callbackJson = await WaitCallbackAsync();
                var receiptResult = MapReceiptResult(callbackJson);

                if (receiptResult == null)
                    return PosOperationResult.Fail("Beko odeme callback verisi cozumlenemedi.");

                UpdateSaleState(receiptResult);

                if (receiptResult.Status != 0 && !string.Equals(receiptResult.Message, "OK", StringComparison.OrdinalIgnoreCase))
                    return PosOperationResult.Fail(receiptResult.Message ?? "Odeme basarisiz.");

                return PosOperationResult.Ok("Odeme sonucu alindi.", receiptResult);
            }
            catch (TaskCanceledException)
            {
                return PosOperationResult.Fail("Beko odeme callback verisi zamaninda donmedi.");
            }
            catch (Exception ex)
            {
                MarkCommunicationAsBroken();
                return CreateCommunicationErrorResult("Beko odeme gonderimi sirasinda USB/driver hatasi olustu.", ex);
            }
        }

        public async Task<PosOperationResult> VoidReceiptAsync()
        {
            try
            {
                EnsureConnected();

                _callbackWaiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _communication.sendPayment("{\"isVoid\": true}");
                var callbackJson = await WaitCallbackAsync();

                var receiptResult = MapReceiptResult(callbackJson);
                if (receiptResult != null)
                    UpdateSaleState(receiptResult);
                else
                    ClearSaleState();

                return PosOperationResult.Ok("Fis iptal sonucu alindi.", JsonSerializer.Deserialize<object>(callbackJson)!);
            }
            catch (TaskCanceledException)
            {
                return PosOperationResult.Fail("Beko fis iptal callback verisi zamaninda donmedi.");
            }
            catch (Exception ex)
            {
                MarkCommunicationAsBroken();
                return CreateCommunicationErrorResult("Beko fis iptal islemi sirasinda USB/driver hatasi olustu.", ex);
            }
        }

        private async Task<string> WaitCallbackAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var _ = cts.Token.Register(() => _callbackWaiter?.TrySetCanceled());
            return await _callbackWaiter!.Task;
        }

        private void EnsureConnected()
        {
            if (!_isConnected)
                throw new InvalidOperationException("Beko cihazi bagli degil.");
        }

        private void SerialInCallback(int type, [MarshalAs(UnmanagedType.BStr)] string value)
        {
            try
            {
                _lastCallbackType = type;
                _lastCallbackValue = value;
                _lastCallbackAtUtc = DateTime.UtcNow;

                if (type == 9)
                    _isQuickSaleScreenAvailable = false;

                _callbackWaiter?.TrySetResult(value);

                var receiptResult = MapReceiptResult(value);
                if (receiptResult != null)
                    UpdateSaleState(receiptResult);
            }
            catch
            {
                MarkCommunicationAsBroken();
                _callbackWaiter?.TrySetException(new InvalidOperationException("Beko callback verisi islenirken hata olustu."));
            }
        }

        private void DeviceStateCallback(bool isConnected, [MarshalAs(UnmanagedType.BStr)] string id)
        {
            try
            {
                _isConnected = isConnected;
                _serialNo = id;
                _lastCallbackAtUtc = DateTime.UtcNow;
                if (!isConnected)
                {
                    _isQuickSaleScreenAvailable = false;
                    ClearSaleState();
                }
            }
            catch
            {
                MarkCommunicationAsBroken();
            }
        }

        private void MarkCommunicationAsBroken()
        {
            _isConnected = false;
            _isQuickSaleScreenAvailable = false;
            ClearSaleState();
        }

        private Task<PosOperationResult> ExecuteCommunicationAsync(string errorMessage, Func<PosOperationResult> operation)
        {
            try
            {
                return Task.FromResult(operation());
            }
            catch (Exception ex)
            {
                MarkCommunicationAsBroken();
                return Task.FromResult(CreateCommunicationErrorResult(errorMessage, ex));
            }
        }

        private static PosOperationResult CreateCommunicationErrorResult(string message, Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(ex.Message))
                message = $"{message} Detay: {ex.Message}";

            return PosOperationResult.Fail(message);
        }

        private static BekoReceiptInfo? TryDeserializeReceiptInfo(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<BekoReceiptInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        private static BekoReceiptResultDto? MapReceiptResult(string callbackJson)
        {
            var receipt = TryDeserializeReceiptInfo(callbackJson);
            if (receipt == null)
                return null;

            return new BekoReceiptResultDto
            {
                BasketId = receipt.basketID,
                Status = receipt.status,
                Message = receipt.message,
                ReceiptNo = receipt.receiptNo > 0 ? receipt.receiptNo : null,
                ZNo = receipt.zNo > 0 ? receipt.zNo : null,
                Uuid = receipt.UUID,
                Payments = receipt.paymentItems?.Select(x => new BekoReceiptPaymentDto
                {
                    Type = x.type,
                    Description = x.description,
                    Amount = x.amount,
                    BatchNo = x.BatchNo,
                    TxnNo = x.TxnNo,
                    OperatorId = x.operatorId,
                    RefundInfo = x.refundInfo
                }).ToList() ?? new List<BekoReceiptPaymentDto>()
            };
        }

        private static bool HasPaymentProgress(BekoReceiptResultDto receiptResult)
            => receiptResult.Payments.Count > 0;

        private void UpdateSaleState(BekoReceiptResultDto receiptResult)
        {
            _lastSaleActivityUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(receiptResult.BasketId))
                _activeBasketId = receiptResult.BasketId;

            if (IsCompletedReceipt(receiptResult) || IsCanceledOrFailedReceipt(receiptResult))
            {
                ClearSaleState(false);
                return;
            }

            if (receiptResult.Payments.Count > 0)
            {
                _hasActiveSale = true;
                _paymentInProgress = true;
                return;
            }

            if (receiptResult.Status == -1 && !string.IsNullOrWhiteSpace(receiptResult.Message))
            {
                ClearSaleState(false);
                return;
            }

            if (receiptResult.Status > 0)
                ClearSaleState(false);
        }

        private void ClearSaleState(bool clearTimestamp = false)
        {
            _hasActiveSale = false;
            _paymentInProgress = false;
            _activeBasketId = null;
            if (clearTimestamp)
                _lastSaleActivityUtc = null;
        }

        private static bool IsCompletedReceipt(BekoReceiptResultDto receiptResult)
            => receiptResult.Status == 0 &&
               (receiptResult.ReceiptNo.GetValueOrDefault() > 0 ||
                receiptResult.ZNo.GetValueOrDefault() > 0 ||
                IsMessage(receiptResult.Message, "OK"));

        private static bool IsCanceledOrFailedReceipt(BekoReceiptResultDto receiptResult)
        {
            if (receiptResult.Status > 0)
                return true;

            if (receiptResult.Status == 0 &&
                receiptResult.ReceiptNo.GetValueOrDefault() == 0 &&
                receiptResult.ZNo.GetValueOrDefault() == 0 &&
                !string.IsNullOrWhiteSpace(receiptResult.BasketId))
                return true;

            return ContainsAny(
                receiptResult.Message,
                "iptal",
                "basarisiz",
                "baÅŸarÄ±sÄ±z",
                "odeme basarisiz",
                "Ã¶deme baÅŸarÄ±sÄ±z",
                "void",
                "cancel");
        }

        private static bool IsMessage(string? value, string expected)
            => string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

        private static bool ContainsAny(string? value, params string[] parts)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            foreach (var part in parts)
            {
                if (value.Contains(part, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private sealed class BekoReceiptInfo
        {
            public string? basketID { get; set; }
            public int receiptNo { get; set; }
            public int zNo { get; set; }
            public int status { get; set; }
            public string? message { get; set; }
            public string? UUID { get; set; }
            public List<BekoPaymentInfo>? paymentItems { get; set; }
        }

        private sealed class BekoPaymentInfo
        {
            public int type { get; set; }
            public string? description { get; set; }
            public decimal amount { get; set; }
            public int BatchNo { get; set; }
            public int TxnNo { get; set; }
            public int operatorId { get; set; }
            public string? refundInfo { get; set; }
        }
    }
}
