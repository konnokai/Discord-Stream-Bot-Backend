using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace DiscordStreamBotBackend.Controllers
{
    [Route("/")]
    [ApiController]
    public class IndexController : Controller
    {
        [EnableCors("allowGET")]
        [HttpGet]
        public IActionResult Index()
        {
            return Redirect("https://dcbot.konnokai.me/stream");
        }
    }
}
