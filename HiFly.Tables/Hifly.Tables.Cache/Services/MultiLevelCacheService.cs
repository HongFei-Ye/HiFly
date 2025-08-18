// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HiFly.Tables.Cache.Services;

/// <summary>
/// 多级缓存服务实现
/// </summary>
public class MultiLevelCacheService : IMultiLevelCacheService
{
    private readonly MemoryCacheService _level1Cache; // L1: 内存缓存
    private readonly IDistributedCache? _level2Cache; // L2: 分布式缓存（Redis等）
    private readonly IRedisCacheService? _redisCache; // Redis专门服务
    private readonly ILogger<MultiLevelCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly Dictionary<string, CacheStatistics> _levelStatistics;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiLevelCacheService(
        MemoryCacheService memoryCacheService,
        ILogger<MultiLevelCacheService> logger,
        IOptions<CacheOptions> options,
        IDistributedCache? distributedCache = null,
        IRedisCacheService? redisCacheService = null)
    {
        _level1Cache = memoryCacheService ?? throw new ArgumentNullException(nameof(memoryCacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _level2Cache = _options.EnableDistributedCache ? distributedCache : null;
        _redisCache = _options.EnableDistributedCache ? redisCacheService : null;
        
        _levelStatistics = new Dictionary<string, CacheStatistics>
        {
            ["Level1"] = new CacheStatistics(),
            ["Level2"] = new CacheStatistics()
        };

        // 配置专门用于缓存的JSON序列化选项
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false, // 缓存中不需要格式化
            IncludeFields = true, // 包含字段
            PropertyNameCaseInsensitive = true, // 大小写不敏感
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = 
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }

    /// <summary>
    /// 获取缓存项（多级查找）
    /// </summary>
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            // L1: 内存缓存查找
            var result = await _level1Cache.GetAsync<T>(key);
            if (result != null)
            {
                _levelStatistics["Level1"].HitCount++;
                _logger.LogDebug("L1缓存命中: {Key}", key);
                return result;
            }

            _levelStatistics["Level1"].MissCount++;

            // L2: 分布式缓存查找
            if (_level2Cache != null)
            {
                result = await GetFromDistributedCacheAsync<T>(key);
                if (result != null)
                {
                    _levelStatistics["Level2"].HitCount++;
                    _logger.LogDebug("L2缓存命中: {Key}", key);

                    // 回写到L1缓存
                    await _level1Cache.SetAsync(key, result, TimeSpan.FromMinutes(_options.MemoryCache.SizeLimitMB));
                    return result;
                }

                _levelStatistics["Level2"].MissCount++;
            }

            _logger.LogDebug("所有缓存层级未命中: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "多级缓存获取时发生错误: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// 设置缓存项（多级写入）
    /// </summary>
    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        var success = true;
        var cacheExpiry = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

        try
        {
            // L1: 写入内存缓存
            var l1Success = await _level1Cache.SetAsync(key, value, cacheExpiry);
            if (!l1Success)
            {
                success = false;
                _logger.LogWarning("L1缓存写入失败: {Key}", key);
            }

            // L2: 写入分布式缓存
            if (_level2Cache != null)
            {
                var l2Success = await SetToDistributedCacheAsync(key, value, cacheExpiry);
                if (!l2Success)
                {
                    success = false;
                    _logger.LogWarning("L2缓存写入失败: {Key}", key);
                }
            }

            if (success)
            {
                _logger.LogDebug("多级缓存写入成功: {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "多级缓存设置时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 删除缓存项（多级删除）
    /// </summary>
    public async Task<bool> RemoveAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var success = true;

        try
        {
            // L1: 删除内存缓存
            var l1Success = await _level1Cache.RemoveAsync(key);
            if (!l1Success)
            {
                success = false;
            }

            // L2: 删除分布式缓存
            if (_level2Cache != null)
            {
                await _level2Cache.RemoveAsync(key);
            }

            _logger.LogDebug("多级缓存删除: {Key}, 成功: {Success}", key, success);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "多级缓存删除时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 检查缓存项是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        // 检查L1缓存
        if (await _level1Cache.ExistsAsync(key))
        {
            return true;
        }

        // 检查L2缓存
        if (_level2Cache != null)
        {
            var result = await GetFromDistributedCacheAsync<object>(key);
            return result != null;
        }

        return false;
    }

    /// <summary>
    /// 批量删除缓存项（支持Redis模式删除）
    /// </summary>
    public async Task<int> RemoveByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        var totalRemoved = 0;

        try
        {
            // L1: 批量删除内存缓存
            var l1Removed = await _level1Cache.RemoveByPatternAsync(pattern);
            totalRemoved += l1Removed;
            _logger.LogDebug("L1缓存删除数量: {Count}", l1Removed);

            // L2: 批量删除分布式缓存（使用Redis服务）
            if (_redisCache != null)
            {
                // 🔥 使用专门的Redis服务进行模式删除
                var l2Removed = await _redisCache.RemoveByPatternAsync(pattern);
                totalRemoved += l2Removed;
                _logger.LogDebug("L2(Redis)缓存删除数量: {Count}", l2Removed);
            }
            else if (_level2Cache != null)
            {
                // 降级方案：标准IDistributedCache不支持模式删除
                _logger.LogWarning("分布式缓存不支持模式删除，仅删除了L1缓存。建议注册IRedisCacheService以支持完整功能。");
            }

            _logger.LogInformation("多级缓存批量删除完成: {Pattern}, 总删除数: {Count}", pattern, totalRemoved);
            return totalRemoved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除缓存时发生错误: {Pattern}", pattern);
            return totalRemoved;
        }
    }

    /// <summary>
    /// 高性能批量删除（使用Redis Lua脚本）
    /// </summary>
    public async Task<int> RemoveByPatternWithLuaAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        var totalRemoved = 0;

        try
        {
            // L1: 批量删除内存缓存
            var l1Removed = await _level1Cache.RemoveByPatternAsync(pattern);
            totalRemoved += l1Removed;

            // L2: 使用Lua脚本进行高性能批量删除
            if (_redisCache is RedisCacheService redisService)
            {
                var l2Removed = await redisService.RemoveByPatternWithLuaAsync(pattern);
                totalRemoved += l2Removed;
                _logger.LogDebug("L2(Redis Lua)缓存删除数量: {Count}", l2Removed);
            }

            _logger.LogInformation("多级缓存Lua批量删除完成: {Pattern}, 总删除数: {Count}", pattern, totalRemoved);
            return totalRemoved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lua批量删除缓存时发生错误: {Pattern}", pattern);
            return totalRemoved;
        }
    }

    /// <summary>
    /// 安全批量删除（使用Redis SCAN，推荐生产环境）
    /// </summary>
    public async Task<int> RemoveByPatternWithScanAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        var totalRemoved = 0;

        try
        {
            // L1: 批量删除内存缓存
            var l1Removed = await _level1Cache.RemoveByPatternAsync(pattern);
            totalRemoved += l1Removed;

            // L2: 使用SCAN命令进行安全批量删除
            if (_redisCache is RedisCacheService redisService)
            {
                var l2Removed = await redisService.RemoveByPatternWithScanAsync(pattern);
                totalRemoved += l2Removed;
                _logger.LogDebug("L2(Redis SCAN)缓存删除数量: {Count}", l2Removed);
            }

            _logger.LogInformation("多级缓存SCAN批量删除完成: {Pattern}, 总删除数: {Count}", pattern, totalRemoved);
            return totalRemoved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SCAN批量删除缓存时发生错误: {Pattern}", pattern);
            return totalRemoved;
        }
    }

    /// <summary>
    /// 获取缓存项的剩余生存时间
    /// </summary>
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
    {
        // 优先从L2缓存获取TTL信息
        if (_redisCache != null)
        {
            // 🔥 使用Redis服务获取精确的TTL
            return await _redisCache.GetTimeToLiveAsync(key);
        }

        return await _level1Cache.GetTimeToLiveAsync(key);
    }

    /// <summary>
    /// 刷新缓存项的过期时间
    /// </summary>
    public async Task<bool> RefreshAsync(string key, TimeSpan expiry)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var success = true;

        try
        {
            // L1: 刷新内存缓存
            var l1Success = await _level1Cache.RefreshAsync(key, expiry);
            if (!l1Success)
            {
                success = false;
            }

            // L2: 刷新分布式缓存
            if (_redisCache != null)
            {
                var l2Success = await _redisCache.RefreshAsync(key, expiry);
                if (!l2Success)
                {
                    success = false;
                }
            }
            else if (_level2Cache != null)
            {
                await _level2Cache.RefreshAsync(key);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新缓存时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 获取缓存层级统计信息
    /// </summary>
    public async Task<Dictionary<string, CacheStatistics>> GetStatisticsAsync()
    {
        try
        {
            // 更新L1统计信息
            var l1Stats = _level1Cache.GetStatistics();
            _levelStatistics["Level1"] = l1Stats;

            // 获取L2统计信息
            if (_redisCache != null)
            {
                var redisInfo = await _redisCache.GetDatabaseInfoAsync();
                if (redisInfo.TryGetValue("Statistics", out var statsObj) && statsObj is CacheStatistics redisStats)
                {
                    _levelStatistics["Level2"] = redisStats;
                }
            }
            else
            {
                _levelStatistics["Level2"].LastUpdated = DateTime.UtcNow;
            }

            return new Dictionary<string, CacheStatistics>(_levelStatistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存统计信息时发生错误");
            return _levelStatistics;
        }
    }

    /// <summary>
    /// 清空指定层级的缓存
    /// </summary>
    public async Task<bool> ClearLevelAsync(string level)
    {
        ArgumentException.ThrowIfNullOrEmpty(level);

        try
        {
            switch (level.ToLower())
            {
                case "level1":
                case "l1":
                case "memory":
                    await _level1Cache.RemoveByPatternAsync("*");
                    _logger.LogInformation("L1缓存已清空");
                    return true;

                case "level2":
                case "l2":
                case "distributed":
                case "redis":
                    if (_redisCache != null)
                    {
                        // 🔥 使用Redis服务清空数据库
                        var result = await _redisCache.FlushDatabaseAsync();
                        if (result)
                        {
                            _logger.LogInformation("L2(Redis)缓存已清空");
                        }
                        return result;
                    }
                    else if (_level2Cache != null)
                    {
                        _logger.LogWarning("分布式缓存清空需要具体实现支持");
                        return false;
                    }
                    break;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空缓存层级时发生错误: {Level}", level);
            return false;
        }
    }

    /// <summary>
    /// 清空所有层级的缓存
    /// </summary>
    public async Task<int> ClearAllAsync()
    {
        var totalCleared = 0;

        try
        {
            // 清空L1缓存
            var l1Cleared = await _level1Cache.RemoveByPatternAsync("*");
            totalCleared += l1Cleared;

            // 清空L2缓存
            if (_redisCache != null)
            {
                // 🔥 使用Redis服务清空所有缓存
                var success = await _redisCache.FlushDatabaseAsync();
                if (success)
                {
                    _logger.LogInformation("L2(Redis)缓存已清空");
                    // Redis清空操作不返回具体数量，这里估算
                    totalCleared += 1000; 
                }
            }
            else if (_level2Cache != null)
            {
                _logger.LogWarning("分布式缓存清空需要具体实现支持");
            }

            _logger.LogInformation("所有缓存层级已清空，总清除项数: {Count}", totalCleared);
            return totalCleared;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空所有缓存时发生错误");
            return totalCleared;
        }
    }

    /// <summary>
    /// 预热缓存
    /// </summary>
    public async Task<int> WarmupAsync<T>(IEnumerable<string> keys, Func<string, Task<T?>> dataLoader) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(dataLoader);

        var warmedCount = 0;
        var keysList = keys.ToList();

        try
        {
            var tasks = keysList.Select(async key =>
            {
                try
                {
                    // 检查是否已存在
                    if (await ExistsAsync(key))
                    {
                        return false;
                    }

                    // 加载数据
                    var data = await dataLoader(key);
                    if (data != null)
                    {
                        await SetAsync(key, data);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "预热缓存项时发生错误: {Key}", key);
                }

                return false;
            });

            var results = await Task.WhenAll(tasks);
            warmedCount = results.Count(success => success);

            _logger.LogInformation("缓存预热完成: {Warmed}/{Total}", warmedCount, keysList.Count);
            return warmedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存预热时发生错误");
            return warmedCount;
        }
    }

    /// <summary>
    /// 从分布式缓存获取数据
    /// </summary>
    private async Task<T?> GetFromDistributedCacheAsync<T>(string key) where T : class
    {
        if (_level2Cache == null) return null;

        try
        {
            var cachedBytes = await _level2Cache.GetAsync(key);
            if (cachedBytes != null)
            {
                var json = System.Text.Encoding.UTF8.GetString(cachedBytes);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从分布式缓存获取数据时发生错误: {Key}", key);
        }

        return null;
    }

    /// <summary>
    /// 向分布式缓存设置数据
    /// </summary>
    private async Task<bool> SetToDistributedCacheAsync<T>(string key, T value, TimeSpan expiry) where T : class
    {
        if (_level2Cache == null) return false;

        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry,
                SlidingExpiration = TimeSpan.FromMinutes(_options.DistributedCache.SlidingExpirationMinutes)
            };

            await _level2Cache.SetAsync(key, bytes, options);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向分布式缓存设置数据时发生错误: {Key}", key);
            return false;
        }
    }
}
