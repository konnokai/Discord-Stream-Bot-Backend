using Discord_Stream_Bot_Backend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace Discord_Stream_Bot_Backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.UseMemberCasing();
            });

            services.AddSingleton<RedisService>();

            services.AddTwitchLibEventSubWebhooks(config =>
            {
                config.CallbackPath = "/TwitchWebHooks";
                config.Secret = Configuration["Twitch:WebHookSecret"];
                config.EnableLogging = false;
            });

            services.AddHostedService<EventSubHostedService>();

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<Middleware.LogMiddleware>();

            app.UseRouting();
            app.UseCors();
            app.UseAuthorization();

            app.UseTwitchLibEventSubWebhooks();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
