// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.Extensions.Logging;

namespace HiFly.Tables.Cache.Services;

/// <summary>
/// 轻量级带缓存数据服务装饰器
/// </summary>
/// <typeparam name="TItem">实体类型</typeparam>
/// <remarks>
/// 这是一个简化版本的缓存装饰器，相比 CachedDataService 具有以下特点：
/// - 更简单的错误处理策略
/// - 保守的缓存清理机制
/// - 更好的稳定性和性能
/// - 适合对稳定性要求较高的生产环境
/// </remarks>
public class LightweightCachedDataService<TItem>(
    IHiFlyDataService<TItem> baseService,
    IMultiLevelCacheService cacheService,
    TableCacheKeyGenerator keyGenerator,
    ILogger<LightweightCachedDataService<TItem>> logger) : IHiFlyDataService<TItem>
    where TItem : class, new()
{
    private readonly IHiFlyDataService<TItem> _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
    private readonly IMultiLevelCacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly TableCacheKeyGenerator _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
    private readonly ILogger<LightweightCachedDataService<TItem>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 查询数据（带轻量级缓存）
    /// </summary>
    public async Task<QueryData<TItem>> OnQueryAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters = null,
        bool isTree = false)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            // 生成缓存键
            var cacheKey = _keyGenerator.GenerateQueryKey<TItem>(options, propertyFilterParameters, isTree);

            // 尝试从缓存获取
            var cachedResult = await _cacheService.GetAsync<QueryData<TItem>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("轻量级缓存命中: {EntityType}, Key: {CacheKey}", typeof(TItem).Name, cacheKey);
                return cachedResult;
            }

            _logger.LogDebug("轻量级缓存未命中，执行查询: {EntityType}, Key: {CacheKey}", typeof(TItem).Name, cacheKey);

            // 从基础服务查询
            var result = await _baseService.OnQueryAsync(options, propertyFilterParameters, isTree);

            // 缓存结果（只在有数据时缓存，增加容错处理）
            if (result.TotalCount > 0)
            {
                var expiration = _keyGenerator.GetQueryCacheExpiration(typeof(TItem), options);
                try
                {
                    await _cacheService.SetAsync(cacheKey, result, expiration);
                    _logger.LogDebug("查询结果已缓存: {EntityType}, Key: {CacheKey}", typeof(TItem).Name, cacheKey);
                }
                catch (Exception cacheEx)
                {
                    // 轻量级策略：缓存失败不应该影响查询结果，只记录警告
                    _logger.LogWarning(cacheEx, "缓存设置失败，继续返回查询结果: {EntityType}, Key: {CacheKey}", 
                        typeof(TItem).Name, cacheKey);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询数据时发生错误，实体: {EntityType}", typeof(TItem).Name);
            
            // 轻量级策略：发生错误时直接调用基础服务，确保业务连续性
            return await _baseService.OnQueryAsync(options, propertyFilterParameters, isTree);
        }
    }

    /// <summary>
    /// 保存数据（轻量级缓存清理）
    /// </summary>
    public async Task<bool> OnSaveAsync(TItem item, ItemChangedType changedType)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            // 执行保存操作
            var result = await _baseService.OnSaveAsync(item, changedType);

            if (result)
            {
                // 轻量级缓存清理：只清理当前实体相关的核心缓存
                await LightweightClearEntityCacheAsync();
                _logger.LogDebug("保存成功，已执行轻量级缓存清理: {EntityType}", typeof(TItem).Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存数据时发生错误，实体: {EntityType}, 变更类型: {ChangeType}",
                typeof(TItem).Name, changedType);
            throw;
        }
    }

    /// <summary>
    /// 删除数据（轻量级缓存清理）
    /// </summary>
    public async Task<bool> OnDeleteAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        try
        {
            // 执行删除操作
            var result = await _baseService.OnDeleteAsync(items);

            if (result)
            {
                // 轻量级缓存清理：只清理当前实体相关的核心缓存
                await LightweightClearEntityCacheAsync();
                _logger.LogDebug("删除成功，已执行轻量级缓存清理: {EntityType}", typeof(TItem).Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除数据时发生错误，实体: {EntityType}", typeof(TItem).Name);
            throw;
        }
    }

    /// <summary>
    /// 轻量级实体缓存清理
    /// </summary>
    /// <remarks>
    /// 采用保守的清理策略，只清理最核心的缓存模式，避免：
    /// - 过度清理导致的性能问题
    /// - 复杂的清理逻辑可能引入的错误
    /// - 不必要的系统资源消耗
    /// </remarks>
    private async Task LightweightClearEntityCacheAsync()
    {
        try
        {
            var entityName = typeof(TItem).Name;
            
            // 轻量级策略：只清理最基本和最重要的缓存模式
            var corePatterns = new[]
            {
                $"HiFly:Tables:Query:{entityName}:*", // 查询缓存
                _keyGenerator.GetEntityCachePattern<TItem>() // 实体缓存
            };

            var totalCleared = 0;
            foreach (var pattern in corePatterns)
            {
                try
                {
                    var cleared = await _cacheService.RemoveByPatternAsync(pattern);
                    totalCleared += cleared;
                    
                    if (cleared > 0)
                    {
                        _logger.LogDebug("轻量级清理缓存模式 {Pattern}: {Count} 项", pattern, cleared);
                    }
                }
                catch (Exception ex)
                {
                    // 轻量级策略：缓存清理失败不应该影响数据操作，只记录警告
                    _logger.LogWarning(ex, "轻量级缓存清理失败，模式: {Pattern}", pattern);
                }
            }

            if (totalCleared > 0)
            {
                _logger.LogInformation("轻量级缓存清理完成: {EntityType}, 总清理: {Count} 项", entityName, totalCleared);
            }
        }
        catch (Exception ex)
        {
            // 轻量级策略：即使缓存清理完全失败，也不影响业务操作
            _logger.LogWarning(ex, "轻量级缓存清理时发生错误: {EntityType}", typeof(TItem).Name);
        }
    }

    /// <summary>
    /// 手动清理所有相关缓存（轻量级版本）
    /// </summary>
    /// <returns>清理是否成功</returns>
    public async Task<bool> ClearAllCacheAsync()
    {
        try
        {
            _logger.LogInformation("开始手动轻量级缓存清理: {EntityType}", typeof(TItem).Name);
            await LightweightClearEntityCacheAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动轻量级缓存清理失败: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 获取缓存状态信息（轻量级版本）
    /// </summary>
    /// <returns>基本缓存状态信息</returns>
    public async Task<Dictionary<string, object>> GetCacheStatusAsync()
    {
        try
        {
            var entityName = typeof(TItem).Name;
            var status = new Dictionary<string, object>
            {
                ["EntityType"] = entityName,
                ["CacheType"] = "Lightweight",
                ["EntityPattern"] = _keyGenerator.GetEntityCachePattern<TItem>(),
                ["QueryPattern"] = $"HiFly:Tables:Query:{entityName}:*"
            };

            // 尝试获取缓存统计信息
            try
            {
                var stats = await _cacheService.GetStatisticsAsync();
                status["Statistics"] = stats;
            }
            catch (Exception ex)
            {
                status["StatisticsError"] = ex.Message;
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取轻量级缓存状态时发生错误: {EntityType}", typeof(TItem).Name);
            return new Dictionary<string, object>
            {
                ["Error"] = ex.Message,
                ["EntityType"] = typeof(TItem).Name
            };
        }
    }

    /// <summary>
    /// 资源释放
    /// </summary>
    public void Dispose()
    {
        _baseService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
