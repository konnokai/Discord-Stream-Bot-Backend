using Discord;
using Discord.Webhook;
using Discord_Stream_Bot_Backend.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Discord_Stream_Bot_Backend.Controllers
{
    [Route("[action]")]
    [ApiController]
    public class BililiveRecorderWebHookController : Controller
    {
        private readonly ILogger<BililiveRecorderWebHookController> _logger;

        public BililiveRecorderWebHookController(ILogger<BililiveRecorderWebHookController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<ContentResult> BililiveRecorderCallback()
        {
            try
            {
                var content = new StreamReader(Request.Body).ReadToEnd();
                var webHookJson = JsonConvert.DeserializeObject<BililiveRecorderWebHookJson>(content);

                if (webHookJson == null)
                {
                    _logger.LogError("Read Bililive Recorder Null");
                    return new ContentResult { StatusCode = 500 };
                }

                _logger.LogInformation("接收到 Bililive 資料: ({Type}) {ChannelName} - {Title}",
                    webHookJson.EventType, webHookJson.EventData.Name, webHookJson.EventData.Title);

                if (webHookJson.EventType == "StreamStarted")
                {
                    await SendMessageToDiscordAsync(webHookJson.EventData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Read Bililive Recorder Data Error");
                return new ContentResult { StatusCode = 500 };
            }

            return new ContentResult { StatusCode = 204 };
        }

        private async Task SendMessageToDiscordAsync(BililiveRecorderWebHookEventData eventData)
        {
            if (string.IsNullOrEmpty(Utility.ServerConfig.BililiveWebHookUrl))
                return;

            try
            {
                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithColor(40, 40, 40)
                    .WithTitle(eventData.Title)
                    .WithDescription(eventData.Name)
                    .WithUrl($"https://live.bilibili.com/{eventData.RoomId}")
                    .AddField("直播狀態", "開台中", true)
                    .AddField("分類", eventData.AreaNameParent, true)
                    .AddField("子分類", eventData.AreaNameChild, true);

                var discordWebhookClient = new DiscordWebhookClient(Utility.ServerConfig.BililiveWebHookUrl);
                await discordWebhookClient.SendMessageAsync(embeds: new List<Embed>() { embedBuilder.Build() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discord WebHook 發送失敗");
            }
        }
    }
}
