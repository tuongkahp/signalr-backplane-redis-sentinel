using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace SignalRChat.Services;

public interface IRedisService
{
    //Task<T> GetStringValueAsync<T>(string key);
    //Task<T> GetStringValueProtobufAsync<T>(string key);
    //Task SetStringValueAsync(string key, object value, int? expireSecond);
    //Task DeleteStringValueAsync(string key);
}

public class RedisService : IRedisService
{
    private readonly ConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabaseAsync _redisDb;

    public RedisService()
    {
        try
        {
            var sentinelConfig = new ConfigurationOptions()
            {
                //ServiceName = _redisSettings.ServiceName,
                TieBreaker = "",
                CommandMap = CommandMap.Default,
                Password = "123456aA@",
                AbortOnConnectFail = false,
                AllowAdmin = true,
                Ssl = false
            };

            sentinelConfig.EndPoints.Add("localhost", 26376);
            sentinelConfig.EndPoints.Add("localhost", 26377);
            sentinelConfig.EndPoints.Add("localhost", 26379);

            var sentinelConnection = ConnectionMultiplexer.SentinelConnect(sentinelConfig, Console.Out);

            // Create master service configuration
            var masterConfig = new ConfigurationOptions
            {
                ServiceName = "mymaster",
                //EndPoints = { "localhost", "6376" },
                CommandMap = CommandMap.Default,
                Password = "123456aA@",
                AbortOnConnectFail = false,
                AllowAdmin = true,
                Ssl = false
            };

            // Get master Redis connection
            _connectionMultiplexer = sentinelConnection.GetSentinelMasterConnection(masterConfig, Console.Out);
            _redisDb = _connectionMultiplexer.GetDatabase(0);

            SetStringValueAsync("Key1", "Test connect");
        }
        catch (Exception ex)
        {
            var err = ($"Init redis err: {ex.Message} - {ex.StackTrace}");
            throw new Exception($"Connect redis has exception");
        }
    }

    public async Task<T> GetStringValueAsync<T>(string key)
    {
        var cache = await _redisDb.StringGetAsync(key);
        return JsonSerializer.Deserialize<T>(cache);
    }

    public async Task<T> GetStringValueProtobufAsync<T>(string key)
    {
        var cache = await _redisDb.StringGetAsync(key);
        return JsonSerializer.Deserialize<T>(cache);
        //return JsonHelper.Deserialize<T>(cache);
    }

    public async Task SetStringValueAsync(string key, object value, int? expireSecond = null)
    {
        if (expireSecond == null)
        {
            await _redisDb.StringSetAsync(key, JsonSerializer.Serialize(value));
            return;
        }

        if (value.GetType() == typeof(string))
            await _redisDb.StringSetAsync(key, value.ToString(), new TimeSpan(0, 0, expireSecond.Value));

        await _redisDb.StringSetAsync(key, JsonSerializer.Serialize(value), new TimeSpan(0, 0, expireSecond.Value));
    }

    public async Task DeleteStringValueAsync(string key)
    {
        await _redisDb.KeyDeleteAsync(key);
    }
}