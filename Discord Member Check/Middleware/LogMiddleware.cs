using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using NLog;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Member_Check.Middleware
{
    public class LogMiddleware
    {
        private readonly RequestDelegate _next;
        private Logger logger = LogManager.GetLogger("AccessLog");

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
                logger.Info($"({remoteIpAddress}) {context.Request.Method} {requestUrl}");

                await _next(context);
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
