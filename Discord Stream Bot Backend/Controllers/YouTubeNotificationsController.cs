using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Discord_Stream_Bot_Backend.Controllers
{
    // https://www.codeproject.com/Tips/1229912/Push-Notification-PubSubHubBub-from-Youtube-to-Csh
    [Route("[action]")]
    [ApiController]
    public class YouTubeNotificationsController : ControllerBase
    {
        private readonly ILogger<YouTubeNotificationsController> _logger;
        private readonly ConcurrentDictionary<YoutubePubSubNotification.YTNotificationType, List<YoutubePubSubNotification>> _needRePublishVideoList = new();
        public YouTubeNotificationsController(ILogger<YouTubeNotificationsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [HttpPost]
        public ContentResult NotificationCallback([FromQuery(Name = "hub.topic")] string topic,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.verify_token")] string verifyToken,
            [FromQuery(Name = "hub.lease_seconds")] string leaseSeconds)
        {
            try
            {
                if (!Request.Headers.TryGetValue("User-Agent", out var userAgent) || !userAgent.ToString().StartsWith("FeedFetcher-Google;"))
                {
                    _logger.LogWarning("無 User-Agent 或標頭無效，略過處理");
                    return Content("400");
                }

                if (Request.Method == "POST")
                {
                    try
                    {
                        if (!Request.Headers.TryGetValue("X-Hub-Signature", out var signature) || !signature.ToString().Contains('='))
                        {
                            _logger.LogWarning("無 X-Hub-Signature 或標頭無效，略過處理");
                            return Content("400");
                        }

                        if (!Request.Headers.TryGetValue("Content-Type", out var contentType) || contentType != "application/atom+xml")
                        {
                            _logger.LogWarning("無 Content-Type 或標頭無效，略過處理");
                            return Content("400");
                        }

                        var data = ConvertAtomToClass(Request.Body, signature.ToString().Split(new char[] { '=' })[1]);
                        if (data == null)
                            return Content("400");

                        _logger.LogInformation("{Data}", data.ToString());
                        return Content("200");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"NotificationCallback-ConvertAtomToClass 錯誤\n");
                        return Content("500");
                    }
                }
                else if (Request.Method == "GET")
                {
                    _logger.LogInformation("New Callback!\n"
                        + "topic: {topic}\n"
                        + "challenge: {challenge}\n"
                        + "mode: {mode}\n"
                        + "verifyToken: {verifyToken}\n"
                        + "leaseSeconds: {leaseSeconds}",
                        topic, challenge, mode, verifyToken, leaseSeconds);
                    switch (mode)
                    {
                        case "subscribe":
                            if (!string.IsNullOrEmpty(verifyToken))
                            {
                                try
                                {
                                    string channelId = new Regex(@"channel_id=(?'ChannelId'[\w\-\\_]{24})").Match(topic).Groups["ChannelId"].Value;
                                    if (string.IsNullOrEmpty(channelId))
                                        return Content("400");

                                    Utility.RedisDb.StringSet($"youtube.pubsub.HMACSecret:{channelId}", verifyToken, TimeSpan.FromSeconds(int.Parse(leaseSeconds)));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "設定 VerifyToken 錯誤");
                                    return Content("400");
                                }
                            }
                            return Content(challenge);
                        default:
                            _logger.LogWarning("NotificationCallback 錯誤，未知的 mode: {Mode}", mode);
                            return Content("400");
                    }
                }
                else
                {
                    _logger.LogWarning($"NotificationCallback 錯誤，未知的標頭");
                    return Content("400");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationCallback 錯誤\n");
                return Content("500");
            }
        }

        //https://ithelp.ithome.com.tw/articles/10186605
        private YoutubePubSubNotification ConvertAtomToClass(Stream stream, string signature)
        {
            try
            {
                var youtubeNotification = new YoutubePubSubNotification();
                string xmlText = new StreamReader(stream).ReadToEnd();
                XmlDocument doc = new();
                doc.LoadXml(xmlText);

                if (xmlText.Contains("https://www.youtube.com/xml/feeds/videos.xml?channel_id"))
                {
                    youtubeNotification.VideoId = doc.GetElementsByTagName("yt:videoId")[0]?.InnerText;
                    youtubeNotification.ChannelId = doc.GetElementsByTagName("yt:channelId")[0]?.InnerText;
                    youtubeNotification.Title = doc.GetElementsByTagName("title")[1]?.InnerText;
                    youtubeNotification.Link = doc.GetElementsByTagName("link")[0]?.Attributes["href"].Value;
                    youtubeNotification.Published = (DateTime)(doc.GetElementsByTagName("published")[0]?.InnerText.ConvertDateTime());
                    youtubeNotification.Updated = (DateTime)(doc.GetElementsByTagName("updated")[0]?.InnerText.ConvertDateTime());
                    youtubeNotification.NotificationType = YoutubePubSubNotification.YTNotificationType.CreateOrUpdated;
                }
                else if (xmlText.Contains("deleted-entry"))
                {
                    var node = doc.GetElementsByTagName("at:deleted-entry")[0];
                    if (node == null)
                    {
                        _logger.LogWarning($"無 at:deleted-entry 節點");
                        return null;
                    }

                    youtubeNotification.VideoId = node.Attributes.GetNamedItem("ref").Value.Split(new char[] { ':' })[2];
                    youtubeNotification.ChannelId = doc.GetElementsByTagName("uri")[0]?.InnerText;
                    youtubeNotification.ChannelId = youtubeNotification.ChannelId.Substring(youtubeNotification.ChannelId.Length - 24, 24);
                    youtubeNotification.Published = (DateTime)node.Attributes.GetNamedItem("when").Value.ConvertDateTime();
                    youtubeNotification.NotificationType = YoutubePubSubNotification.YTNotificationType.Deleted;
                }
                else
                {
                    _logger.LogWarning("未知的 Atom");
                    _logger.LogWarning("{Atom}", xmlText);
                    return null;
                }

                if (!Utility.RedisDb.KeyExists($"youtube.pubsub.HMACSecret:{youtubeNotification.ChannelId}"))
                {
                    _logger.LogWarning("Redis 無 {YoutubeChannelId} 的 HMACSecret 值", youtubeNotification.ChannelId);
                    Utility.RedisSub.Publish(new RedisChannel("youtube.pubsub.NeedRegister", RedisChannel.PatternMode.Literal), youtubeNotification.ChannelId);
                    return null;
                }

                string HMACsecret = Utility.RedisDb.StringGet($"youtube.pubsub.HMACSecret:{youtubeNotification.ChannelId}");
                string HMACSHA1 = ConvertToHexadecimal(SignWithHmac(xmlText, HMACsecret));
                if (HMACSHA1 != signature)
                {
                    _logger.LogWarning("HMACSHA1 比對失敗: {HMACSHA1} vs {Signature}", HMACSHA1, signature);
                    Utility.RedisSub.Publish(new RedisChannel("youtube.pubsub.NeedRegister", RedisChannel.PatternMode.Literal), youtubeNotification.ChannelId);
                    return null;
                }

                if (Utility.RedisSub.Publish(new RedisChannel(GetRedisChannelName(youtubeNotification.NotificationType), RedisChannel.PatternMode.Literal), JsonConvert.SerializeObject(youtubeNotification)) >= 1)
                {
                    if (_needRePublishVideoList.Any())
                    {
                        _logger.LogWarning("已可重新發送通知訊息");

                        foreach (var item in _needRePublishVideoList)
                        {
                            foreach (var video in item.Value)
                            {
                                _logger.LogInformation("重新發送通知訊息: {VideoId}", video.VideoId);
                                Utility.RedisSub.Publish(new RedisChannel(GetRedisChannelName(video.NotificationType), RedisChannel.PatternMode.Literal), JsonConvert.SerializeObject(video), CommandFlags.FireAndForget);
                            }
                        }

                        _logger.LogInformation("已重新發送全部通知訊息");
                        _needRePublishVideoList.Clear();
                    }
                }
                else
                {
                    _needRePublishVideoList.AddOrUpdate(youtubeNotification.NotificationType,
                        new List<YoutubePubSubNotification>() { youtubeNotification },
                        (type, list) =>
                        {
                            if (!list.Any((x) => x.VideoId == youtubeNotification.VideoId))
                            {
                                list.Add(youtubeNotification);
                            }

                            return list;
                        });

                    _logger.LogWarning("通知訊息發送失敗，儲存到清單待命: {VideoId}", youtubeNotification.VideoId);
                }

                return youtubeNotification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertAtomToClass 錯誤\n");
                return null;
            }
        }

        private static string GetRedisChannelName(YoutubePubSubNotification.YTNotificationType notificationType)
            => notificationType == YoutubePubSubNotification.YTNotificationType.CreateOrUpdated ? "youtube.pubsub.CreateOrUpdate" : "youtube.pubsub.Deleted";

        //https://stackoverflow.com/questions/4390543/facebook-real-time-update-validating-x-hub-signature-sha1-signature-in-c-sharp
        private static byte[] SignWithHmac(string dataToSign, string keyBody)
        {
            byte[] key = Encoding.UTF8.GetBytes(keyBody);
            byte[] data = Encoding.UTF8.GetBytes(dataToSign);
            using var hmacAlgorithm = new HMACSHA1(key);
            return hmacAlgorithm.ComputeHash(data);
        }

        //https://stackoverflow.com/questions/4390543/facebook-real-time-update-validating-x-hub-signature-sha1-signature-in-c-sharp
        private static string ConvertToHexadecimal(IEnumerable<byte> bytes)
        {
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }

    public class YoutubePubSubNotification
    {
        public enum YTNotificationType { CreateOrUpdated, Deleted }

        public YTNotificationType NotificationType { get; set; } = YTNotificationType.CreateOrUpdated;
        public string VideoId { get; set; }
        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public DateTime Published { get; set; }
        public DateTime Updated { get; set; }

        public override string ToString()
        {
            switch (NotificationType)
            {
                case YTNotificationType.CreateOrUpdated:
                    return $"({NotificationType} at {Updated}) {ChannelId} - {VideoId} | {Title}";
                case YTNotificationType.Deleted:
                    return $"({NotificationType} at {Published}) {ChannelId} - {VideoId}";
                default:
                    break;
            }
            return "";
        }
    }

    public static class Ext
    {
        public static DateTime? ConvertDateTime(this string text)
        {
            try
            {
                return Convert.ToDateTime(text);
            }
            catch
            {
                return new DateTime();
            }
        }
    }
}