using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Stream_Bot_Backend.Middleware
{
    public class LogMiddleware
    {
        private readonly RequestDelegate _next;
        private Logger logger = LogManager.GetLogger("ACCE");

        public LogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var originalResponseBodyStream = context.Response.Body;

            try
            {
                var remoteIpAddress = context.GetRemoteIPAddress();
                var requestUrl = context.Request.GetDisplayUrl();
                string badReqRedisKey = $"server.errorcount:{remoteIpAddress.ToString().Replace(":", "-").Replace(".", "-")}";
                string rngReqRedisKey = $"server.rngvideocount:{remoteIpAddress.ToString().Replace(":", "-").Replace(".", "-")}";
                bool isRedisError = false;

                try
                {
                    if (!context.Request.Headers.TryGetValue("Content-Type", out var contentType) || contentType != "application/atom+xml")
                    {
                        var badCount = await Utility.RedisDb.StringGetAsync(badReqRedisKey);
                        if (badCount.HasValue && int.Parse(badCount.ToString()) >= 5)
                        {
                            await Utility.RedisDb.StringIncrementAsync(badReqRedisKey);
                            await Utility.RedisDb.KeyExpireAsync(badReqRedisKey, TimeSpan.FromHours(1));
                            var errorMessage = JsonConvert.SerializeObject(new
                            {
                                ErrorMessage = "429 Too Many Requests"
                            });
                            var bytes = Encoding.UTF8.GetBytes(errorMessage);

                            context.Response.StatusCode = 429;
                            await originalResponseBodyStream.WriteAsync(
                                bytes, 0, bytes.Length);
                            return;
                        }
                    }
                    if (requestUrl.ToLower().Contains("randomvideo"))
                    {
                        var rngReqCount = await Utility.RedisDb.StringGetAsync(rngReqRedisKey);
                        if (rngReqCount.HasValue && int.Parse(rngReqCount.ToString()) >= 5)
                        {
                            await Utility.RedisDb.StringIncrementAsync(rngReqRedisKey);
                            await Utility.RedisDb.KeyExpireAsync(rngReqRedisKey, TimeSpan.FromHours(1));
                            var errorMessage = JsonConvert.SerializeObject(new
                            {
                                ErrorMessage = "429 Too Many Requests"
                            });
                            var bytes = Encoding.UTF8.GetBytes(errorMessage);

                            context.Response.StatusCode = 429;
                            await originalResponseBodyStream.WriteAsync(
                                bytes, 0, bytes.Length);
                            return;
                        }
                    }
                }
                catch (RedisConnectionException redisEx)
                {
                    logger.Error(redisEx, "Redis掛掉了");
                    isRedisError = true;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Middleware錯誤");
                }

                await _next(context);

                logger.Info($"{remoteIpAddress} | {context.Request.Method} | {context.Response.StatusCode} | {requestUrl}");

                if (!isRedisError)
                {
                    if (context.Response.StatusCode >= 400 && context.Response.StatusCode < 500)
                    {
                        await Utility.RedisDb.StringIncrementAsync(badReqRedisKey);
                        await Utility.RedisDb.KeyExpireAsync(badReqRedisKey, TimeSpan.FromHours(1));
                    }
                    if (requestUrl.ToLower().Contains("randomvideo"))
                    {
                        await Utility.RedisDb.StringIncrementAsync(rngReqRedisKey);
                        await Utility.RedisDb.KeyExpireAsync(rngReqRedisKey, TimeSpan.FromHours(1));
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e);

                var errorMessage = JsonConvert.SerializeObject(new
                {
                    ErrorMessage = e.Message
                });
                var bytes = Encoding.UTF8.GetBytes(errorMessage);

                await originalResponseBodyStream.WriteAsync(
                    bytes, 0, bytes.Length);
            }
        }
    }
}