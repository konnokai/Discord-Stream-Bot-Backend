using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace Discord_Stream_Bot_Backend.Services
{
    // https://github.com/TwitchLib/TwitchLib.EventSub.Webhooks/blob/main/TwitchLib.EventSub.Webhooks.Example/EventSubHostedService.cs
    public class EventSubHostedService : IHostedService
    {
        private readonly ILogger<EventSubHostedService> _logger;
        private readonly IEventSubWebhooks _eventSubWebhooks;

        public EventSubHostedService(ILogger<EventSubHostedService> logger, IEventSubWebhooks eventSubWebhooks)
        {
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.OnError += OnError;
            _eventSubWebhooks.OnStreamOffline += _eventSubWebhooks_OnStreamOffline;
            _eventSubWebhooks.OnChannelUpdate += _eventSubWebhooks_OnChannelUpdate;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.OnError += OnError;
            _eventSubWebhooks.OnStreamOffline -= _eventSubWebhooks_OnStreamOffline;
            _eventSubWebhooks.OnChannelUpdate -= _eventSubWebhooks_OnChannelUpdate;
            return Task.CompletedTask;
        }

        private void OnError(object sender, OnErrorArgs e)
        {
            _logger.LogError("Twitch 錯誤，原因: {Reason} - 訊息: {Message}", e.Reason, e.Message);
        }

        private void _eventSubWebhooks_OnStreamOffline(object sender, TwitchLib.EventSub.Webhooks.Core.EventArgs.Stream.StreamOfflineArgs e)
        {
            _logger.LogInformation("Twitch 直播已離線: {UserName} ({UserId})", e.Notification.Event.BroadcasterUserName, e.Notification.Event.BroadcasterUserId);
            Utility.RedisSub.Publish(new("twitch:stream_offline", StackExchange.Redis.RedisChannel.PatternMode.Literal), e.Notification.Event.BroadcasterUserId);
        }

        private void _eventSubWebhooks_OnChannelUpdate(object sender, ChannelUpdateArgs e)
        {
            _logger.LogInformation("Twitch 頻道狀態更新: {UserName} - {Titlie} ({CategoryName})",
                e.Notification.Event.BroadcasterUserName,
                e.Notification.Event.Title,
                e.Notification.Event.CategoryName);

            Utility.RedisSub.Publish(new("twitch:channel_update", StackExchange.Redis.RedisChannel.PatternMode.Literal), JsonConvert.SerializeObject(e.Notification.Event));
        }
    }
}