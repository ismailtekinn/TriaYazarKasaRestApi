using Microsoft.AspNetCore.Mvc;
using TriaYazarKasaRestApi.Business.Interfaces;
using TriaYazarKasaRestApi.Entities.DTOs;

namespace TriaYazarKasaRestApiWebApi.Controllers
{
    [ApiController]
    [Route("api/unified-sale")]
    public class UnifiedSaleController : ControllerBase
    {
        private readonly IUnifiedSaleService _unifiedSaleService;

        public UnifiedSaleController(IUnifiedSaleService unifiedSaleService)
        {
            _unifiedSaleService = unifiedSaleService;
        }

        [HttpPost("{deviceType}")]
        public async Task<ActionResult<ApiResponseDto<object>>> Send(
            string deviceType,
            [FromBody] UnifiedSaleRequestDto request)
        {
            var result = await _unifiedSaleService.ExecuteAsync(deviceType, request);
            return Ok(ApiResponseDto<object>.Ok(result, "OK"));
        }
    }
}
