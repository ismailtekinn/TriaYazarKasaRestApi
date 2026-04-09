namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class UnifiedSaleRequestDto
    {
        public UnifiedTransactionStartParamDto TransactionStartParam { get; set; } = new();
    }

    public class UnifiedTransactionStartParamDto
    {
        public int IslemTipi { get; set; }
        public UnifiedKasiyerDto? Kasiyer { get; set; }
        public UnifiedBelgeParamsDto BelgeParams { get; set; } = new();
        public string? YoneticiSifresi { get; set; }
        public string? LisansData { get; set; }
    }

    public class UnifiedKasiyerDto
    {
        public int KasiyerNo { get; set; }
        public string? KasiyerAdi { get; set; }
        public string? Sifre { get; set; }
    }

    public class UnifiedBelgeParamsDto
    {
        public int EcrZNo { get; set; }
        public int EcrBelgeNo { get; set; }
        public long Id { get; set; }
        public string? BelgeNo { get; set; }
        public string? KasiyerAdi { get; set; }
        public int SaleType { get; set; }
        public int InvoiceType { get; set; }
        public int CustomerNoType { get; set; }
        public string? InvoiceNo { get; set; }
        public string? CustomerNo { get; set; }
        public string? CustomerName { get; set; }
        public bool IrsaliyeYerineGecer { get; set; }
        public int SlipCount { get; set; }
        public string? KatkiPayiTCNo { get; set; }
        public decimal KatkiPayiTutari { get; set; }
        public int KatkiPayiNo { get; set; }
        public decimal SatisToplam { get; set; }
        public decimal Toplam { get; set; }
        public decimal OdemeToplami { get; set; }
        public List<UnifiedAciklamaDto> BelgeSabitAltBilgisi { get; set; } = new();
        public List<UnifiedBelgeDetayDto> BelgeDetay { get; set; } = new();
        public List<UnifiedOdemeDetayDto> OdemeDetay { get; set; } = new();
    }

    public class UnifiedAciklamaDto
    {
        public string? AciklamaBilgisi { get; set; }
        public uint PrintStyle { get; set; } = 4;
    }

    public class UnifiedBelgeDetayDto
    {
        public int Id { get; set; }
        public string? UrunAdi { get; set; }
        public string? Barkod { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal Adet { get; set; }
        public decimal IndirimOran { get; set; }
        public decimal IndirimTutar { get; set; }
        public int DepartmanNo { get; set; }
        public int Birim { get; set; }
        public int TaxPercent { get; set; }
    }

    public class UnifiedOdemeDetayDto
    {
        public int Id { get; set; }
        public int OdemeTipi { get; set; }
        public decimal OdemeTutar { get; set; }
        public int YazarkasaOdemeTipi { get; set; }
        public decimal ParaUstu { get; set; }
    }
}
