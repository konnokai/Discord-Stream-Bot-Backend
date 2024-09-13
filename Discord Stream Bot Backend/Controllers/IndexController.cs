using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Discord_Stream_Bot_Backend.Controllers
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
