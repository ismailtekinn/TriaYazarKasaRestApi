using System;
using System.Threading;
using TriaYazarKasaRestApi.Business.Interfaces;
namespace TriaYazarKasaRestApi.Business.Models
{
    public class HuginActiveConnection
    {
        public Guid ConnectionId { get; set; }
        public DateTime ConnectedAt { get; set; }
        public IHuginAdapter Adapter { get; set; }
        public SemaphoreSlim Semaphore { get; set; } = new SemaphoreSlim(1, 1);
    }
}