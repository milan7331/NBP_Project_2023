using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Newtonsoft.Json;
using NBP_Project_2023.Shared;

namespace NBP_Project_2023.Server.Services
{
    public class RedisCachingService //: IRedisCachingService //, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisCachingService(IConnectionMultiplexer redis)
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

            if (value == RedisValue.Null)
            {
                return null;
            }
            result = Deserialize<T>(value);
            return result;           
        }

        //public async Task<T?> GetCourierLoginDataAsync<T>(string firstName, string lastName) where T : class
        //{
        //    // specific wrapper method that checks for courier data in another set first and then uses
        //    // the regular GetStringDataAsync<T>(string key) where T : class
        //    T? result = default;
        //}

        public async Task<bool> SetStringDataAsync<T>(string key, T value) where T : class
        {
            IDatabase db;
            RedisValue redisValue;

            db = _redis.GetDatabase(1);
            redisValue = Serialize(value);

            return await db.StringSetAsync($"{typeof(T).Name}:{key}", redisValue, TimeSpan.FromHours(6.0));
        }

        public async Task<bool> DeleteStringDataAsync(string key)
        {
            IDatabase db;
            db = _redis.GetDatabase(1);
            return await db.KeyDeleteAsync(key);

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
