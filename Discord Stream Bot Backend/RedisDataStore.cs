using Discord_Stream_Bot_Backend.Auth;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using NLog.Web;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace Discord_Stream_Bot_Backend
{
    public class RedisDataStore : IDataStore
    {
        private readonly IDatabase _database;
        private readonly NLog.Logger _logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

        public RedisDataStore(ConnectionMultiplexer connectionMultiplexer)
        {
            _database = connectionMultiplexer.GetDatabase(1);
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync<T>(string key)
        {
            await _database.KeyDeleteAsync(GenerateStoredKey(key, typeof(T)));
        }

        public async Task<T> GetAsync<T>(string key)
        {
            if (!await _database.KeyExistsAsync(GenerateStoredKey(key, typeof(T))))
                return default(T);

            var str = (await _database.StringGetAsync(GenerateStoredKey(key, typeof(T)))).ToString();

            try
            {
                return TokenManager.GetTokenResponseValue<T>(str);
            }
            catch (Exception ex)
            {
                _logger.Warn($"RedisDataStore-GetAsync ({key}): 解密失敗，也許還沒加密? {ex}");

                try
                {
                    return JsonConvert.DeserializeObject<T>(str);
                }
                catch (Exception ex2)
                {
                    _logger.Error($"RedisDataStore-GetAsync ({key}): JsonDes失敗 {ex2}");
                    return default(T);
                }
            }
        }

        public async Task StoreAsync<T>(string key, T value)
        {
            var encValue = TokenManager.CreateTokenResponseToken(value);
            await _database.StringSetAsync(GenerateStoredKey(key, typeof(T)), encValue);
        }

        public async Task<bool> IsExistUserTokenAsync<T>(string key)
        {
            return await _database.KeyExistsAsync(GenerateStoredKey(key, typeof(T)));
        }

        public static string GenerateStoredKey(string key, Type t)
        {
            return string.Format("{0}:{1}", t.FullName, key);
        }
    }
}
