// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HiFly.Tables.Cache.Services;

/// <summary>
/// 内存缓存服务实现（简化版本，仅支持内存缓存）
/// </summary>
public class MultiLevelCacheService : IMultiLevelCacheService
{
    private readonly MemoryCacheService _memoryCache;
    private readonly ILogger<MultiLevelCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly Dictionary<string, CacheStatistics> _levelStatistics;

    public MultiLevelCacheService(
        MemoryCacheService memoryCacheService,
        ILogger<MultiLevelCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCacheService ?? throw new ArgumentNullException(nameof(memoryCacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        _levelStatistics = new Dictionary<string, CacheStatistics>
        {
            ["Memory"] = new CacheStatistics()
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
            var result = await _memoryCache.GetAsync<T>(key);
            if (result != null)
            {
                _levelStatistics["Memory"].HitCount++;
                _logger.LogDebug("内存缓存命中: {Key}", key);
                return result;
            }

            _levelStatistics["Memory"].MissCount++;
            _logger.LogDebug("内存缓存未命中: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存时发生错误: {Key}", key);
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
            var cacheExpiry = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
            var success = await _memoryCache.SetAsync(key, value, cacheExpiry);
            
            if (success)
            {
                _logger.LogDebug("内存缓存写入成功: {Key}", key);
            }
            else
            {
                _logger.LogWarning("内存缓存写入失败: {Key}", key);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存时发生错误: {Key}", key);
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
            var success = await _memoryCache.RemoveAsync(key);
            _logger.LogDebug("内存缓存删除: {Key}, 成功: {Success}", key, success);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除缓存时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 检查缓存项是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return await _memoryCache.ExistsAsync(key);
    }

    /// <summary>
    /// 批量删除缓存项
    /// </summary>
    public async Task<int> RemoveByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        try
        {
            var removed = await _memoryCache.RemoveByPatternAsync(pattern);
            _logger.LogInformation("内存缓存批量删除完成: {Pattern}, 删除数: {Count}", pattern, removed);
            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除缓存时发生错误: {Pattern}", pattern);
            return 0;
        }
    }

    /// <summary>
    /// 获取缓存项的剩余生存时间
    /// </summary>
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
    {
        return await _memoryCache.GetTimeToLiveAsync(key);
    }

    /// <summary>
    /// 刷新缓存项的过期时间
    /// </summary>
    public async Task<bool> RefreshAsync(string key, TimeSpan expiry)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        try
        {
            return await _memoryCache.RefreshAsync(key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新缓存时发生错误: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public async Task<Dictionary<string, CacheStatistics>> GetStatisticsAsync()
    {
        try
        {
            var memoryStats = _memoryCache.GetStatistics();
            _levelStatistics["Memory"] = memoryStats;

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
            if (level.ToLower() is "memory" or "level1" or "l1")
            {
                await _memoryCache.RemoveByPatternAsync("*");
                _logger.LogInformation("内存缓存已清空");
                return true;
            }

            _logger.LogWarning("未知的缓存层级: {Level}", level);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空缓存层级时发生错误: {Level}", level);
            return false;
        }
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public async Task<int> ClearAllAsync()
    {
        try
        {
            var cleared = await _memoryCache.RemoveByPatternAsync("*");
            _logger.LogInformation("所有内存缓存已清空，清除项数: {Count}", cleared);
            return cleared;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空所有缓存时发生错误");
            return 0;
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
    /// 获取所有缓存键
    /// </summary>
    /// <returns>所有缓存键列表</returns>
    public async Task<List<string>> GetAllKeysAsync()
    {
        try
        {
            return await _memoryCache.GetAllKeysAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有缓存键时发生错误");
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取匹配模式的缓存键
    /// </summary>
    /// <param name="pattern">模式，支持通配符*</param>
    /// <returns>匹配的缓存键列表</returns>
    public async Task<List<string>> GetKeysByPatternAsync(string pattern)
    {
        try
        {
            return await _memoryCache.GetKeysByPatternAsync(pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存键时发生错误: {Pattern}", pattern);
            return new List<string>();
        }
    }
}
