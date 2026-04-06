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

        public bool IsConnected => _isConnected;

        public BekoAdapter(string token = "TOKEN FINTECH")
        {
            _communication = POSCommunication.getInstance(token);
            _communication.setSerialInCallback(SerialInCallback);
            _communication.setDeviceStateCallback(DeviceStateCallback);
        }

        public Task<PosOperationResult> ConnectAsync()
        {
            var deviceIndex = _communication.getActiveDeviceIndex();
            if (deviceIndex < 0)
                return Task.FromResult(PosOperationResult.Fail("Beko cihaz bulunamadi."));

            _isConnected = true;
            _lastDeviceIndex = deviceIndex;
            return Task.FromResult(PosOperationResult.Ok("Beko baglantisi hazir.", new { deviceIndex }));
        }

        public Task<PosOperationResult> DisconnectAsync()
        {
            _isConnected = false;
            return Task.FromResult(PosOperationResult.Ok("Beko baglantisi kapatildi."));
        }

        public async Task<PosOperationResult> GetStatusAsync()
        {
            EnsureConnected();

            var deviceIndex = _communication.getActiveDeviceIndex();
            _lastDeviceIndex = deviceIndex >= 0 ? deviceIndex : _lastDeviceIndex;

            if (deviceIndex < 0)
                return PosOperationResult.Fail("Beko cihaz bulunamadi.");

            var fiscalInfoRaw = _communication.getFiscalInfo();
            if (string.IsNullOrWhiteSpace(fiscalInfoRaw))
                return PosOperationResult.Fail("Beko cihaz durum bilgisi alinamadi.");

            _isQuickSaleScreenAvailable =
                !fiscalInfoRaw.Contains("ECR Hızlı Satış Ekranında Değil.", StringComparison.OrdinalIgnoreCase) &&
                !fiscalInfoRaw.Contains("ECR Hizli Satis Ekraninda Degil.", StringComparison.OrdinalIgnoreCase) &&
                !fiscalInfoRaw.Contains("ECR HÄ±zlÄ± SatÄ±ÅŸ EkranÄ±nda DeÄŸil.", StringComparison.OrdinalIgnoreCase);

            if (!_isQuickSaleScreenAvailable)
            {
                return PosOperationResult.Ok("Beko durum bilgisi alindi.", new
                {
                    isConnected = true,
                    statusCode = "NOT_ON_QUICK_SALE_SCREEN",
                    deviceState = "Cihaz hizli satis ekraninda degil.",
                    hasActiveOperation = false,
                    isReadyForNewSale = false,
                    serialNo = _serialNo,
                    deviceIndex = _lastDeviceIndex
                });
            }

            _callbackWaiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = _communication.sendPayment("{\"isVoid\": true}");

            if (result != 1)
                return PosOperationResult.Fail("Durum sorgusu icin iptal kontrol istegi Beko cihaza gonderilemedi.", result);

            string callbackJson;
            try
            {
                callbackJson = await WaitCallbackAsync();
            }
            catch (TaskCanceledException)
            {
                return PosOperationResult.Ok("Beko durum bilgisi alindi.", new
                {
                    isConnected = true,
                    statusCode = "NO_CALLBACK",
                    deviceState = "Cihaz bagli ancak durum sorgusuna callback donmedi.",
                    hasActiveOperation = false,
                    isReadyForNewSale = false,
                    serialNo = _serialNo,
                    deviceIndex = _lastDeviceIndex
                });
            }
            var receiptInfo = TryDeserializeReceiptInfo(callbackJson);

            if (receiptInfo == null)
                return PosOperationResult.Fail("Beko durum bilgisi callback verisi cozumlenemedi.");

            if (receiptInfo.status == -1 && IsAnyMessage(receiptInfo.message,
                "Odeme baslamadan once satis yapiniz",
                "Ödeme başlamadan önce satış yapınız",
                "Ã–deme baÅŸlamadan Ã¶nce satÄ±ÅŸ yapÄ±nÄ±z"))
            {
                return PosOperationResult.Ok("Beko durum bilgisi alindi.", new
                {
                    isConnected = true,
                    statusCode = "IDLE",
                    deviceState = "Cihaz satis ekraninda ve yeni satis icin hazir.",
                    hasActiveOperation = false,
                    isReadyForNewSale = true,
                    serialNo = _serialNo,
                    deviceIndex = _lastDeviceIndex
                });
            }

            if (IsAnyMessage(receiptInfo.message,
                "Odeme basladiktan sonra iptal yapilamaz",
                "Ödeme başladıktan sonra iptal yapılamaz",
                "Ã–deme baÅŸladÄ±ktan sonra iptal yapÄ±lamaz"))
            {
                return PosOperationResult.Ok("Beko durum bilgisi alindi.", new
                {
                    isConnected = true,
                    statusCode = "PAYMENT",
                    deviceState = "Odeme baslamis, cihazda bitirilmemis veya kismi odenmis fis var.",
                    hasActiveOperation = true,
                    isReadyForNewSale = false,
                    serialNo = _serialNo,
                    deviceIndex = _lastDeviceIndex
                });
            }

            if (receiptInfo.status == 0 && receiptInfo.receiptNo == -1 && receiptInfo.zNo == -1)
            {
                return PosOperationResult.Ok("Beko durum bilgisi alindi.", new
                {
                    isConnected = true,
                    statusCode = "OPEN_SALE",
                    deviceState = "Odeme alinmamis acik fis vardi ve iptal edildi.",
                    hasActiveOperation = true,
                    isReadyForNewSale = false,
                    serialNo = _serialNo,
                    deviceIndex = _lastDeviceIndex,
                    basketId = receiptInfo.basketID
                });
            }

            return PosOperationResult.Ok("Beko durum bilgisi alindi.", new
            {
                isConnected = true,
                statusCode = "UNKNOWN",
                deviceState = string.IsNullOrWhiteSpace(receiptInfo.message)
                    ? "Cihaz durumu yorumlanamadi."
                    : receiptInfo.message,
                hasActiveOperation = receiptInfo.status == 0 || receiptInfo.status == -1,
                isReadyForNewSale = false,
                serialNo = _serialNo,
                deviceIndex = _lastDeviceIndex,
                rawResponse = receiptInfo
            });
        }

        public async Task<PosOperationResult> GetDeviceInfoAsync()
        {
            EnsureConnected();
            var json = _communication.getFiscalInfo();
            return PosOperationResult.Ok("Cihaz bilgisi alindi.", JsonSerializer.Deserialize<object>(json)!);
        }

        // public async Task<PosOperationResult> SendBasketAsync(BekoBasketRequestDto request)
        // {
        //     EnsureConnected();

        //     var payload = new
        //     {
        //         basketID = request.BasketId,
        //         documentType = request.DocumentType,
        //         taxFreeAmount = request.TaxFreeAmount,
        //         createInvoice = request.CreateInvoice,
        //         isWayBill = request.IsWayBill,
        //         note = request.Note,
        //         items = request.Items.Select(x => new
        //         {
        //             barcode = x.Barcode,
        //             name = x.Name,
        //             pluNo = x.PluNo,
        //             price = x.Price,
        //             sectionNo = x.SectionNo,
        //             taxPercent = x.TaxPercent,
        //             type = x.Type,
        //             unit = x.Unit,
        //             vatID = x.VatId,
        //             limit = x.Limit,
        //             quantity = x.Quantity
        //         }).ToList(),
        //         paymentItems = request.PaymentItems.Select(x => new
        //         {
        //             description = x.Description,
        //             amount = x.Amount,
        //             type = x.Type
        //         }).ToList()
        //     };

        //     _callbackWaiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
        //     _communication.sendBasket(JsonSerializer.Serialize(payload));
        //     var callbackJson = await WaitCallbackAsync();

        //     return PosOperationResult.Ok("Sepet Beko cihaza gonderildi.", JsonSerializer.Deserialize<object>(callbackJson)!);
        // }

        public async Task<PosOperationResult> SendBasketAsync(BekoBasketRequestDto request)
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

            if (receiptResult.Status != 0 && !HasPaymentProgress(receiptResult))
                return PosOperationResult.Fail(receiptResult.Message ?? "Beko satis islemi basarisiz.");

            return PosOperationResult.Ok("Sepet Beko cihaza gonderildi.", receiptResult);
        }


        // public async Task<PosOperationResult> SendPaymentAsync(BekoPaymentRequestDto request)
        // {
        //     EnsureConnected();

        //     _callbackWaiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
        //     var result = _communication.sendPayment(JsonSerializer.Serialize(new
        //     {
        //         description = request.Description,
        //         amount = request.Amount,
        //         type = request.Type
        //     }));

        //     if (result != 1)
        //         return PosOperationResult.Fail("Odeme istegi Beko cihaza gonderilemedi.", result);

        //     var callbackJson = await WaitCallbackAsync();
        //     return PosOperationResult.Ok("Odeme sonucu alindi.", JsonSerializer.Deserialize<object>(callbackJson)!);
        // }

        public async Task<PosOperationResult> SendPaymentAsync(BekoPaymentRequestDto request)
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

            if (receiptResult.Status != 0 && !string.Equals(receiptResult.Message, "OK", StringComparison.OrdinalIgnoreCase))
                return PosOperationResult.Fail(receiptResult.Message ?? "Odeme basarisiz.");

            return PosOperationResult.Ok("Odeme sonucu alindi.", receiptResult);
        }


        public async Task<PosOperationResult> VoidReceiptAsync()
        {
            EnsureConnected();

            _callbackWaiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _communication.sendPayment("{\"isVoid\": true}");
            var callbackJson = await WaitCallbackAsync();

            return PosOperationResult.Ok("Fis iptal sonucu alindi.", JsonSerializer.Deserialize<object>(callbackJson)!);
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
            _lastCallbackType = type;
            _lastCallbackValue = value;
            _lastCallbackAtUtc = DateTime.UtcNow;

            if (type == 9)
                _isQuickSaleScreenAvailable = false;

            _callbackWaiter?.TrySetResult(value);
        }

        private void DeviceStateCallback(bool isConnected, [MarshalAs(UnmanagedType.BStr)] string id)
        {
            _isConnected = isConnected;
            _serialNo = id;
            _lastCallbackAtUtc = DateTime.UtcNow;
            if (!isConnected)
                _isQuickSaleScreenAvailable = false;
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

        private static bool IsAnyMessage(string? value, params string[] expectedValues)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            foreach (var expectedValue in expectedValues)
            {
                if (string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
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
