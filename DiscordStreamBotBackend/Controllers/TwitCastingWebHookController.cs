using DiscordStreamBotBackend.Model.TwitCasting;
using DiscordStreamBotBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;

namespace DiscordStreamBotBackend.Controllers
{
    [Route("[action]")]
    [ApiController]
    public class TwitCastingWebHookController : Controller
    {
        private readonly ILogger<TwitCastingWebHookController> _logger;
        private readonly IConfiguration _configuration;
        private readonly RedisService _redisService;

        public TwitCastingWebHookController(ILogger<TwitCastingWebHookController> logger, IConfiguration configuration, RedisService redisService)
        {
            _logger = logger;
            _configuration = configuration;
            _redisService = redisService;

            if (string.IsNullOrEmpty(_configuration["TwitCasting:WebHookSignature"]))
            {
                _logger.LogError("TwitCasting WebHook Signature is not set in configuration! Please set it in appsettings.json or environment variables.");
            }
        }

        [HttpPost]
        public ContentResult TwitCastingWebHook()
        {
            try
            {
                var content = new StreamReader(Request.Body).ReadToEnd();
                var webHookJson = JsonConvert.DeserializeObject<TwitCastingWebHookJson>(content);

                if (webHookJson == null)
                {
                    _logger.LogError("TwitCasting WebHook Is Null!");
                    return new ContentResult { StatusCode = 400 };
                }

                if (webHookJson.Signature != _configuration["TwitCasting:WebHookSignature"])
                {
                    _logger.LogError("Invalid Signature from TwitCasting WebHook!");
                    return new ContentResult { StatusCode = 401 };
                }

                _logger.LogInformation("接收到 TwitCasting WebHook 資料: (Live: {IsLive}) {ChannelName} - {Title}",
                    webHookJson.Movie.IsLive, webHookJson.Broadcaster.Name, webHookJson.Movie.Title);

                if (webHookJson.Movie.IsLive)
                {
                    _redisService.AddPubMessage("twitcasting.pubsub.startlive", content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Read TwitCasting WebHook Error");
                return new ContentResult { StatusCode = 500 };
            }

            return new ContentResult { StatusCode = 204 };
        }
    }
}
