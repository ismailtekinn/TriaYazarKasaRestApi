using Microsoft.AspNetCore.Mvc;
using TriaYazarKasaRestApi.Business.Interfaces;

namespace TriaYazarKasaRestApiWebApi.Controllers
{
    [ApiController]
    [Route("api/connections")]
    public class ConnectionController : ControllerBase
    {
        private readonly IAutoConnectionStore _autoConnectionStore;

        public ConnectionController(IAutoConnectionStore autoConnectionStore)
        {
            _autoConnectionStore = autoConnectionStore;
        }

        [HttpGet("auto")]
        public IActionResult GetAutoConnections()
        {
            return Ok(new
            {
                huginConnectionId = _autoConnectionStore.HuginConnectionId,
                bekoConnectionId = _autoConnectionStore.BekoConnectionId
            });
        }
    }
}
