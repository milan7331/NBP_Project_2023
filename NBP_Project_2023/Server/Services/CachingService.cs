using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Newtonsoft.Json;
using NBP_Project_2023.Shared;

namespace NBP_Project_2023.Server.Services
{
    public class CachingService : IRedisCachingService //, IDisposable
    {
        private readonly ConnectionMultiplexer _redis;

        public CachingService(ConnectionMultiplexer redis)
        {
            // add failed connetion error? /lmao
            // add alt methods for other access variations that dont go through the id
            _redis = redis;
        }

        public async Task<T?> GetStringDataAsync<T>(string key) where T : class 
        {
            T? result = default;
            IDatabase db;
            RedisValue value;

            db = _redis.GetDatabase(1);
            value = await db.StringGetSetExpiryAsync($"{typeof(T).Name}:{key}", TimeSpan.FromHours(6.0));

            if (value != RedisValue.Null)
            {
                result = Deserialize<T>(value);
            }

            return result;           
        }

        public async Task<bool> SetStringDataAsync<T>(string key, T value) where T : class
        {
            IDatabase db;
            RedisValue redisValue;

            db = _redis.GetDatabase(1);
            redisValue = Serialize(value);

            return await db.StringSetAsync($"{typeof(T).Name}:{key}", redisValue, TimeSpan.FromHours(6.0));
        }

        private static RedisValue Serialize<T>(T value) where T : class
        {
            return new RedisValue(JsonConvert.SerializeObject(value));
        }

        private static T? Deserialize<T>(RedisValue value) where T : class
        {
            string json = value.ToString();
            return JsonConvert.DeserializeObject<T>(json) ?? null;
        }

    }
}
