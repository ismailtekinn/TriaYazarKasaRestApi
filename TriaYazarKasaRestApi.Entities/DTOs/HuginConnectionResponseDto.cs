using System;
using TriaYazarKasaRestApi.Entities.Enums;

namespace TriaYazarKasaRestApi.Entities.DTOs
{
    public class HuginConnectionResponseDto
    {
        public Guid ConnectionId { get; set; }
        public bool IsConnected { get; set; }
        public PosConnectionStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}
