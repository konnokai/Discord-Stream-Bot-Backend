using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Xml;
using Newtonsoft.Json;

namespace Discord_Stream_Bot_Backend.Controllers
{
    // https://www.codeproject.com/Tips/1229912/Push-Notification-PubSubHubBub-from-Youtube-to-Csh
    [Route("[action]")]
    [ApiController]
    public class YouTubeNotificationsController : ControllerBase
    {
        private readonly ILogger<YouTubeNotificationsController> _logger;
        public YouTubeNotificationsController(ILogger<YouTubeNotificationsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [HttpPost]
        public ContentResult NotificationCallback([FromQuery(Name = "hub.topic")] string topic, [FromQuery(Name = "hub.challenge")] string challenge, [FromQuery(Name = "hub.mode")] string mode, [FromQuery(Name = "hub.lease_seconds")] string leaseSeconds)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    try
                    {
                        var data = ConvertAtomToClass(Request.Body);
                        if (data == null)
                            return Content("400");

                        _logger.LogInformation(data.ToString());
                        return Content("200");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"NotificationCallback-ConvertAtomToSyndication錯誤\n");
                        return Content("500");
                    }
                }
                else if (Request.Method == "GET")
                {
                    _logger.LogInformation($"New Callback!\n" + $"topic: {topic}\n" + $"challenge: {challenge}\n" + $"mode: {mode}\n" + $"leaseSeconds: {leaseSeconds}");
                    switch (mode)
                    {
                        case "subscribe":
                            return Content(challenge);
                        default:
                            _logger.LogWarning($"NotificationCallback錯誤，未知的mode: {mode}");
                            return Content("400");
                    }
                }
                else
                {
                    _logger.LogWarning($"NotificationCallback錯誤，未知的標頭");
                    return Content("400");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationCallback錯誤\n");
                return Content("500");
            }
        }

        //https://ithelp.ithome.com.tw/articles/10186605
        private YoutubeNotification ConvertAtomToClass(Stream stream)
        {
            try
            {
                var youtubeNotification = new YoutubeNotification();
                string xmlText = new StreamReader(stream).ReadToEnd();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlText);

                if (xmlText.Contains("https://www.youtube.com/xml/feeds/videos.xml?channel_id"))
                {
                    youtubeNotification.VideoId = doc.GetElementsByTagName("yt:videoId")[0]?.InnerText;
                    youtubeNotification.ChannelId = doc.GetElementsByTagName("yt:channelId")[0]?.InnerText;
                    youtubeNotification.Title = doc.GetElementsByTagName("title")[1]?.InnerText;
                    youtubeNotification.Link = doc.GetElementsByTagName("link")[0]?.Attributes["href"].Value;
                    youtubeNotification.Published = (DateTime)(doc.GetElementsByTagName("published")[0]?.InnerText.ConvertDateTime());
                    youtubeNotification.Updated = (DateTime)(doc.GetElementsByTagName("updated")[0]?.InnerText.ConvertDateTime());
                    youtubeNotification.NotificationType = YoutubeNotification.YTNotificationType.CreateOrUpdated;

                    Utility.RedisSub.Publish("youtube.pubsub.update", JsonConvert.SerializeObject(youtubeNotification), StackExchange.Redis.CommandFlags.FireAndForget);
                }
                else if (xmlText.Contains("deleted-entry"))
                {
                    var node = doc.GetElementsByTagName("at:deleted-entry")[0];
                    if (node == null)
                        return null;

                    youtubeNotification.VideoId = node.Attributes.GetNamedItem("ref").Value.Split(new char[] { ':' })[2];
                    youtubeNotification.ChannelId = doc.GetElementsByTagName("uri")[0]?.InnerText;
                    youtubeNotification.ChannelId = youtubeNotification.ChannelId.Substring(youtubeNotification.ChannelId.Length - 24, 24);
                    youtubeNotification.Published = (DateTime)node.Attributes.GetNamedItem("when").Value.ConvertDateTime();
                    youtubeNotification.NotificationType = YoutubeNotification.YTNotificationType.Deleted;

                    Utility.RedisSub.Publish("youtube.pubsub.deleted", JsonConvert.SerializeObject(youtubeNotification), StackExchange.Redis.CommandFlags.FireAndForget);
                }
                else
                {
                    _logger.LogWarning("未知的Atom");
                    _logger.LogWarning(xmlText);
                    return null;
                }

                return youtubeNotification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertAtomToClass錯誤\n");
                return null;
            }
        }
    }

    public class YoutubeNotification
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