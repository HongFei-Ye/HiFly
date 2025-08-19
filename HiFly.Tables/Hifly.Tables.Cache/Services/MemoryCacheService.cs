// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HiFly.Tables.Cache.Services;

/// <summary>
/// 内存缓存服务实现
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly ConcurrentDictionary<string, DateTime> _keyTracker;
    private readonly CacheStatistics _statistics;
    private readonly JsonSerializerOptions _jsonOptions;

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _keyTracker = new ConcurrentDictionary<string, DateTime>();
        _statistics = new CacheStatistics();

        // 配置专门用于缓存的JSON序列化选项
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false, // 缓存中不需要格式化
            IncludeFields = true, // 包含字段
            PropertyNameCaseInsensitive = true, // 大小写不敏感
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // 允许从字符串读取数字
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                // 添加对 Guid 的支持
                new JsonGuidConverter(),
                // 添加对 DateTime 的支持
                new JsonDateTimeConverter()
            }
        };
    }

    /// <summary>
    /// 获取缓存项
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            var fullKey = BuildFullKey(key);

            if (_memoryCache.TryGetValue(fullKey, out var cachedValue))
            {
                _statistics.HitCount++;
                _statistics.LastUpdated = DateTime.UtcNow;

                if (cachedValue is string jsonString)
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);
                        _logger.LogDebug("缓存命中: {Key}", key);
                        return result;
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "JSON 反序列化失败，移除损坏的缓存项: {Key}", key);
                        _memoryCache.Remove(fullKey);
                        _keyTracker.TryRemove(fullKey, out _);
                        _statistics.MissCount++;
                        return null;
                    }
                }

                if (cachedValue is T directValue)
                {
                    _logger.LogDebug("缓存直接命中: {Key}", key);
                    return directValue;
                }

                // 如果缓存值类型不匹配，记录警告并移除
                _logger.LogWarning("缓存值类型不匹配，移除缓存项: {Key}, 期望类型: {ExpectedType}, 实际类型: {ActualType}",
                    key, typeof(T).Name, cachedValue?.GetType().Name ?? "null");
                _memoryCache.Remove(fullKey);
                _keyTracker.TryRemove(fullKey, out _);
            }

            _statistics.MissCount++;
            _logger.LogDebug("缓存未命中: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存项时发生错误: {Key}", key);
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

            // 序列化为 JSON 字符串以确保一致性
            string jsonValue;
            try
            {
                jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON 序列化失败: {Key}, 类型: {Type}", key, typeof(T).Name);
                return false;
            }

            var entryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiry,
                Size = CalculateSize(jsonValue),
                Priority = CacheItemPriority.Normal
            };

            // 检查内存限制 - 使用 SizeLimitMB 配置
            var maxSizeBytes = _options.MemoryCache.SizeLimitMB * 1024 * 1024; // 转换为字节
            if (entryOptions.Size > maxSizeBytes / 100) // 单个项目不应超过总限制的1%
            {
                _logger.LogWarning("缓存项过大，跳过缓存: {Key}, 大小: {Size} bytes, 限制: {Limit} bytes",
                    key, entryOptions.Size, maxSizeBytes / 100);
                return false;
            }

            _memoryCache.Set(fullKey, jsonValue, entryOptions);
            _keyTracker[fullKey] = DateTime.UtcNow.Add(cacheExpiry);
            _statistics.ItemCount = _keyTracker.Count;

            _logger.LogDebug("缓存设置成功: {Key}, 过期时间: {Expiry}", key, cacheExpiry);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存项时发生错误: {Key}", key);
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
            _memoryCache.Remove(fullKey);
            _keyTracker.TryRemove(fullKey, out _);
            _statistics.ItemCount = _keyTracker.Count;
            _statistics.LastUpdated = DateTime.UtcNow;

            _logger.LogDebug("缓存项已删除: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除缓存项时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 检查缓存项是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var fullKey = BuildFullKey(key);
        return _memoryCache.TryGetValue(fullKey, out _);
    }

    /// <summary>
    /// 批量删除缓存项
    /// </summary>
    public async Task<int> RemoveByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        try
        {
            var regex = new Regex(pattern.Replace("*", ".*"), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var keysToRemove = _keyTracker.Keys.Where(key => regex.IsMatch(key)).ToList();

            var removedCount = 0;
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _keyTracker.TryRemove(key, out _);
                removedCount++;
            }

            _statistics.ItemCount = _keyTracker.Count;
            _statistics.LastUpdated = DateTime.UtcNow;

            _logger.LogDebug("批量删除缓存项: {Pattern}, 删除数量: {Count}", pattern, removedCount);
            return removedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除缓存项时发生错误: {Pattern}", pattern);
            return 0;
        }
    }

    /// <summary>
    /// 获取缓存项的剩余生存时间
    /// </summary>
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
    {
        // MemoryCache doesn't provide direct TTL access
        // This would need to be tracked separately if needed
        return null;
    }

    /// <summary>
    /// 刷新缓存项的过期时间
    /// </summary>
    public async Task<bool> RefreshAsync(string key, TimeSpan expiry)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var fullKey = BuildFullKey(key);
        if (_memoryCache.TryGetValue(fullKey, out var value))
        {
            // 重新设置相同的值但使用新的过期时间
            var entryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry,
                SlidingExpiration = TimeSpan.FromMinutes(Math.Min(expiry.TotalMinutes / 2, 30)),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(fullKey, value, entryOptions);
            _statistics.LastUpdated = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        _statistics.ItemCount = _keyTracker.Count;
        return _statistics;
    }

    /// <summary>
    /// 计算缓存项大小
    /// </summary>
    private static int CalculateSize(string value)
    {
        return System.Text.Encoding.UTF8.GetByteCount(value);
    }

    /// <summary>
    /// 构建完整的缓存键
    /// </summary>
    private string BuildFullKey(string key)
    {
        return $"{_options.KeyPrefix}{key}";
    }

    /// <summary>
    /// 获取所有缓存键
    /// </summary>
    /// <returns>所有缓存键列表</returns>
    public async Task<List<string>> GetAllKeysAsync()
    {
        await Task.CompletedTask; // 保持异步签名一致性
        
        // 清理过期的键
        CleanupExpiredKeys();
        
        return _keyTracker.Keys.ToList();
    }

    /// <summary>
    /// 获取匹配模式的缓存键
    /// </summary>
    /// <param name="pattern">模式，支持通配符*</param>
    /// <returns>匹配的缓存键列表</returns>
    public async Task<List<string>> GetKeysByPatternAsync(string pattern)
    {
        await Task.CompletedTask;
        
        CleanupExpiredKeys();
        
        if (string.IsNullOrEmpty(pattern) || pattern == "*")
        {
            return _keyTracker.Keys.ToList();
        }
        
        var regex = new Regex(pattern.Replace("*", ".*"), RegexOptions.Compiled | RegexOptions.IgnoreCase);
        return _keyTracker.Keys.Where(key => regex.IsMatch(key)).ToList();
    }

    /// <summary>
    /// 清理过期的键跟踪记录
    /// </summary>
    private void CleanupExpiredKeys()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _keyTracker
            .Where(kvp => kvp.Value < now)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var expiredKey in expiredKeys)
        {
            _keyTracker.TryRemove(expiredKey, out _);
        }
        
        _statistics.ItemCount = _keyTracker.Count;
    }
}

/// <summary>
/// 自定义 Guid 转换器
/// </summary>
public class JsonGuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (Guid.TryParse(stringValue, out var guid))
            {
                return guid;
            }
        }

        throw new JsonException($"无法将值转换为 Guid: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("D"));
    }
}

/// <summary>
/// 自定义 DateTime 转换器
/// </summary>
public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (DateTime.TryParse(stringValue, out var dateTime))
            {
                return dateTime;
            }
        }

        throw new JsonException($"无法将值转换为 DateTime: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O")); // ISO 8601 格式
    }
}
