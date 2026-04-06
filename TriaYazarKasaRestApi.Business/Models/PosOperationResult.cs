namespace TriaYazarKasaRestApi.Business.Models
{
    public class PosOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public int? ResultCode { get; set; }

        public static PosOperationResult Ok(string message, object data = null, int? resultCode = 0)
        {
            return new PosOperationResult
            {
                Success = true,
                Message = message,
                Data = data,
                ResultCode = resultCode
            };
        }

        public static PosOperationResult Fail(string message, int? resultCode = null)
        {
            return new PosOperationResult
            {
                Success = false,
                Message = message,
                ResultCode = resultCode
            };
        }
    }
}