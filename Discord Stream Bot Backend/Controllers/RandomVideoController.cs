using Discord_Stream_Bot_Backend.Auth;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Discord_Stream_Bot_Backend.Controllers
{
    [Route("[action]")]
    [ApiController]
    public class RandomVideoController : Controller
    {
        readonly Logger logger = LogManager.GetLogger("RngVideo");

        [EnableCors("allowGET")]
        [HttpGet]
        public async Task<RedirectResult> RandomVideo()
        {
            try
            {
                await Utility.RedisDb.StringIncrementAsync("discord_stream_bot:randomVideoClickCount");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Redis Increment Error\n");
            }

            try
            {
                List<string> randomVideoUrlList = new()
                {
                    "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                    "https://www.youtube.com/watch?v=ST-Q-hX9Yzo",
                    "https://www.youtube.com/watch?v=h-mUGj41hWA",
                    "https://www.youtube.com/watch?v=BMvqvnyGtGo",
                    "https://www.youtube.com/watch?v=0rLGxUxucdE",
                    "https://www.youtube.com/watch?v=Z_VNp7VUtqA",
                    "https://www.youtube.com/watch?v=uSvGR5H7lUk"
                };

                if (System.IO.File.Exists("RandomVideoUrl.txt"))
                {
                    string[] strings = System.IO.File.ReadAllLines("RandomVideoUrl.txt");
                    if (strings.Any())
                        randomVideoUrlList.AddRange(strings.Where((x) => !string.IsNullOrWhiteSpace(x)).Select((x) => x.Trim()));
                }

                if (Utility.NowRecordList.Any())
                    randomVideoUrlList.AddRange(Utility.NowRecordList.Select((x) => $"https://www.youtube.com/watch?v={x}"));

                var index = RNG.Next(randomVideoUrlList.Count);
                logger.Info($"randomVideoUrlList.Count: {randomVideoUrlList.Count}, RNG.Next: {index}");

                string randomUrl = randomVideoUrlList[Math.Max(0, Math.Min(randomVideoUrlList.Count - 1, index))];
                logger.Info(randomUrl);

                return Redirect(randomUrl);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "RandomVideo\n");
                return Redirect("https://dcbot.konnokai.me/stream");
            }
        }
    }
}