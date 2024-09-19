using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Discord_Stream_Bot_Backend
{
    public class Program
    {
        public static string VERSION => GetLinkerTime(Assembly.GetEntryAssembly());
        public static void Main(string[] args)
        {
#if DEBUG
            Console.WriteLine("Waiting for debugger to attach");
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine("Debugger attached");
#endif

            var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
            try
            {
                logger.Debug("init main");
                logger.Info(VERSION + " 初始化中");

                Utility.ServerConfig.InitServerConfig();

                try
                {
                    RedisConnection.Init(Utility.ServerConfig.RedisOption);
                    Utility.Redis = RedisConnection.Instance.ConnectionMultiplexer;
                    Utility.RedisDb = Utility.Redis.GetDatabase(1);
                    Utility.RedisSub = Utility.Redis.GetSubscriber();

                    Utility.RedisSub.Subscribe(new StackExchange.Redis.RedisChannel("member.syncRedisToken", StackExchange.Redis.RedisChannel.PatternMode.Literal), (channel, value) =>
                    {
                        if (!value.HasValue || string.IsNullOrEmpty(value))
                            return;

                        logger.Info($"接收到新的 {nameof(ServerConfig.RedisTokenKey)}");

                        Utility.ServerConfig.RedisTokenKey = value.ToString();

                        try { File.WriteAllText("server_config.json", JsonConvert.SerializeObject(Utility.ServerConfig, Formatting.Indented)); }
                        catch (Exception ex)
                        {
                            logger.Error($"設定檔保存失敗: {ex}");
                            logger.Error($"請手動將此字串填入設定檔中的 \"{nameof(ServerConfig.RedisTokenKey)}\" 欄位: {value.ToString()}");
                            Environment.Exit(3);
                        }
                    });

                    logger.Info("Redis已連線");

                    try
                    {
                        Utility.TwitchWebHookSecret = Utility.Redis.GetDatabase(0).StringGet("twitch:webhook_secret");
                        if (string.IsNullOrEmpty(Utility.TwitchWebHookSecret))
                        {
                            logger.Warn("缺少 TwitchWebHookSecret，嘗試重新建立...");

                            Utility.TwitchWebHookSecret = ServerConfig.GenRandomKey(64);
                            Utility.Redis.GetDatabase(0).StringSet("twitch:webhook_secret", Utility.TwitchWebHookSecret);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "獲取 TwitchWebHookSecret 失敗\r\n");
                        return;
                    }
                }
                catch (Exception exception)
                {
                    logger.Error(exception, "Redis 連線錯誤，請確認伺服器是否已開啟\r\n");
                    return;
                }

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception exception)
            {
                //NLog: catch setup errors
                logger.Error(exception, "Stopped program because of exception\r\n");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
                Utility.RedisSub.UnsubscribeAll();
                Utility.Redis.Dispose();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                })
                .UseNLog();
        }

        public static string GetLinkerTime(Assembly assembly)
        {
            const string BuildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion != null)
            {
                var value = attribute.InformationalVersion;
                var index = value.IndexOf(BuildVersionMetadataPrefix);
                if (index > 0)
                {
                    value = value[(index + BuildVersionMetadataPrefix.Length)..];
                    return value;
                }
            }
            return default;
        }
    }
}
