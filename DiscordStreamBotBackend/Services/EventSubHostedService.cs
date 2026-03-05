using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.EventArgs.Stream;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;

namespace DiscordStreamBotBackend.Services
{
    // https://github.com/TwitchLib/TwitchLib.EventSub.Webhooks/blob/main/TwitchLib.EventSub.Webhooks.Example/EventSubHostedService.cs
    public class EventSubHostedService : IHostedService
    {
        private readonly ILogger<EventSubHostedService> _logger;
        private readonly IEventSubWebhooks _eventSubWebhooks;
        private readonly RedisService _redisService;

        public EventSubHostedService(ILogger<EventSubHostedService> logger, IEventSubWebhooks eventSubWebhooks, RedisService redisService)
        {
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
            _redisService = redisService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.Error += OnError;
            _eventSubWebhooks.StreamOffline += _eventSubWebhooks_OnStreamOffline;
            _eventSubWebhooks.ChannelUpdate += _eventSubWebhooks_OnChannelUpdate;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.Error -= OnError;
            _eventSubWebhooks.StreamOffline -= _eventSubWebhooks_OnStreamOffline;
            _eventSubWebhooks.ChannelUpdate -= _eventSubWebhooks_OnChannelUpdate;
            return Task.CompletedTask;
        }

        private Task OnError(object sender, OnErrorArgs e)
        {
            _logger.LogError("Twitch 錯誤，原因: {Reason} - 訊息: {Message}", e.Reason, e.Message);
            return Task.CompletedTask;
        }

        private Task _eventSubWebhooks_OnStreamOffline(object sender, StreamOfflineArgs e)
        {
            _logger.LogInformation("Twitch 直播已離線: {UserName} ({UserId})", e.Payload.Event.BroadcasterUserName, e.Payload.Event.BroadcasterUserId);
            _redisService.AddPubMessage("twitch:stream_offline", JsonConvert.SerializeObject(e.Payload.Event));

            return Task.CompletedTask;
        }

        private Task _eventSubWebhooks_OnChannelUpdate(object sender, ChannelUpdateArgs e)
        {
            _logger.LogInformation("Twitch 頻道狀態更新: {UserName} - {Titlie} ({CategoryName})",
                e.Payload.Event.BroadcasterUserName,
                e.Payload.Event.Title,
                e.Payload.Event.CategoryName);

            _redisService.AddPubMessage("twitch:channel_update", JsonConvert.SerializeObject(e.Payload.Event));

            return Task.CompletedTask;
        }
    }
}