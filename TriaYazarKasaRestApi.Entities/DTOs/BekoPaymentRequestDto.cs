namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class BekoPaymentRequestDto
    {
        public int Type { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
    }
}