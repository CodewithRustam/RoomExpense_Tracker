namespace AppExpenseTrackerApi.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/health")]
    [ApiController]
    public class ServerHealthController : ControllerBase
    {
        [HttpGet]
        [HttpHead]
        public IActionResult Get()
        {
            return Ok("Alive");
        }
    }
}
