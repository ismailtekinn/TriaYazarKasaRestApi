using System.Text.Json;

namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class BekoDeviceInfoDto
    {
        public bool IsConnected { get; set; }
        public int? ActiveDeviceIndex { get; set; }
        public string? SelectedDeviceId { get; set; }
        public string? LastConnectedDeviceId { get; set; }
        public DateTime? LastCallbackAtUtc { get; set; }
        public JsonElement FiscalInfo { get; set; }
    }
}
