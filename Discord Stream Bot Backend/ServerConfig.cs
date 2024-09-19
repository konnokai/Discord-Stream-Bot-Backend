using Newtonsoft.Json;
using NLog;
using System;
using System.IO;

public class ServerConfig
{
    public string BililiveWebHookUrl { get; set; } = "";
    public string DiscordClientId { get; set; } = "";
    public string DiscordClientSecret { get; set; } = "";
    public string GoogleClientId { get; set; } = "";
    public string GoogleClientSecret { get; set; } = "";
    public string RedirectURI { get; set; } = "";
    public string RedisOption { get; set; } = "127.0.0.1,syncTimeout=3000";
    public string TokenKey { get; set; } = "";
    public string RedisTokenKey { get; set; } = "";

    private Logger logger = LogManager.GetLogger("Conf");

    public void InitServerConfig()
    {
        try { File.WriteAllText("server_config_example.json", JsonConvert.SerializeObject(new ServerConfig(), Formatting.Indented)); } catch { }
        if (!File.Exists("server_config.json"))
        {
            logger.Error($"server_config.json 遺失，請依照 {Path.GetFullPath("server_config_example.json")} 內的格式填入正確的數值");
            if (!Console.IsInputRedirected)
                Console.ReadKey();
            Environment.Exit(3);
        }

        var config = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText("server_config.json"));

        try
        {
            if (string.IsNullOrWhiteSpace(config.DiscordClientId))
            {
                logger.Error("DiscordClientId 遺失，請輸入至 server_config.json 後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }
            if (string.IsNullOrWhiteSpace(config.DiscordClientSecret))
            {
                logger.Error("DiscordClientSecret 遺失，請輸入至 server_config.json 後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.GoogleClientId))
            {
                logger.Error("GoogleClientId 遺失，請輸入至 server_config.json 後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.GoogleClientSecret))
            {
                logger.Error("GoogleClientSecret 遺失，請輸入至 server_config.json 後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.RedirectURI))
            {
                logger.Error("RedirectURI 遺失，請輸入至 server_config.json 後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.RedisOption))
            {
                logger.Error("RedisOption 遺失，請輸入至 server_config.json 後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            BililiveWebHookUrl = config.BililiveWebHookUrl;
            DiscordClientId = config.DiscordClientId;
            DiscordClientSecret = config.DiscordClientSecret;
            GoogleClientId = config.GoogleClientId;
            GoogleClientSecret = config.GoogleClientSecret;
            RedirectURI = config.RedirectURI;
            RedisOption = config.RedisOption;
            TokenKey = config.TokenKey;
            RedisTokenKey = config.RedisTokenKey;

            if (string.IsNullOrWhiteSpace(config.TokenKey) || string.IsNullOrWhiteSpace(TokenKey))
            {
                logger.Error($"{nameof(TokenKey)} 遺失，將重新建立隨機亂數");

                TokenKey = GenRandomKey();

                try { File.WriteAllText("server_config.json", JsonConvert.SerializeObject(this, Formatting.Indented)); }
                catch (Exception ex)
                {
                    logger.Error($"設定檔保存失敗: {ex}");
                    logger.Error($"請手動將此字串填入設定檔中的 \"{nameof(TokenKey)}\" 欄位: {TokenKey}");
                    Environment.Exit(3);
                }
            }

            if (string.IsNullOrWhiteSpace(config.RedisTokenKey) || string.IsNullOrWhiteSpace(RedisTokenKey))
            {
                logger.Error($"{nameof(RedisTokenKey)} 遺失，將重新建立隨機亂數");

                RedisTokenKey = GenRandomKey();

                try { File.WriteAllText("server_config.json", JsonConvert.SerializeObject(this, Formatting.Indented)); }
                catch (Exception ex)
                {
                    logger.Error($"設定檔保存失敗: {ex}");
                    logger.Error($"請手動將此字串填入設定檔中的 \"{nameof(RedisTokenKey)}\" 欄位: {RedisTokenKey}");
                    Environment.Exit(3);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            throw;
        }
    }

    public static string GenRandomKey(int length = 128)
    {
        var characters = "ABCDEF_GHIJKLMNOPQRSTUVWXYZ@abcdefghijklmnopqrstuvwx-yz0123456789";
        var Charsarr = new char[128];
        var random = new Random();

        for (int i = 0; i < Charsarr.Length; i++)
        {
            Charsarr[i] = characters[random.Next(characters.Length)];
        }

        var resultString = new string(Charsarr);
        resultString = resultString[Math.Min(length, resultString.Length)..];
        return resultString;
    }
}