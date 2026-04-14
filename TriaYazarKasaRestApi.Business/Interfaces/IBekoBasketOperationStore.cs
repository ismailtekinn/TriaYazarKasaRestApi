using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.Interfaces
{
    public interface IBekoBasketOperationStore
    {
        void Set(BekoBasketOperationStatusDto operation);
        BekoBasketOperationStatusDto? Get(string basketId);
        BekoBasketOperationStatusDto AddOrUpdate(string basketId, Func<string, BekoBasketOperationStatusDto> addValueFactory, Func<string, BekoBasketOperationStatusDto, BekoBasketOperationStatusDto> updateValueFactory);
    }
}
