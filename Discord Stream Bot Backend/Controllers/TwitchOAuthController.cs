using Discord_Stream_Bot_Backend.Model.Twitch;
using Discord_Stream_Bot_Backend.Services;
using Discord_Stream_Bot_Backend.Services.Auth;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Enums;

namespace Discord_Stream_Bot_Backend.Controllers
{
    // https://github.com/swiftyspiffy/Twitch-Auth-Example
    [Route("[action]")]
    [ApiController]
    public class TwitchOAuthController : Controller
    {
        private readonly ILogger<TwitchOAuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly RedisService _redisService;
        private readonly TokenService _tokenService;
        private readonly TwitchLib.Api.TwitchAPI _twitchAPI;

        public TwitchOAuthController(ILogger<TwitchOAuthController> logger,
            IConfiguration configuration,
            RedisService redisService,
            TokenService tokenService)
        {
            _logger = logger;
            _configuration = configuration;
            _redisService = redisService;
            _tokenService = tokenService;
            _twitchAPI = new TwitchLib.Api.TwitchAPI()
            {
                Settings =
                {
                     ClientId = _configuration["Twitch:ClientId"],
                     Secret = _configuration["Twitch:ClientSecret"]
                }
            };
        }

        [EnableCors("allowGET")]
        [HttpGet]
        public APIResult GetTwitchOAuthUrl(string state)
        {
            var url = _twitchAPI.Auth.GetAuthorizationCodeUrl(_configuration["RedirectUrl"],
                new List<AuthScopes>() { AuthScopes.Helix_Moderation_Read, AuthScopes.Helix_User_Read_Subscriptions },
                true,
                state);

            return new APIResult(ResultStatusCode.OK, url);
        }

        public async Task<APIResult> TwitchCallBack(string code, string state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return new APIResult(ResultStatusCode.BadRequest, "參數錯誤");

            try
            {
                if (await _redisService.RedisDb.KeyExistsAsync($"twitch:code:{code}"))
                    return new APIResult(ResultStatusCode.BadRequest, "請確認是否有插件或軟體導致重複驗證\n如網頁正常顯示資料則無需理會");

                await _redisService.RedisDb.StringSetAsync($"twitch:code:{code}", "0", TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TwitchCallBack - Redis 設定錯誤\n");
                return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
            }

            try
            {
                var discordUser = _tokenService.GetUser<string>(state);
                if (string.IsNullOrEmpty(discordUser))
                    return new APIResult(ResultStatusCode.Unauthorized, "Token 無效，請重新登入 Discord");

                var authCodeResponse = await _twitchAPI.Auth.GetAccessTokenFromCodeAsync(code, _configuration["Twitch:ClientSecret"], _configuration["RedirectUrl"]);
                if (authCodeResponse == null)
                    return new APIResult(ResultStatusCode.Unauthorized, "請重新登入 Twitch 帳號");

                if (string.IsNullOrEmpty(authCodeResponse.AccessToken))
                    return new APIResult(ResultStatusCode.Unauthorized, "Twitch 授權驗證無效\n請解除應用程式授權後再登入 Twitch 帳號");

                if (!string.IsNullOrEmpty(authCodeResponse.RefreshToken))
                {
                    var twitchAccessTokenData = new TwitchAccessTokenData()
                    {
                        AccessToken = authCodeResponse.AccessToken,
                        RefreshToken = authCodeResponse.RefreshToken,
                        ExpiresIn = authCodeResponse.ExpiresIn,
                        Scopes = authCodeResponse.Scopes,
                        TokenType = authCodeResponse.TokenType,
                    };

                    var encValue = _tokenService.CreateTokenResponseToken(twitchAccessTokenData);
                    await _redisService.RedisDb.StringSetAsync(new($"twitch:oauth:{discordUser}"), encValue);
                    return new APIResult(ResultStatusCode.OK);
                }
                else
                {
                    return new APIResult(ResultStatusCode.Unauthorized, "無法刷新 Twitch 授權\n請重新登入 Twitch 帳號");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("invalid_grant"))
                {
                    _logger.LogWarning("偵測到 invalid_grant");
                    return new APIResult(ResultStatusCode.Unauthorized, "Twitch 授權驗證無效或尚未登入\n請解除應用程式授權後再登入 Twitch 帳號");
                }
                else
                {
                    _logger.LogError(ex, "TwitchCallBack - 整體錯誤\n");
                    return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                }
            }
        }

        [EnableCors("allowGET")]
        [HttpGet]
        public async Task<APIResult> GetTwitchData(string token = "")
        {
            if (string.IsNullOrEmpty(token))
                return new APIResult(ResultStatusCode.BadRequest, "Token 不可為空");

            try
            {
                var discordUser = _tokenService.GetUser<string>(token);
                if (string.IsNullOrEmpty(discordUser))
                    return new APIResult(ResultStatusCode.Unauthorized, "Token 無效");

                var twitchTokenEnc = await _redisService.RedisDb.StringGetAsync(new RedisKey($"twitch:oauth:{discordUser}"));
                if (!twitchTokenEnc.HasValue)
                    return new APIResult(ResultStatusCode.Unauthorized, "請登入 Twitch 帳號");

                var twitchToken = _tokenService.GetTokenResponseValue<TwitchAccessTokenData>(twitchTokenEnc);
                if (string.IsNullOrEmpty(twitchToken.AccessToken))
                    return new APIResult(ResultStatusCode.Unauthorized, "Twitch 授權驗證無效\n請解除應用程式授權後再登入 Twitch 帳號");

                if (string.IsNullOrEmpty(twitchToken.RefreshToken))
                    return new APIResult(ResultStatusCode.Unauthorized, "無法刷新 Twitch 授權\n請重新登入 Twitch 帳號");

                try
                {
                    if (await _twitchAPI.Auth.ValidateAccessTokenAsync(twitchToken.AccessToken) == null)
                    {
                        var refreshResponse = await _twitchAPI.Auth.RefreshAuthTokenAsync(twitchToken.RefreshToken, _configuration["Twitch:ClientSecret"]);

                        twitchToken.AccessToken = refreshResponse.AccessToken;
                        twitchToken.RefreshToken = refreshResponse.RefreshToken;
                        twitchToken.ExpiresIn = refreshResponse.ExpiresIn;
                        twitchToken.Scopes = refreshResponse.Scopes;

                        var encValue = _tokenService.CreateTokenResponseToken(refreshResponse);
                        await _redisService.RedisDb.StringSetAsync(new($"twitch:oauth:{discordUser}"), encValue);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetTwitchData - 刷新 Token 錯誤\n");
                    return new APIResult(ResultStatusCode.Unauthorized, "無法刷新 Twitch 授權\n請重新登入 Twitch 帳號");
                }

                TwitchLib.Api.Helix.Models.Users.GetUsers.GetUsersResponse userData = null;
                try
                {
                    userData = await _twitchAPI.Helix.Users.GetUsersAsync(accessToken: twitchToken.AccessToken);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("403"))
                    {
                        _logger.LogError(ex, $"403錯誤\n");
                        return new APIResult(ResultStatusCode.Unauthorized, "請重新登入，並在登入 Google 帳號時勾選\n\"查看、編輯及永久刪除您的 YouTube 影片、評價、留言和字幕\"");
                    }
                    else if (ex.Message.Contains("401"))
                    {
                        _logger.LogError(ex, $"401錯誤\n");
                        return new APIResult(ResultStatusCode.InternalServerError, "請嘗試重新登入 Twitch 帳號");
                    }
                    else
                    {
                        _logger.LogError(ex, "GetTwitchData - Twitch API 回傳錯誤\n");
                        return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
                    }
                }

                if (userData == null)
                {
                    _logger.LogWarning("GetTwitchData - 無法取得資料\n");
                    return new APIResult(ResultStatusCode.BadRequest, "無法取得資料，請向孤之界回報");
                }

                var user = userData.Users.First();

                _logger.LogInformation("Twitch User OAuth Fetch Done: {TwitchUserName} ({TwitchUserLogin})", user.DisplayName, user.Login);
                return new APIResult(ResultStatusCode.OK, new { UserName = user.DisplayName, UserLogin = user.Login, UserAvatarUrl = user.ProfileImageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTwitchData - 整體錯誤\n");
                return new APIResult(ResultStatusCode.InternalServerError, "伺服器內部錯誤，請向孤之界回報");
            }
        }
    }
}
