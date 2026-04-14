using Microsoft.AspNetCore.Mvc;

namespace TriaYazarKasaRestApiWebApi.Controllers
{
    [ApiController]
    [Route("api/beko/debug")]
    public class BekoDebugController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BekoDebugController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("usb-devices")]
        public async Task<IActionResult> ListUsbDevices()
        {
            var client = _httpClientFactory.CreateClient("BekoWorker");
            var response = await client.GetAsync("api/beko/debug/usb-devices");
            var body = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }
    }
}
