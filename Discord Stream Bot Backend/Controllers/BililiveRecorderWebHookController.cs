using Discord;
using Discord.Webhook;
using Discord_Stream_Bot_Backend.Model;
using Discord_Stream_Bot_Backend.Model.BiliBili;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Discord_Stream_Bot_Backend.Controllers
{
    [Route("[action]")]
    [ApiController]
    public class BililiveRecorderWebHookController : Controller
    {
        private readonly ILogger<BililiveRecorderWebHookController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public BililiveRecorderWebHookController(ILogger<BililiveRecorderWebHookController> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
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
            if (string.IsNullOrEmpty(_configuration["BililiveWebHookUrl"]))
                return;

            try
            {
                var embedBuilder = new EmbedBuilder();
                embedBuilder.WithColor(0, 229, 132)
                   .WithTitle(eventData.Title)
                   .WithDescription(eventData.Name)
                   .WithUrl($"https://live.bilibili.com/{eventData.RoomId}")
                   .AddField("直播狀態", "開台中", true)
                   .AddField("分類", eventData.AreaNameParent, true)
                   .AddField("子分類", eventData.AreaNameChild, true);

                try
                {
                    // https://socialsisteryi.github.io/bilibili-API-collect/docs/live/info.html#%E8%8E%B7%E5%8F%96%E7%9B%B4%E6%92%AD%E9%97%B4%E4%BF%A1%E6%81%AF
                    var getRoomInfoJson = JsonConvert.DeserializeObject<BiliBiliGetRoomInfoJson>(await _httpClient.GetStringAsync($"https://api.live.bilibili.com/room/v1/Room/get_info?room_id={eventData.RoomId}"));

                    // https://socialsisteryi.github.io/bilibili-API-collect/docs/live/info.html#%E8%8E%B7%E5%8F%96%E4%B8%BB%E6%92%AD%E4%BF%A1%E6%81%AF
                    var getLiveUserJson = JsonConvert.DeserializeObject<BiliBiliGetLiveUserInfoJson>(await _httpClient.GetStringAsync($"https://api.live.bilibili.com/live_user/v1/Master/info?uid={getRoomInfoJson.Data.Uid}"));

                    embedBuilder.WithDescription(Format.Url(getLiveUserJson.Data.Info.Uname, $"https://space.bilibili.com/{getLiveUserJson.Data.Info.Uid}/"));
                    embedBuilder.WithImageUrl(getRoomInfoJson.Data.Keyframe);
                    embedBuilder.WithThumbnailUrl(getLiveUserJson.Data.Info.Face);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Get Bililive Info 失敗");
                }

                var discordWebhookClient = new DiscordWebhookClient(_configuration["BililiveWebHookUrl"]);
                await discordWebhookClient.SendMessageAsync(embeds: new List<Embed>() { embedBuilder.Build() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discord WebHook 發送失敗");
            }
        }
    }
}
