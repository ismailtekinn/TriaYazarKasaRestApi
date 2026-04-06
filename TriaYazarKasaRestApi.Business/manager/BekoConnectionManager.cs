using System.Collections.Concurrent;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.Models;

namespace TriaYazarKasaRestApi.Business.manager
{
    public class BekoConnectionManager : IBekoConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, BekoActiveConnection> _connections = new();

        public void Add(BekoActiveConnection connection) => _connections[connection.ConnectionId] = connection;
        public BekoActiveConnection? Get(Guid connectionId) => _connections.TryGetValue(connectionId, out var c) ? c : null;
        public bool Remove(Guid connectionId) => _connections.TryRemove(connectionId, out _);
    }
}