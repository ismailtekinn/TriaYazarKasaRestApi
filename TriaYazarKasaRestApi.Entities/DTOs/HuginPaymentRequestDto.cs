namespace TriaYazarKasaRestApi.Entities.DTOs
{
    
        public class HuginCashPaymentRequestDto
    {
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    public class HuginCardPaymentRequestDto
    {
        public decimal Amount { get; set; }
        public int Installment { get; set; }
        public string? CardNumber { get; set; }
    }
}