using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Xml;

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

        //原則上不須處理Deleted的影片，小幫手會自動處理 (暫時)
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
                            return Content("200");

                        if (data.IsNewVideo)
                        {
                            _logger.LogInformation($"New Video!\n" +
                                $"Title: {data}\n" +
                                $"Video Id: {data.VideoId}\n" +
                                $"Channel Id: {data.ChannelId}\n" +
                                $"Published: {data.Published}");
                        }
                        else
                        {
                            _logger.LogInformation($"Video Update!\n" +
                               $"Title: {data.Title}\n" +
                               $"Video Id: {data.VideoId}\n" +
                               $"Channel Id: {data.ChannelId}\n" +
                               $"Published: {data.Published}\n" +
                               $"Updated: {data.Updated}");
                        }

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
            string xmlText = new StreamReader(stream).ReadToEnd();
            //_logger.LogInformation(xmlText);
            if (xmlText.Contains("deleted-entry"))
                return null;

            var YoutubeNotification = new YoutubeNotification();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);
            YoutubeNotification.VideoId = doc.GetElementsByTagName("yt:videoId")[0]?.InnerText;
            YoutubeNotification.ChannelId = doc.GetElementsByTagName("yt:channelId")[0]?.InnerText;
            YoutubeNotification.Title = doc.GetElementsByTagName("title")[1]?.InnerText;
            YoutubeNotification.Link = doc.GetElementsByTagName("link")[0]?.Attributes["href"].Value;
            YoutubeNotification.Published = doc.GetElementsByTagName("published")[0]?.InnerText;
            YoutubeNotification.Updated = doc.GetElementsByTagName("updated")[0]?.InnerText;

            return YoutubeNotification;


            //Regex regex = new Regex(@"<at:deleted-entry ref=""yt:video:(?'VideoId'[\w\-\\_]{11})"" when=""(?'Date'\d{4}-\d{2}-\d{2})T(?'Time'\d{2}:\d{2}:\d{2})");
            //if (regex.IsMatch(xmlText))
            //{
            //    var regexResult = regex.Match(xmlText);
            //    youtubeNotification = new YoutubeNotification()
            //    {
            //        VideoId = regexResult.Groups["VideoId"].Value,
            //        Published = Convert.ToDateTime($"{regexResult.Groups["Date"].Value} {regexResult.Groups["Time"].Value}").AddHours(8),
            //        NotificationType = NotificationType.Deleted
            //    };
            //}
            //else
            //{
            //}
        }
    }

    public class YoutubeNotification
    {
        public string VideoId { get; set; }
        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Published { get; set; }
        public string Updated { get; set; }
        public bool IsNewVideo
        {
            get
            {
                return Published == Updated && !string.IsNullOrEmpty(Published);
            }
        }
    }
}
