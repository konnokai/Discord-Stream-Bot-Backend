# Discord Stream Bot Backend

自行運行所需環境與參數
-
1. .NET Core 6.0 Runtime 或 SDK ([微軟網址](https://dotnet.microsoft.com/en-us/download/dotnet/6.0))
2. Redis Server ([Windows 下載網址](https://github.com/MicrosoftArchive/redis)，Linux 可直接透過 apt 或 yum 安裝)
3. Discord & Google 的 OAuth Client ID 跟 Client Secret，用於 YouTube 會限驗證，需搭配 [網站前端](https://github.com/DDhackers/auto-discord-ytmember-checker) 使用，如不需要會限驗證則可不用
4. Login Redirect Url，搭配上面的網站前端做YT會限使用，網址格式為: `https://[前端域名]/stream/login`
5. tmux 或 screen 等可背景運行的軟體 (Linux 環境下需要)
6. Redis 資料加密 Key 跟 JWT 加密 Key 會在運行的時後自行產生故不需要特別設定
7. TwitCasting 的 WebHook Signature，並且需要將 WebHook URL 設定為 `https://[後端域名]/TwitcastingWebHook` ([TwitCasting Dev](https://twitcasting.tv/developerapp.php))