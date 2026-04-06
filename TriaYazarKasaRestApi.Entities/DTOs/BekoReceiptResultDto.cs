namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class BekoReceiptResultDto
    {
        public string? BasketId { get; set; }
        public int Status { get; set; }
        public string? Message { get; set; }
        public int? ReceiptNo { get; set; }
        public int? ZNo { get; set; }
        public string? Uuid { get; set; }
        public List<BekoReceiptPaymentDto> Payments { get; set; } = new();
    }

    public class BekoReceiptPaymentDto
    {
        public int Type { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public int BatchNo { get; set; }
        public int TxnNo { get; set; }
        public int OperatorId { get; set; }
        public string? RefundInfo { get; set; }
    }
}