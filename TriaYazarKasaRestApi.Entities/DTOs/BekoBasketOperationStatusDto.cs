namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class BekoBasketOperationStatusDto
    {
        public string BasketId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsFinal { get; set; }
        public string? SaleJson { get; set; }
        public BekoReceiptResultDto? ReceiptResult { get; set; }
    }
}
