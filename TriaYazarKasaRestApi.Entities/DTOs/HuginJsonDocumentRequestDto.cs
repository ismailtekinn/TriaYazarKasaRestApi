namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class HuginJsonDocumentRequestDto
    {
        public List<HuginJsonItemDto>? FiscalItems { get; set; }
        public List<HuginJsonPaymentDto>? Payments { get; set; }
        public List<string>? FooterNotes { get; set; }
        public HuginEndOfReceiptDto? EndOfReceiptInfo { get; set; }
        public HuginPharmacyInfoDto? PharmacyInfo { get; set; }
    }

    public class HuginJsonItemDto
    {
        public int Id { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Name { get; set; }
        public string? Barcode { get; set; }
        public int DeptId { get; set; }
        public int Status { get; set; }
        public HuginJsonAdjustmentDto? Adj { get; set; }
        public string? NoteLine1 { get; set; }
        public string? NoteLine2 { get; set; }
    }
    public class HuginJsonAdjustmentDto
    {
        public int Type { get; set; }
        public decimal Amount { get; set; }
        public int Percentage { get; set; }
        public string? NoteLine1 { get; set; }
        public string? NoteLine2 { get; set; }
    }

    public class HuginJsonPaymentDto
    {
        public int Type { get; set; }
        public int Index { get; set; }
        public decimal PaidTotal { get; set; }
        public bool ViaByEft { get; set; }
    }

    public class HuginEndOfReceiptDto
    {
        public bool CloseReceiptFlag { get; set; } = true;
        public bool BarcodeFlag { get; set; }
        public string? Barcode { get; set; }
    }

    public class HuginPharmacyInfoDto
    {
        public string? SSNNumber { get; set; }
        public decimal ContributionAmount { get; set; }
    }
}