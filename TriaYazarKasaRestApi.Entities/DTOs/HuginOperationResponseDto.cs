namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class HuginOperationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public int? ErrorCode { get; set; }
    }
}