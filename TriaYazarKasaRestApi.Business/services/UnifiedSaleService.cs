using System.Text.Json;
using System.Text.Json.Serialization;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.services
{
    public class UnifiedSaleService : IUnifiedSaleService
    {
        private readonly IBekoDeviceService _bekoDeviceService;
        private readonly IHuginDeviceService _huginDeviceService;
        private readonly IAutoConnectionStore _autoConnectionStore;

        public UnifiedSaleService(
            IBekoDeviceService bekoDeviceService,
            IHuginDeviceService huginDeviceService,
            IAutoConnectionStore autoConnectionStore)
        {
            _bekoDeviceService = bekoDeviceService;
            _huginDeviceService = huginDeviceService;
            _autoConnectionStore = autoConnectionStore;
        }

        public async Task<object> ExecuteAsync(string deviceType, UnifiedSaleRequestDto request, Guid? connectionId = null)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.TransactionStartParam);
            ArgumentNullException.ThrowIfNull(request.TransactionStartParam.BelgeParams);

            var normalizedDeviceType = (deviceType ?? string.Empty).Trim().ToLowerInvariant();
            return normalizedDeviceType switch
            {
                "beko" => await ExecuteBekoAsync(request, connectionId),
                "hugin" => await ExecuteHuginAsync(request, connectionId),
                _ => throw new InvalidOperationException($"Desteklenmeyen cihaz tipi: {deviceType}")
            };
        }

        private async Task<object> ExecuteBekoAsync(UnifiedSaleRequestDto request, Guid? connectionId)
        {
            var resolvedConnectionId = connectionId ?? _autoConnectionStore.BekoConnectionId;
            if (resolvedConnectionId == null || resolvedConnectionId == Guid.Empty)
                throw new InvalidOperationException("Beko baglantisi bulunamadi.");

            var belge = request.TransactionStartParam.BelgeParams;
            var basketRequest = new BekoBasketRequestDto
            {
                BasketId = string.IsNullOrWhiteSpace(belge.BelgeNo) ? belge.Id.ToString() : belge.BelgeNo,
                DocumentType = ConvertBekoSaleType(belge.SaleType, belge.InvoiceType),
                TaxFreeAmount = ToBekoAmount(belge.KatkiPayiTutari),
                CreateInvoice = belge.InvoiceType != 0,
                IsWayBill = belge.IrsaliyeYerineGecer,
                Note = string.Join(" ", belge.BelgeSabitAltBilgisi
                    .Select(x => x.AciklamaBilgisi)
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                    .Trim()
            };

            basketRequest.Items = belge.BelgeDetay.Select((item, index) => new BekoBasketItemDto
            {
                Barcode = item.Barkod,
                Name = string.IsNullOrWhiteSpace(item.UrunAdi) ? $"Kalem {index + 1}" : item.UrunAdi,
                PluNo = item.Id > 0 ? item.Id : index + 1,
                Price = ToBekoAmount(item.BirimFiyat),
                SectionNo = item.DepartmanNo,
                TaxPercent = item.TaxPercent,
                Type = 0,
                Unit = "Adet",
                VatId = item.DepartmanNo,
                Limit = 0,
                Quantity = ToBekoQuantity(item.Adet)
            }).ToList();

            basketRequest.PaymentItems = belge.OdemeDetay.Select(payment => new BekoPaymentRequestDto
            {
                Type = ConvertBekoPaymentType(payment.OdemeTipi, payment.YazarkasaOdemeTipi),
                Amount = ToBekoAmount(payment.OdemeTutar - payment.ParaUstu),
                Description = ConvertBekoPaymentDescription(payment.OdemeTipi, payment.YazarkasaOdemeTipi)
            }).ToList();

            return await _bekoDeviceService.SendBasketAsync(resolvedConnectionId.Value, basketRequest);
        }

        private async Task<object> ExecuteHuginAsync(UnifiedSaleRequestDto request, Guid? connectionId)
        {
            var resolvedConnectionId = connectionId ?? _autoConnectionStore.HuginConnectionId;
            if (resolvedConnectionId == null || resolvedConnectionId == Guid.Empty)
                throw new InvalidOperationException("Hugin baglantisi bulunamadi.");

            var belge = request.TransactionStartParam.BelgeParams;
            var hasCardPayment = belge.OdemeDetay.Any(payment => payment.OdemeTipi == 0);
            var documentRequest = new HuginJsonDocumentRequestDto
            {
                FiscalItems = belge.BelgeDetay.Select((item, index) => new HuginJsonItemDto
                {
                    Id = item.Id > 0 ? item.Id : index + 1,
                    Quantity = item.Adet <= 0 ? 1 : item.Adet,
                    Price = item.BirimFiyat,
                    Name = item.UrunAdi,
                    Barcode = item.Barkod,
                    DeptId = item.DepartmanNo,
                    Status = 0,
                    Adj = item.IndirimTutar > 0
                        ? new HuginJsonAdjustmentDto
                        {
                            Type = 0,
                            Amount = item.IndirimTutar,
                            Percentage = 0
                        }
                        : null
                }).ToList(),
                Payments = hasCardPayment
                    ? null
                    : belge.OdemeDetay.Select((payment, index) => new HuginJsonPaymentDto
                    {
                        Type = ConvertHuginPaymentType(payment.OdemeTipi, payment.YazarkasaOdemeTipi),
                        Index = index + 1,
                        PaidTotal = payment.OdemeTutar - payment.ParaUstu,
                        ViaByEft = payment.OdemeTipi == 0
                    }).ToList(),
                FooterNotes = belge.BelgeSabitAltBilgisi
                    .SelectMany(x => (x.AciklamaBilgisi ?? string.Empty)
                        .Replace("\r\n", "\n")
                        .Replace('\r', '\n')
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .ToList(),
                EndOfReceiptInfo = new HuginEndOfReceiptDto
                {
                    CloseReceiptFlag = !hasCardPayment,
                    BarcodeFlag = false
                },
                PharmacyInfo = new HuginPharmacyInfoDto
                {
                    SSNNumber = belge.KatkiPayiTCNo,
                    ContributionAmount = belge.KatkiPayiTutari
                }
            };

            Console.WriteLine(documentRequest);

            return await _huginDeviceService.SendJsonDocumentAsync(resolvedConnectionId.Value, documentRequest);
        }

        private static int ToBekoAmount(decimal value)
            => (int)Math.Round(value * 100m, MidpointRounding.AwayFromZero);

        private static int ToBekoQuantity(decimal value)
            => (int)Math.Round((value <= 0 ? 1 : value) * 1000m, MidpointRounding.AwayFromZero);

        private static int ConvertBekoSaleType(int saleType, int invoiceType)
            => saleType switch
            {
                0 => 0,
                1 when invoiceType == 0 => 1,
                1 when invoiceType == 1 => 2,
                1 => 3,
                4 => 4,
                5 => 5,
                6 => 6,
                _ => 0
            };

        private static int ConvertBekoPaymentType(int paymentType, int yazarkasaOdemeTipi)
            => paymentType switch
            {
                0 => 3,
                1 => 1,
                2 => 2,
                3 => 38,
                4 => 17,
                22 => 4,
                28 => 17,
                29 => 18,
                30 => 19,
                31 => 20,
                32 => 21,
                33 => 22,
                34 => 23,
                35 => 24,
                36 => 25,
                38 => 38,
                39 => 39,
                40 => 40,
                99 => yazarkasaOdemeTipi,
                _ => 1
            };

        private static string ConvertBekoPaymentDescription(int paymentType, int yazarkasaOdemeTipi)
        {
            var resolved = ConvertBekoPaymentType(paymentType, yazarkasaOdemeTipi);
            return resolved switch
            {
                1 => "NAKIT",
                2 => "YEMEK KARTI",
                3 => "KREDI KARTI",
                17 => "ACIK HESAP",
                38 => "HAVALE EFT",
                _ => $"ODEME_{resolved}"
            };
        }

        private static int ConvertHuginPaymentType(int paymentType, int yazarkasaOdemeTipi)
            => paymentType switch
            {
                0 => 5,
                1 => 1,
                3 => 3,
                4 => 3,
                5 => 3,
                99 => yazarkasaOdemeTipi,
                _ => 1
            };
    }
}
