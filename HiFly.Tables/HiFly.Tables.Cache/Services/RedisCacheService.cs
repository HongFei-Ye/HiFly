// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HiFly.Tables.Cache.Services;

/// <summary>
/// Redis缓存服务实现
/// </summary>
public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CacheStatistics _statistics;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IDistributedCache distributedCache,
        ILogger<RedisCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        
        _database = _redis.GetDatabase();
        _statistics = new CacheStatistics();

        // 配置JSON序列化选项
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = 
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }

    #region ICacheService 基础实现

    /// <summary>
    /// 获取缓存项
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            var fullKey = BuildFullKey(key);
            var cachedBytes = await _distributedCache.GetAsync(fullKey);
            
            if (cachedBytes != null)
            {
                _statistics.HitCount++;
                var json = System.Text.Encoding.UTF8.GetString(cachedBytes);
                var result = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                
                _logger.LogDebug("Redis缓存命中: {Key}", key);
                return result;
            }

            _statistics.MissCount++;
            _logger.LogDebug("Redis缓存未命中: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Redis获取缓存项时发生错误: {Key}", key);
            _statistics.MissCount++;
            return null;
        }
    }

    /// <summary>
    /// 设置缓存项
    /// </summary>
    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var fullKey = BuildFullKey(key);
            var cacheExpiry = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiry,
                SlidingExpiration = TimeSpan.FromMinutes(_options.DistributedCache.SlidingExpirationMinutes)
            };

            await _distributedCache.SetAsync(fullKey, bytes, options);
            _statistics.ItemCount++;
            _statistics.LastUpdated = DateTime.UtcNow;

            _logger.LogDebug("Redis缓存设置成功: {Key}, 过期时间: {Expiry}", key, cacheExpiry);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向Redis设置缓存项时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 删除缓存项
    /// </summary>
    public async Task<bool> RemoveAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            var fullKey = BuildFullKey(key);
            await _distributedCache.RemoveAsync(fullKey);
            
            _logger.LogDebug("Redis缓存项已删除: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Redis删除缓存项时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 检查缓存项是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            var fullKey = BuildFullKey(key);
            return await _database.KeyExistsAsync(fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查Redis缓存项存在性时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 批量删除缓存项（模式匹配）
    /// </summary>
    public async Task<int> RemoveByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        try
        {
            var fullPattern = BuildFullKey(pattern);
            
            // 🔥 使用Redis的KEYS命令查找匹配的键
            var matchingKeys = await GetKeysByPatternAsync(pattern);
            var keysList = matchingKeys.ToList();

            if (keysList.Count == 0)
            {
                _logger.LogDebug("没有找到匹配的Redis缓存键: {Pattern}", pattern);
                return 0;
            }

            // 🔥 批量删除匹配的键
            var deletedCount = await RemoveKeysAsync(keysList.Select(k => k));

            _logger.LogInformation("Redis批量删除缓存完成: {Pattern}, 删除数量: {Count}", pattern, deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis批量删除缓存时发生错误: {Pattern}", pattern);
            return 0;
        }
    }

    /// <summary>
    /// 获取缓存项的剩余生存时间
    /// </summary>
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            var fullKey = BuildFullKey(key);
            var ttl = await _database.KeyTimeToLiveAsync(fullKey);
            return ttl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Redis缓存项TTL时发生错误: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// 刷新缓存项的过期时间
    /// </summary>
    public async Task<bool> RefreshAsync(string key, TimeSpan expiry)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            var fullKey = BuildFullKey(key);
            return await _database.KeyExpireAsync(fullKey, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新Redis缓存项过期时间时发生错误: {Key}", key);
            return false;
        }
    }

    #endregion

    #region IRedisCacheService 特殊实现

    /// <summary>
    /// 获取Redis数据库
    /// </summary>
    public async Task<object?> GetDatabaseAsync()
    {
        await Task.CompletedTask;
        return _database;
    }

    /// <summary>
    /// 按模式查找键（优雅处理权限问题）
    /// </summary>
    public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        try
        {
            var fullPattern = BuildFullKey(pattern);
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            // 尝试使用KEYS命令查找匹配的键
            var keys = server.Keys(pattern: fullPattern);
            var result = keys.Select(k => k.ToString().Replace(_options.KeyPrefix, "")).ToList();
            
            _logger.LogDebug("找到匹配的Redis键: {Pattern}, 数量: {Count}", pattern, result.Count);
            return result;
        }
        catch (Exception ex) when (ex.Message.Contains("KEYS") || ex.Message.Contains("admin mode"))
        {
            _logger.LogWarning("Redis KEYS命令权限受限: {Message}", ex.Message);
            
            // 权限受限时，返回空集合但记录警告
            _logger.LogInformation("由于Redis权限限制，无法使用KEYS命令。模式删除功能可能受限。");
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查找Redis键时发生错误: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// 批量删除指定的键
    /// </summary>
    public async Task<int> RemoveKeysAsync(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var keysList = keys.ToList();
        if (keysList.Count == 0)
        {
            return 0;
        }

        try
        {
            // 🔥 使用Redis的DEL命令批量删除
            var fullKeys = keysList.Select(BuildFullKey).Select(k => (RedisKey)k).ToArray();
            var deletedCount = await _database.KeyDeleteAsync(fullKeys);
            
            _logger.LogDebug("Redis批量删除键完成，删除数量: {Count}/{Total}", deletedCount, keysList.Count);
            return (int)deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis批量删除键时发生错误");
            return 0;
        }
    }

    /// <summary>
    /// 清空数据库中的所有键
    /// </summary>
    public async Task<bool> FlushDatabaseAsync()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            await server.FlushDatabaseAsync();
            
            _logger.LogWarning("Redis数据库已清空");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空Redis数据库时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 获取数据库信息和统计（优雅处理权限问题）
    /// </summary>
    public async Task<Dictionary<string, object>> GetDatabaseInfoAsync()
    {
        var stats = new Dictionary<string, object>
        {
            ["IsConnected"] = _redis.IsConnected,
            ["DatabaseId"] = _database.Database,
            ["Statistics"] = _statistics,
            ["ConnectionEndpoints"] = _redis.GetEndPoints().Select(ep => ep.ToString()).ToArray()
        };

        try
        {
            // 尝试获取基本连接信息
            stats["ConnectionString"] = _redis.Configuration ?? "连接字符串不可用";
            
            // 尝试获取服务器信息（需要管理员权限）
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var info = await server.InfoAsync();
                
                stats["ServerInfo"] = info?.SelectMany(g => g)
                    .ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value) ?? new Dictionary<string, object>();
                    
                _logger.LogDebug("成功获取Redis服务器详细信息");
            }
            catch (Exception ex) when (ex.Message.Contains("admin mode") || ex.Message.Contains("INFO"))
            {
                // 权限受限，但连接正常
                stats["ServerInfo"] = new Dictionary<string, object>
                {
                    ["权限说明"] = "当前用户无管理员权限，无法获取详细服务器信息",
                    ["状态"] = "连接正常，缓存功能可用"
                };
                _logger.LogInformation("Redis权限受限，无法获取详细服务器信息，但连接正常");
            }

            // 尝试执行简单的测试操作来验证功能
            try
            {
                var testKey = "health_check_" + Guid.NewGuid();
                await _database.StringSetAsync(testKey, "test", TimeSpan.FromSeconds(10));
                var testResult = await _database.StringGetAsync(testKey);
                await _database.KeyDeleteAsync(testKey);
                
                stats["HealthCheck"] = new Dictionary<string, object>
                {
                    ["基本操作"] = "✅ 正常",
                    ["读写测试"] = testResult == "test" ? "✅ 成功" : "❌ 失败",
                    ["删除测试"] = "✅ 成功"
                };
            }
            catch (Exception ex)
            {
                stats["HealthCheck"] = new Dictionary<string, object>
                {
                    ["基本操作"] = $"❌ 失败: {ex.Message}"
                };
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取Redis数据库信息时发生错误");
            stats["Error"] = ex.Message;
            stats["ErrorType"] = ex.GetType().Name;
        }

        return stats;
    }

    /// <summary>
    /// 执行Lua脚本进行批量操作
    /// </summary>
    public async Task<object?> ExecuteScriptAsync(string script, string[]? keys = null, object[]? values = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(script);

        try
        {
            var redisKeys = keys?.Select(k => (RedisKey)BuildFullKey(k)).ToArray();
            var redisValues = values?.Select(v => (RedisValue)v.ToString()).ToArray();
            
            var result = await _database.ScriptEvaluateAsync(script, redisKeys, redisValues);
            
            _logger.LogDebug("Redis Lua脚本执行完成");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Redis Lua脚本时发生错误");
            return null;
        }
    }

    #endregion

    #region 高级批量删除方法

    /// <summary>
    /// 使用Lua脚本进行高性能批量删除
    /// </summary>
    public async Task<int> RemoveByPatternWithLuaAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        try
        {
            var fullPattern = BuildFullKey(pattern);
            
            // 🔥 使用Lua脚本进行原子性批量删除操作
            var luaScript = @"
                local keys = redis.call('KEYS', ARGV[1])
                local count = 0
                for i=1,#keys do
                    count = count + redis.call('DEL', keys[i])
                end
                return count
            ";

            var result = await _database.ScriptEvaluateAsync(luaScript, values: new RedisValue[] { fullPattern });
            var deletedCount = (int)result;
            
            _logger.LogInformation("Redis Lua脚本批量删除完成: {Pattern}, 删除数量: {Count}", pattern, deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis Lua脚本批量删除时发生错误: {Pattern}", pattern);
            return 0;
        }
    }

    /// <summary>
    /// 使用SCAN命令进行安全的模式删除（推荐用于生产环境）
    /// </summary>
    public async Task<int> RemoveByPatternWithScanAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        try
        {
            var fullPattern = BuildFullKey(pattern);
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            var totalDeleted = 0;
            const int batchSize = 1000; // 批处理大小
            
            // 🔥 使用SCAN命令逐批扫描和删除
            await foreach (var key in server.KeysAsync(pattern: fullPattern, pageSize: batchSize))
            {
                try
                {
                    var deleted = await _database.KeyDeleteAsync(key);
                    if (deleted)
                    {
                        totalDeleted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "删除单个Redis键时发生错误: {Key}", key);
                }
            }
            
            _logger.LogInformation("Redis SCAN批量删除完成: {Pattern}, 删除数量: {Count}", pattern, totalDeleted);
            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis SCAN批量删除时发生错误: {Pattern}", pattern);
            return 0;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 构建完整的缓存键
    /// </summary>
    private string BuildFullKey(string key)
    {
        return $"{_options.KeyPrefix}{key}";
    }

    #endregion
}
