namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class HuginConnectRequestDto
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string SerialPort { get; set; }
        public bool UseTcp { get; set; }
        public string FiscalId { get; set; }
    }
}
