using Microsoft.AspNetCore.Http;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Discord_Stream_Bot_Backend
{
    public static class Utility
    {
        public static List<string> NowRecordList { get; private set; } = new List<string>();       
        public static ConnectionMultiplexer Redis { get; set; }
        public static ISubscriber RedisSub { get; set; }
        public static IDatabase RedisDb { get; set; }
        public static ServerConfig ServerConfig { get; set; } = new ServerConfig();

        private const string UnReservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        private static readonly Logger logger;

        static Utility()
        {
            _ = new Timer((obj) => RefreshNowRecordList(), null, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(20));
            logger = LogManager.GetLogger("Utility");
        }

        /// <summary>
        /// Url Encoding
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string UrlEncode(string value)
        {
            StringBuilder result = new StringBuilder();

            foreach (char symbol in value)
            {
                if (UnReservedChars.IndexOf(symbol) != -1)
                {
                    result.Append(symbol);
                }
                else
                {
                    result.Append('%' + String.Format("{0:X2}", (int)symbol));
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Get remote ip address, optionally allowing for x-forwarded-for header check
        /// </summary>
        /// <param name="context">Http context</param>
        /// <param name="allowForwarded">Whether to allow x-forwarded-for header check</param>
        /// <returns>IPAddress</returns>
        public static IPAddress GetRemoteIPAddress(this HttpContext context, bool allowForwarded = true)
        {
            if (allowForwarded)
            {
                // if you are allowing these forward headers, please ensure you are restricting context.Connection.RemoteIpAddress
                // to cloud flare ips: https://www.cloudflare.com/ips/
                string header = (context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault());
                if (IPAddress.TryParse(header, out IPAddress ip))
                {
                    return ip;
                }
            }
            return context.Connection.RemoteIpAddress;
        }

        private static void RefreshNowRecordList()
        {
            try
            {
                var newNowRecordList = Redis.GetDatabase(0).SetMembers("youtube.nowRecord").Select((x) => x.ToString()).ToList();
                if (newNowRecordList.Any())
                {
                    NowRecordList.Clear();
                    NowRecordList.AddRange(newNowRecordList);
                }

                logger.Info($"重整現正直播的清單: {NowRecordList.Count}個直播");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "現正直播清單重整失敗");
                NowRecordList = new List<string>();
            }
        }
    }

    public class MemberData
    {
        public string DiscordUserId { get; set; }
        public string GoogleAccessToken { get; set; }
        public string GoogleRefrechToken { get; set; }
        public DateTime GoogleExpiresIn { get; set; }
        public string GoogleUserName { get; set; }
        public string GoogleUserAvatar { get; set; }
        public string YoutubeChannelId { get; set; }
    }

    /// <summary>
    /// API回傳狀態碼
    /// </summary>
    public enum ResultStatusCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        OK = 200,
        /// <summary>
        /// 已新增
        /// </summary>
        Created = 201,
        /// <summary>
        /// 錯誤的請求
        /// </summary>
        BadRequest = 400,
        /// <summary>
        /// 使用者未驗證
        /// </summary>
        Unauthorized = 401,
        /// <summary>
        /// 請求太多次
        /// </summary>
        TooManyRequests = 429,
        /// <summary>
        /// 伺服器內部錯誤
        /// </summary>
        InternalServerError = 500
    }

    /// <summary>
    /// API回傳物件
    /// </summary>
    public class APIResult
    {
        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="code">回傳狀態碼</param>
        /// <param name="message">Object訊息</param>
        public APIResult(ResultStatusCode code, object message = null)
        {
            this.code = (int)code;
            this.message = message;
        }

        public int code { get; set; }
        public object message { get; set; }
    }
}
