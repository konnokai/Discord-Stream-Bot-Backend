# Discord Stream Bot Backend
-

自行運行所需環境與參數
-
1. .NET Core 6.0 Runtime 或 SDK ([微軟網址](https://dotnet.microsoft.com/en-us/download/dotnet/5.0))
2. Redis Server ([Windows下載網址](https://github.com/MicrosoftArchive/redis)，Linux可直接透過apt或yum安裝)
3. Discord & Google 的 OAuth Client ID 跟 Client Secret，用於 YouTube 會限驗證，需搭配 [網站前端](https://github.com/DDhackers/auto-discord-ytmember-checker) 使用，如不需要會限驗證則可不用
4. Login Redirect Url，搭配上面的網站前端做YT會限使用，網址格式為: `https://[前端域名]/stream/login`
5. tmux 或 screen 等可背景運行的軟體 (Linux 環境下需要)
