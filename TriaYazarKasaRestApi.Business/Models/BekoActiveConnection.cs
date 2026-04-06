using TriaYazarKasaRestApi.Business.Interfaces;

namespace TriaYazarKasaRestApi.Business.Models
{
    public class BekoActiveConnection
    {
    public Guid ConnectionId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public IBekoAdapter Adapter { get; set; } = default!;
    public SemaphoreSlim Semaphore { get; set; } = new(1, 1);
    }
}