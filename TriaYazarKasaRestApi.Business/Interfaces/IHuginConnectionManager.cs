using System;
using System.Collections.Generic;
using TriaYazarKasaRestApi.Business.Models;

namespace TriaYazarKasaRestApi.Business.Interfaces
{
    public interface IHuginConnectionManager
    {
        void Add(HuginActiveConnection connection);
        HuginActiveConnection Get(Guid connectionId);
        IReadOnlyCollection<HuginActiveConnection> GetAll();
        bool Remove(Guid connectionId);
    }
}