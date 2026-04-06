namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class BekoBasketRequestDto
    {
        public string BasketId { get; set; } = Guid.NewGuid().ToString();
        public int DocumentType { get; set; }
        public int TaxFreeAmount { get; set; }
        public bool CreateInvoice { get; set; }
        public bool IsWayBill { get; set; }
        public string? Note { get; set; }
        public List<BekoBasketItemDto> Items { get; set; } = new();
        public List<BekoPaymentRequestDto> PaymentItems { get; set; } = new();
    }

    public class BekoBasketItemDto
    {
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PluNo { get; set; }
        public int Price { get; set; }
        public int SectionNo { get; set; }
        public int TaxPercent { get; set; }
        public int Type { get; set; }
        public string? Unit { get; set; }
        public int VatId { get; set; }
        public int Limit { get; set; }
        public int Quantity { get; set; }
    }
}