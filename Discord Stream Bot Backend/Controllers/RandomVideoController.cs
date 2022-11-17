using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord_Stream_Bot_Backend.Controllers
{
    [Route("[action]")]
    public class RandomVideoController : Controller
    {
        [EnableCors("allowGET")]
        [HttpGet]
        public RedirectResult RandomVideo()
        {
            List<string> randomVideoUrlList = new List<string>
            {
                "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                "https://www.youtube.com/watch?v=ST-Q-hX9Yzo",
                "https://www.youtube.com/watch?v=h-mUGj41hWA",
                "https://www.youtube.com/watch?v=BMvqvnyGtGo",
                "https://www.youtube.com/watch?v=0rLGxUxucdE",
                "https://www.youtube.com/watch?v=Z_VNp7VUtqA",
                "https://www.youtube.com/watch?v=uSvGR5H7lUk"
            };

            if (Utility.NowRecordList.Any())
                randomVideoUrlList.AddRange(Utility.NowRecordList.Select((x) => $"https://www.youtube.com/watch?v={x}"));

            return Redirect(randomVideoUrlList[new Random().Next(0, randomVideoUrlList.Count)]);
        }
    }
}
