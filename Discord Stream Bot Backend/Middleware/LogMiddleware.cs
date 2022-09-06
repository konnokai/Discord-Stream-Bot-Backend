using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using NLog;
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
                string redisKey = $"server.errorcount:{remoteIpAddress.ToString().Replace(":", "-").Replace(".", "-")}";

                if (!context.Request.Headers.TryGetValue("Content-Type", out var contentType) || contentType != "application/atom+xml")
                {
                    var badCount = await Utility.RedisDb.StringGetAsync(redisKey);
                    if (badCount.HasValue && int.Parse(badCount.ToString()) >= 5)
                    {
                        await Utility.RedisDb.StringIncrementAsync(redisKey);
                        await Utility.RedisDb.KeyExpireAsync(redisKey, TimeSpan.FromHours(1));
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

                await _next(context);

                logger.Info($"{remoteIpAddress} | {context.Request.Method} | {context.Response.StatusCode} | {requestUrl}");
                if (context.Response.StatusCode >= 400 && context.Response.StatusCode < 500)
                {
                    await Utility.RedisDb.StringIncrementAsync(redisKey);
                    await Utility.RedisDb.KeyExpireAsync(redisKey, TimeSpan.FromHours(1));
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
