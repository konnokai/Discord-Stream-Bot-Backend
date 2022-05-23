using Newtonsoft.Json;
using NLog;
using System;
using System.IO;

public class ServerConfig
{
    public string DiscordClientId { get; set; } = "";
    public string DiscordClientSecret { get; set; } = "";
    //public string GoogleApiKey { get; set; } = "";
    public string GoogleClientId { get; set; } = "";
    public string GoogleClientSecret { get; set; } = "";
    public string RedirectURI { get; set; } = "";
    public string RedisOption { get; set; } = "127.0.0.1,syncTimeout=3000";
    public string TokenKey { get; set; } = GenRandomKey();

    private Logger logger = LogManager.GetLogger("Conf");

    public void InitServerConfig()
    {
        try { File.WriteAllText("server_config_example.json", JsonConvert.SerializeObject(new ServerConfig(), Formatting.Indented)); } catch { }
        if (!File.Exists("server_config.json"))
        {
            logger.Error($"server_config.json遺失，請依照 {Path.GetFullPath("server_config_example.json")} 內的格式填入正確的數值");
            if (!Console.IsInputRedirected)
                Console.ReadKey();
            Environment.Exit(3);
        }

        var config = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText("server_config.json"));

        try
        {
            if (string.IsNullOrWhiteSpace(config.DiscordClientId))
            {
                logger.Error("DiscordToken遺失，請輸入至server_config.json後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }
            if (string.IsNullOrWhiteSpace(config.DiscordClientSecret))
            {
                logger.Error("DiscordToken遺失，請輸入至server_config.json後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            //if (string.IsNullOrWhiteSpace(config.GoogleApiKey))
            //{
            //    logger.Error("GoogleApiKey遺失，請輸入至server_config.json後重開伺服器");
            //    if (!Console.IsInputRedirected)
            //        Console.ReadKey();
            //    Environment.Exit(3);
            //}

            if (string.IsNullOrWhiteSpace(config.GoogleClientId))
            {
                logger.Error("GoogleClientId遺失，請輸入至server_config.json後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.GoogleClientSecret))
            {
                logger.Error("GoogleClientSecret遺失，請輸入至server_config.json後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.RedirectURI))
            {
                logger.Error("RedirectURI遺失，請輸入至server_config.json後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.RedisOption))
            {
                logger.Error("RedisOption遺失，請輸入至server_config.json後重開伺服器");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            if (string.IsNullOrWhiteSpace(config.TokenKey))
            {
                logger.Error("TokenKey遺失，請重新建立隨機亂數");
                if (!Console.IsInputRedirected)
                    Console.ReadKey();
                Environment.Exit(3);
            }

            DiscordClientId = config.DiscordClientId;
            DiscordClientSecret = config.DiscordClientSecret;
            //GoogleApiKey = config.GoogleApiKey;
            GoogleClientId = config.GoogleClientId;
            GoogleClientSecret = config.GoogleClientSecret;
            RedirectURI = config.RedirectURI;
            RedisOption = config.RedisOption;
            TokenKey = config.TokenKey;
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            throw;
        }
    }

    private static string GenRandomKey()
    {
        var characters = "ABCDEF_GHIJKLMNOPQRSTUVWXYZ@abcdefghijklmnopqrstuvwx-yz0123456789";
        var Charsarr = new char[128];
        var random = new Random();

        for (int i = 0; i < Charsarr.Length; i++)
        {
            Charsarr[i] = characters[random.Next(characters.Length)];
        }

        var resultString = new string(Charsarr);
        return resultString;
    }
}