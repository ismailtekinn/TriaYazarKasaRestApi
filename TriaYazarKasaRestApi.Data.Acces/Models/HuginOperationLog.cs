using System;

namespace TriaYazarKasaRestApi.Data.Acces.Models
{
    public class HuginOperationLog
    {
        public int Id { get; set; }
        public Guid ConnectionId { get; set; }
        public string OperationName { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}