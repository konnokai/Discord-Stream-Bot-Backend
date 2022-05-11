using Newtonsoft.Json;
using System;
using System.Text;

namespace Discord_Member_Check.Auth
{
    public class TokenManager
    {
        //金鑰，從設定檔或資料庫取得
        public static string key = Utility.ServerConfig.TokenKey;

        /// <summary>
        /// 產生加密使用者資料
        /// </summary>
        /// <param name="user">尚未加密的使用者資料</param>
        /// <returns>已加密的使用者資料</returns>
        public static string CreateToken(string userId)
        {
            var json = JsonConvert.SerializeObject(userId);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            var iv = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16);

            //使用 AES 加密 Payload
            var encrypt = TokenCrypto
                .AESEncrypt(base64, key.Substring(0, 16), iv);

            //取得簽章
            var signature = TokenCrypto
                .ComputeHMACSHA256(iv + "." + encrypt, key.Substring(0, 64));

            return iv + "." + encrypt + "." + signature;
        }

        /// <summary>
        /// 解密使用者資料
        /// </summary>
        /// <param name="token">已加密的使用者資料</param>
        /// <returns>未加密的使用者資料</returns>
        public static string GetUser(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            token = token.Replace(" ", "+");
            var split = token.Split('.');
            if (split.Length != 3) return null;

            var iv = split[0];
            var encrypt = split[1];
            var signature = split[2];

            //檢查簽章是否正確
            if (signature != TokenCrypto.ComputeHMACSHA256(iv + "." + encrypt, key.Substring(0, 64)))            
                return null;            

            //使用 AES 解密 Payload
            var base64 = TokenCrypto.AESDecrypt(encrypt, key.Substring(0, 16), iv);
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var payload = JsonConvert.DeserializeObject<string>(json);

            return payload;
        }
    }
}
