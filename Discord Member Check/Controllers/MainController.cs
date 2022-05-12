using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Member_Check.Controllers
{
    [Route("[action]")]
    [ApiController]
    public class MainController : ControllerBase
    {
        private readonly ILogger<MainController> _logger;
        private readonly GoogleAuthorizationCodeFlow flow;

        public MainController(ILogger<MainController> logger)
        {
            _logger = logger;
            flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = Utility.ServerConfig.GoogleClientId,
                    ClientSecret = Utility.ServerConfig.GoogleClientSecret
                },
                Scopes = new string[] { "https://www.googleapis.com/auth/youtube.force-ssl https://www.googleapis.com/auth/userinfo.profile" },
                DataStore = new RedisDataStore(RedisConnection.Instance.ConnectionMultiplexer)
            });
        }

        [EnableCors("allowGET")]
        [HttpGet]
        public async Task<APIResult> DiscordCallBack(string code)
        {
            if (string.IsNullOrEmpty(code))
                return new APIResult(ResultStatusCode.BadRequest, "參數錯誤");

            try
            {
                using WebClient webClient = new WebClient();
                TokenData tokenData = null;
                try
                {
                    webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    tokenData = JsonConvert.DeserializeObject<TokenData>(await webClient.UploadStringTaskAsync("https://discord.com/api/oauth2/token",
                        $"code={code}&client_id={Utility.ServerConfig.DiscordClientId}&client_secret={Utility.ServerConfig.DiscordClientSecret}&redirect_uri={Utility.UrlEncode(Utility.ServerConfig.RedirectURI)}&grant_type=authorization_code"));

                    if (tokenData == null || tokenData.access_token == null)
                        return new APIResult(ResultStatusCode.Unauthorized, "認證錯誤，請重新登入Discord");
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("400")) return new APIResult(ResultStatusCode.BadRequest, "請重新登入Discord");

                    _logger.LogError(ex.ToString());
                    return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                }

                DiscordUser discordUser = null;
                try
                {
                    webClient.Headers.Clear();
                    webClient.Headers.Add(HttpRequestHeader.Accept, "application/json");
                    webClient.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {tokenData.access_token}");
                    var discordMeJson = await webClient.DownloadStringTaskAsync("https://discord.com/api/v9/users/@me");
                    discordUser = JsonConvert.DeserializeObject<DiscordUser>(discordMeJson);

                    _logger.LogInformation($"Discord User OAuth Done: {discordUser.username} ({discordUser.id})");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                }

                string token = "";
                try
                {
                    token = Auth.TokenManager.CreateToken(discordUser.id);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex.ToString());
                    return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                }

                return new APIResult(ResultStatusCode.OK, new { Token = token, DiscordData = discordUser });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
            }

        }

        [EnableCors("allowGET")]
        [HttpGet]
        public async Task<APIResult> GoogleCallBack(string code, string state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return new APIResult(ResultStatusCode.BadRequest, "參數錯誤");

            try
            {
                var discordUser = Auth.TokenManager.GetUser<string>(state);
                if (discordUser == null)
                    return new APIResult(ResultStatusCode.Unauthorized, "Token無效，請重新登入Discord");

                var googleToken = await flow.ExchangeCodeForTokenAsync(discordUser, code, Utility.ServerConfig.RedirectURI, CancellationToken.None);
                if (googleToken == null || string.IsNullOrEmpty(googleToken.AccessToken) || string.IsNullOrEmpty(googleToken.RefreshToken))
                    return new APIResult(ResultStatusCode.Unauthorized, "Google授權驗證無效或尚未登入Google\n請解除應用程式授權後再登入Google帳號");

                return new APIResult(ResultStatusCode.OK);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("invalid_grant"))
                {
                    return new APIResult(ResultStatusCode.Unauthorized, "Google授權驗證無效或尚未登入Google\n請解除應用程式授權後再登入Google帳號");
                }
                else
                {
                    _logger.LogError(ex.ToString());
                    return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                }
            }
        }

        [EnableCors("allowGET")]
        [HttpGet]
        public async Task<APIResult> GetGoogleData(string token = "")
        {
            if (string.IsNullOrEmpty(token))
                return new APIResult(ResultStatusCode.BadRequest, "Token不可為空");

            try
            {
                var discordUser = Auth.TokenManager.GetUser<string>(token);
                if (discordUser == null)
                    return new APIResult(ResultStatusCode.Unauthorized, "Token無效");

                var googleToken = await flow.LoadTokenAsync(discordUser, CancellationToken.None);

                if (googleToken == null || string.IsNullOrEmpty(googleToken.AccessToken) || string.IsNullOrEmpty(googleToken.RefreshToken))
                {
                   if (googleToken != null && !string.IsNullOrEmpty(googleToken.AccessToken))
                        await flow.RevokeTokenAsync(discordUser, googleToken.AccessToken, CancellationToken.None);
                    return new APIResult(ResultStatusCode.Unauthorized, "Google授權驗證無效或尚未登入Google\n請解除應用程式授權後再登入Google帳號");
                }

                try
                {
                    if (googleToken.IsExpired(Google.Apis.Util.SystemClock.Default))
                        await flow.RefreshTokenAsync(discordUser, googleToken.RefreshToken, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return new APIResult(ResultStatusCode.Unauthorized, "Google授權驗證無效\n請解除應用程式授權後再登入Google帳號");
                }

                MemberData user = new MemberData();
                using WebClient webClient = new WebClient();
                webClient.Headers.Add(HttpRequestHeader.Accept, "application/json");
                webClient.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {googleToken.AccessToken}");
                webClient.Headers.Add(HttpRequestHeader.AcceptLanguage, "zh-TW, en;q=0.9;, *;q=0.5");

                try
                {
                    GoogleJson userData = JsonConvert.DeserializeObject<GoogleJson>(await webClient.DownloadStringTaskAsync($"https://people.googleapis.com/v1/people/me?personFields=photos%2Cnames"));

                    user.GoogleUserName = userData.names.First().displayName;
                    user.GoogleUserAvatar = userData.photos.First().url;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                }

                try
                {
                    YoutubeChannelMeJson youtubeUserData = JsonConvert.DeserializeObject<YoutubeChannelMeJson>(await webClient.DownloadStringTaskAsync($"https://youtube.googleapis.com/youtube/v3/channels?part=id&mine=true"));
                    user.YoutubeChannelId = youtubeUserData.items.First().id;
                }
                catch (ArgumentNullException)
                {
                    return new APIResult(ResultStatusCode.BadRequest, "請確認此Google帳號有Youtube頻道");
                }
                catch (WebException ex)
                {
                    if (ex.Message.Contains("403")) return new APIResult(ResultStatusCode.Unauthorized, "請在登入Google帳號時勾選\n\"查看、編輯及永久刪除您的 YouTube 影片、評價、留言和字幕\"");
                    else
                    {
                        _logger.LogError(ex.ToString());
                        return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                }

                return new APIResult(ResultStatusCode.OK, new { UserName = user.GoogleUserName, UserAvatarUrl = user.GoogleUserAvatar });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error");
                return new APIResult(ResultStatusCode.InternalServerError, null);
            }
        }
    }
}
