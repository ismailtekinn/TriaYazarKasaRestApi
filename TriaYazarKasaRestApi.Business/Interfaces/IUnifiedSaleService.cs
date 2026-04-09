using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApi.Business.Interfaces
{
    public interface IUnifiedSaleService
    {
        Task<object> ExecuteAsync(string deviceType, UnifiedSaleRequestDto request, Guid? connectionId = null);
    }
}
