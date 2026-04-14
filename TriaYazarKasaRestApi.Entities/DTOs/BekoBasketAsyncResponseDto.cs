namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class BekoBasketAsyncResponseDto
    {
        public string BasketId { get; set; } = string.Empty;
        public string OperationId { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
