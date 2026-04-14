using System.Collections.Concurrent;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.manager
{
    public class BekoBasketOperationStore : IBekoBasketOperationStore
    {
        private readonly ConcurrentDictionary<string, BekoBasketOperationStatusDto> _operations = new(StringComparer.OrdinalIgnoreCase);

        public void Set(BekoBasketOperationStatusDto operation)
        {
            _operations[operation.BasketId] = operation;
        }

        public BekoBasketOperationStatusDto? Get(string basketId)
        {
            return _operations.TryGetValue(basketId, out var operation) ? operation : null;
        }

        public BekoBasketOperationStatusDto AddOrUpdate(string basketId, Func<string, BekoBasketOperationStatusDto> addValueFactory, Func<string, BekoBasketOperationStatusDto, BekoBasketOperationStatusDto> updateValueFactory)
        {
            return _operations.AddOrUpdate(basketId, addValueFactory, updateValueFactory);
        }
    }
}
