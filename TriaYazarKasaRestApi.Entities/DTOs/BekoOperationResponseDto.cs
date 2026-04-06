namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class BekoOperationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public int? ErrorCode { get; set; }
    }
}