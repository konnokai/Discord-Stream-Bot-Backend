using Discord_Stream_Bot_Backend.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Stream_Bot_Backend.Services
{
    public class RedisService
    {

        public List<string> NowRecordList { get; private set; } = new List<string>();
        public ConnectionMultiplexer Redis { get; set; }
        public ISubscriber RedisSub { get; set; }
        public IDatabase RedisDb { get; set; }

        private readonly ILogger<RedisService> _logger;
        private readonly BlockingCollection<KeyValuePair<string, string>> _messageQueue = new(1);
        private readonly ConcurrentDictionary<string, KeyValuePair<string, string>> _needRePublishMessageList = new();

        private readonly IConfiguration _configuration;
        private readonly Timer _timer;

        public RedisService(ILogger<RedisService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            try
            {
                RedisConnection.Init(_configuration["RedisConnectOption"]);
                Redis = RedisConnection.Instance.ConnectionMultiplexer;
                RedisDb = Redis.GetDatabase(1);
                RedisSub = Redis.GetSubscriber();

                _logger.LogInformation("Redis已連線");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Redis 連線錯誤，請確認伺服器是否已開啟");
                throw;
            }

            _timer = new Timer((obj) => RefreshNowRecordList(), null, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(20));

            _ = Task.Run(() =>
            {
                while (!_messageQueue.IsCompleted)
                {
                    KeyValuePair<string, string>? message = null;
                    try
                    {
                        message = _messageQueue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogInformation("Adding was completed!");
                        break;
                    }

                    if (message != null)
                    {
                        try
                        {
                            if (RedisSub.Publish(new RedisChannel(message.Value.Key, RedisChannel.PatternMode.Literal), message.Value.Value) >= 1)
                            {
                                if (_needRePublishMessageList.Any())
                                {
                                    _logger.LogWarning("已可重新發送通知訊息");

                                    // 有可能是編輯觸發到重新發送，所以要先從清單內移除該編輯影片
                                    if (message.Value.Key.StartsWith("youtube.pubsub"))
                                    {
                                        var youtubeData = JsonConvert.DeserializeObject<YoutubePubSubNotification>(message.Value.Value);
                                        _needRePublishMessageList.TryRemove(youtubeData.VideoId, out _);
                                    }

                                    foreach (var item in _needRePublishMessageList.Values)
                                    {
                                        AddPubMessage(item.Key, item.Value);
                                    }

                                    _logger.LogInformation("已重新發送全部通知訊息");
                                    _needRePublishMessageList.Clear();
                                }
                            }
                            else
                            {
                                string saveKey = message.Value.GetHashCode().ToString();
                                if (message.Value.Key.StartsWith("youtube.pubsub"))
                                {
                                    var youtubeData = JsonConvert.DeserializeObject<YoutubePubSubNotification>(message.Value.Value);
                                    saveKey = youtubeData.VideoId;
                                }

                                _needRePublishMessageList.AddOrUpdate(saveKey,
                                    message.Value,
                                    (type, list) => message.Value);

                                _logger.LogWarning("通知訊息發送失敗，儲存到清單待命 | Channel: \"{channel}\" | Message: \"{message}\"", message.Value.Key, message.Value.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "通知訊息發送錯誤 | Channel: \"{channel}\" | Message: \"{message}\"", message.Value.Key, message.Value.Value);
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            RedisSub.UnsubscribeAll();
            Redis.Dispose();

            _timer.Change(Timeout.Infinite, 0);
            _timer.Dispose();
        }

        public void AddPubMessage(string channel, string msg)
        {
            _messageQueue.TryAdd(new KeyValuePair<string, string>(channel, msg));
        }

        public void AddYouTubePubMessage(YoutubePubSubNotification youtubeNotification)
        {
            _messageQueue.TryAdd(new KeyValuePair<string, string>(GetRedisChannelName(youtubeNotification.NotificationType), JsonConvert.SerializeObject(youtubeNotification)));
        }

        private static string GetRedisChannelName(YoutubePubSubNotification.YTNotificationType notificationType)
            => notificationType == YoutubePubSubNotification.YTNotificationType.CreateOrUpdated ? "youtube.pubsub.CreateOrUpdate" : "youtube.pubsub.Deleted";

        private void RefreshNowRecordList()
        {
            try
            {
                var newNowRecordList = Redis.GetDatabase(0).SetMembers("youtube.nowRecord").Select((x) => x.ToString()).ToList();
                if (newNowRecordList.Any())
                {
                    NowRecordList.Clear();
                    NowRecordList.AddRange(newNowRecordList);
                }

                _logger.LogInformation("重整現正直播的清單: {NowRecordList.Count} 個直播", NowRecordList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "現正直播清單重整失敗");
                NowRecordList = new List<string>();
            }
        }
    }
}
