using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Business.Models;

namespace TriaYazarKasaRestApi.Business.manager
{
    public class HuginConnectionManager : IHuginConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, HuginActiveConnection> _connections = new();

        public void Add(HuginActiveConnection connection)
        {
            _connections[connection.ConnectionId] = connection;
        }

        public HuginActiveConnection Get(Guid connectionId)
        {
            _connections.TryGetValue(connectionId, out var connection);
            return connection;
        }

        public IReadOnlyCollection<HuginActiveConnection> GetAll()
        {
            return _connections.Values.ToList().AsReadOnly();
        }

        public bool Remove(Guid connectionId)
        {
            return _connections.TryRemove(connectionId, out _);
        }
    }
}