using DiscordStreamBotBackend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace DiscordStreamBotBackend
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
            services.AddSingleton<Services.Auth.TokenService>();

            var hostUri = new Uri(Configuration["RedirectUrl"]);
            services.AddCors(options =>
            {
                options.AddPolicy(name: "allowGET", builder =>
                {
                    builder.WithOrigins($"{hostUri.Scheme}://{hostUri.Authority}")
                           .WithMethods("GET")
                           .WithHeaders("Content-Type");
                });
                options.AddPolicy(name: "allowPOST", builder =>
                {
                    builder.WithOrigins($"{hostUri.Scheme}://{hostUri.Authority}")
                           .WithMethods("POST")
                           .WithHeaders("Content-Type");
                });
            });

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
