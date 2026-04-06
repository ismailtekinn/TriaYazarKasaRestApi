using TriaYazarKasaRestApi.Business.Interfaces;

namespace TriaYazarKasaRestApi.Business.manager
{
    public class AutoConnectionStore : IAutoConnectionStore
    {
        private Guid? _huginConnectionId;
        private Guid? _bekoConnectionId;

        public Guid? HuginConnectionId => _huginConnectionId;
        public Guid? BekoConnectionId => _bekoConnectionId;

        public void SetHugin(Guid? connectionId)
        {
            _huginConnectionId = connectionId;
        }

        public void SetBeko(Guid? connectionId)
        {
            _bekoConnectionId = connectionId;
        }
    }
}