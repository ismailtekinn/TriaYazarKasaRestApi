namespace TriaYazarKasaRestApi.Business.Interfaces
{
    public interface IAutoConnectionStore
    {
        Guid? HuginConnectionId { get; }
        Guid? BekoConnectionId { get; }

        void SetHugin(Guid? connectionId);
        void SetBeko(Guid? connectionId);
    }
}