using TriaYazarKasaRestApi.Business.Models;

namespace TriaYazarKasaRestApi.Business.Interfaces
{
    public interface IBekoConnectionManager
    {
        void Add(BekoActiveConnection connection);
        BekoActiveConnection? Get(Guid connectionId);
        bool Remove(Guid connectionId);
    }
}