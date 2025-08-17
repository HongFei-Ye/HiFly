// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
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
                    var result = JsonSerializer.Deserialize<T>(jsonString);
                    _logger.LogDebug("缓存命中: {Key}", key);
                    return result;
                }

                if (cachedValue is T directValue)
                {
                    _logger.LogDebug("缓存直接命中: {Key}", key);
                    return directValue;
                }
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
            var expiryTime = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

            var entryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiryTime,
                SlidingExpiration = TimeSpan.FromMinutes(Math.Min(expiryTime.TotalMinutes / 2, 30)),
                Priority = CacheItemPriority.Normal
            };

            // 注册移除回调
            entryOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
            {
                if (evictedKey is string keyStr)
                {
                    _keyTracker.TryRemove(keyStr, out _);
                    _statistics.ItemCount = Math.Max(0, _statistics.ItemCount - 1);
                }
            });

            // 序列化复杂对象
            object cacheValue = value;
            if (ShouldSerialize<T>())
            {
                cacheValue = JsonSerializer.Serialize(value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            _memoryCache.Set(fullKey, cacheValue, entryOptions);
            _keyTracker.TryAdd(fullKey, DateTime.UtcNow);
            _statistics.ItemCount++;
            _statistics.LastUpdated = DateTime.UtcNow;

            _logger.LogDebug("缓存设置成功: {Key}, 过期时间: {Expiry}", key, expiryTime);
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
            _statistics.ItemCount = Math.Max(0, _statistics.ItemCount - 1);
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

            _statistics.ItemCount = Math.Max(0, _statistics.ItemCount - removedCount);
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
    /// 构建完整的缓存键
    /// </summary>
    private string BuildFullKey(string key)
    {
        return key.StartsWith(_options.KeyPrefix) ? key : $"{_options.KeyPrefix}{key}";
    }

    /// <summary>
    /// 判断是否需要序列化
    /// </summary>
    private static bool ShouldSerialize<T>()
    {
        var type = typeof(T);
        return !type.IsPrimitive && 
               type != typeof(string) && 
               type != typeof(DateTime) && 
               type != typeof(TimeSpan) &&
               type != typeof(Guid);
    }
}
