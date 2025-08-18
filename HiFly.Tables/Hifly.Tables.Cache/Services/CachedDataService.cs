// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Cache.Services;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HiFly.Tables.Cache.Services;

/// <summary>
/// 带缓存的数据服务装饰器
/// </summary>
/// <typeparam name="TItem">实体类型</typeparam>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="baseService">基础数据服务</param>
/// <param name="cacheService">缓存服务</param>
/// <param name="keyGenerator">缓存键生成器</param>
/// <param name="logger">日志记录器</param>
public class CachedDataService<TItem>(
    IHiFlyDataService<TItem> baseService,
    IMultiLevelCacheService cacheService,
    TableCacheKeyGenerator keyGenerator,
    ILogger<CachedDataService<TItem>> logger) : IHiFlyDataService<TItem>
    where TItem : class, new()
{
    private readonly IHiFlyDataService<TItem> _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
    private readonly IMultiLevelCacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly TableCacheKeyGenerator _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
    private readonly ILogger<CachedDataService<TItem>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 查询数据（带缓存）
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
                _logger.LogDebug("缓存命中: {CacheKey}", cacheKey);
                return cachedResult;
            }

            _logger.LogDebug("缓存未命中，执行查询: {CacheKey}", cacheKey);

            // 从基础服务查询
            var result = await _baseService.OnQueryAsync(options, propertyFilterParameters, isTree);

            // 缓存结果
            if (result.TotalCount > 0)
            {
                var expiration = _keyGenerator.GetQueryCacheExpiration(typeof(TItem), options);
                await _cacheService.SetAsync(cacheKey, result, expiration);
                _logger.LogDebug("查询结果已缓存: {CacheKey}, 过期时间: {Expiration}", cacheKey, expiration);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询数据时发生错误，实体: {EntityType}", typeof(TItem).Name);
            
            // 发生错误时直接调用基础服务
            return await _baseService.OnQueryAsync(options, propertyFilterParameters, isTree);
        }
    }

    /// <summary>
    /// 删除数据（清理相关缓存）
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
                // 使用智能缓存清理策略
                await SmartCacheClearAsync();
                
                // 延迟验证缓存清理效果
                await VerifyCacheClearingAsync();
                
                _logger.LogDebug("删除成功，已清理 {EntityType} 相关缓存", typeof(TItem).Name);
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
    /// 保存数据（清理相关缓存）
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
                // 增强的缓存清理 - 确保所有级别都被清理
                await EnhancedClearAllRelatedCacheAsync();
                _logger.LogDebug("保存成功，已清理 {EntityType} 相关缓存", typeof(TItem).Name);
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
    /// 预热缓存
    /// </summary>
    /// <param name="queries">查询选项列表</param>
    /// <param name="propertyFilterParameters">属性过滤参数</param>
    /// <param name="isTree">是否为树形结构</param>
    /// <returns>预热成功的查询数量</returns>
    public async Task<int> WarmupCacheAsync(
        IEnumerable<QueryPageOptions> queries,
        PropertyFilterParameters? propertyFilterParameters = null,
        bool isTree = false)
    {
        var successCount = 0;

        foreach (var query in queries)
        {
            try
            {
                // 强制查询以填充缓存
                await OnQueryAsync(query, propertyFilterParameters, isTree);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "预热缓存失败，查询: {@Query}", query);
            }
        }

        _logger.LogInformation("缓存预热完成，成功: {SuccessCount}/{TotalCount}",
            successCount, queries.Count());

        return successCount;
    }

    /// <summary>
    /// 清理相关缓存
    /// </summary>
    private async Task ClearRelatedCacheAsync()
    {
        try
        {
            var pattern = _keyGenerator.GetEntityCachePattern<TItem>();
            await _cacheService.RemoveByPatternAsync(pattern);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理缓存时发生错误，实体: {EntityType}", typeof(TItem).Name);
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计信息</returns>
    public async Task<Dictionary<string, object>> GetCacheStatsAsync()
    {
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            var pattern = _keyGenerator.GetEntityCachePattern<TItem>();

            return new Dictionary<string, object>
            {
                ["EntityType"] = typeof(TItem).Name,
                ["CachePattern"] = pattern,
                ["Statistics"] = stats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存统计信息时发生错误");
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 增强的缓存清理 - 确保所有级别和相关模式都被清理
    /// </summary>
    private async Task EnhancedClearAllRelatedCacheAsync()
    {
        var totalCleared = 0;
        var entityName = typeof(TItem).Name;

        try
        {
            _logger.LogInformation("开始增强缓存清理: {EntityType}", entityName);

            // 1. 获取所有相关的缓存模式
            var patterns = GetAllRelatedCachePatterns();
            
            // 2. 清理每个模式的缓存
            foreach (var pattern in patterns)
            {
                try
                {
                    var cleared = await _cacheService.RemoveByPatternAsync(pattern);
                    totalCleared += cleared;
                    _logger.LogDebug("已清理缓存模式 {Pattern}: {Count} 项", pattern, cleared);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "清理缓存模式失败: {Pattern}", pattern);
                }
            }

            // 3. 强制清理多级缓存的特定级别
            await ForceCleanAllCacheLevelsAsync();

            _logger.LogInformation("增强缓存清理完成: {EntityType}, 总清理项数: {TotalCleared}", 
                entityName, totalCleared);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "增强缓存清理时发生错误: {EntityType}", entityName);
        }
    }

    /// <summary>
    /// 获取所有相关的缓存模式
    /// </summary>
    private List<string> GetAllRelatedCachePatterns()
    {
        var entityName = typeof(TItem).Name;
        var patterns = new List<string>();

        // 1. 基础实体缓存模式
        patterns.Add(_keyGenerator.GetEntityCachePattern<TItem>());

        // 2. 查询缓存模式（不同的查询可能有不同的缓存键）
        patterns.Add($"HiFly:Tables:Query:{entityName}:*");
        
        // 3. 树形缓存模式（如果适用）
        patterns.Add($"HiFly:Tables:Tree:{entityName}:*");
        
        // 4. 实体详情缓存模式
        patterns.Add($"HiFly:Tables:Entity:{entityName}:*");
        
        // 5. 统计缓存模式
        patterns.Add($"HiFly:Tables:Stats:{entityName}:*");

        // 6. 通用实体相关缓存模式
        patterns.Add($"*:{entityName}:*");

        return patterns.Distinct().ToList();
    }

    /// <summary>
    /// 验证缓存清理效果
    /// </summary>
    private async Task VerifyCacheClearingAsync()
    {
        try
        {
            var entityName = typeof(TItem).Name;
            
            // 等待一小段时间让缓存清理完成
            await Task.Delay(100);
            
            // 验证主要缓存模式是否确实被清理
            var mainPattern = $"HiFly:Tables:Query:{entityName}:*";
            
            // 如果是多级缓存服务，检查各级别的统计信息
            if (_cacheService is MultiLevelCacheService multiLevelService)
            {
                var stats = await multiLevelService.GetStatisticsAsync();
                _logger.LogDebug("缓存清理后统计信息: {Stats}", JsonSerializer.Serialize(stats));
            }
            
            _logger.LogDebug("缓存清理验证完成: {EntityType}", entityName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "验证缓存清理时发生错误");
        }
    }

    /// <summary>
    /// 强制清理所有缓存级别
    /// </summary>
    private async Task ForceCleanAllCacheLevelsAsync()
    {
        try
        {
            if (_cacheService is MultiLevelCacheService multiLevelService)
            {
                var entityName = typeof(TItem).Name;
                
                // 获取清理前的统计信息
                var statsBefore = await multiLevelService.GetStatisticsAsync();
                _logger.LogDebug("清理前缓存统计: {EntityType}, 统计: {Stats}",
                    entityName, JsonSerializer.Serialize(statsBefore));
                
                // 使用模式清理而不是清理整个级别
                var patterns = GetAllRelatedCachePatterns();
                foreach (var pattern in patterns)
                {
                    try
                    {
                        var removed = await multiLevelService.RemoveByPatternAsync(pattern);
                        if (removed > 0)
                        {
                            _logger.LogDebug("多级缓存清理模式 {Pattern}: {Count} 项", pattern, removed);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "多级缓存清理模式失败: {Pattern}", pattern);
                    }
                }
                
                // 获取清理后的统计信息
                var statsAfter = await multiLevelService.GetStatisticsAsync();
                _logger.LogDebug("清理后缓存统计: {EntityType}, 统计: {Stats}",
                    entityName, JsonSerializer.Serialize(statsAfter));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "强制清理缓存级别时发生错误");
        }
    }

    /// <summary>
    /// 手动强制清理所有相关缓存
    /// </summary>
    /// <returns>清理是否成功</returns>
    public async Task<bool> ForceClearAllCacheAsync()
    {
        try
        {
            _logger.LogInformation("开始手动强制清理 {EntityType} 所有相关缓存", typeof(TItem).Name);
            await EnhancedClearAllRelatedCacheAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动强制清理缓存失败: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 清理指定缓存键的所有级别
    /// </summary>
    /// <param name="cacheKey">缓存键</param>
    /// <returns>清理是否成功</returns>
    public async Task<bool> ClearSpecificCacheAllLevelsAsync(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheKey);

        try
        {
            _logger.LogDebug("开始清理指定缓存键的所有级别: {CacheKey}", cacheKey);

            // 清理该键在所有级别的缓存
            var removed = await _cacheService.RemoveAsync(cacheKey);
            
            if (_cacheService is MultiLevelCacheService multiLevelService)
            {
                // 确保在所有级别都清理
                await multiLevelService.RemoveAsync(cacheKey);
            }

            _logger.LogDebug("已清理缓存键 {CacheKey}, 结果: {Removed}", cacheKey, removed);
            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理指定缓存键时发生错误: {CacheKey}", cacheKey);
            return false;
        }
    }

    /// <summary>
    /// 紧急缓存清理 - 当普通清理不彻底时使用
    /// </summary>
    /// <returns>清理是否成功</returns>
    public async Task<bool> EmergencyCacheClearAsync()
    {
        try
        {
            var entityName = typeof(TItem).Name;
            _logger.LogWarning("开始紧急缓存清理: {EntityType}", entityName);

            var totalCleared = 0;

            // 1. 清理所有可能的缓存模式
            var patterns = GetAllRelatedCachePatterns();
            
            // 添加更广泛的模式
            patterns.AddRange(new[]
            {
                $"*{entityName}*",  // 包含实体名的所有键
                "HiFly:Tables:*",   // 所有表格相关缓存
                "*:Query:*",        // 所有查询缓存
                "*:Entity:*"        // 所有实体缓存
            });

            foreach (var pattern in patterns.Distinct())
            {
                try
                {
                    var cleared = await _cacheService.RemoveByPatternAsync(pattern);
                    totalCleared += cleared;
                    if (cleared > 0)
                    {
                        _logger.LogInformation("紧急清理模式 {Pattern}: {Count} 项", pattern, cleared);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "紧急清理模式失败: {Pattern}", pattern);
                }
            }

            // 2. 如果是多级缓存，强制清理L1级别
            if (_cacheService is MultiLevelCacheService multiLevelService)
            {
                try
                {
                    await multiLevelService.ClearLevelAsync("L1");
                    _logger.LogInformation("紧急清理: 已清空L1缓存");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "紧急清理L1缓存失败");
                }
            }

            _logger.LogWarning("紧急缓存清理完成: {EntityType}, 总清理: {Total} 项", entityName, totalCleared);
            return totalCleared > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "紧急缓存清理失败: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 智能缓存清理 - 根据清理效果选择策略
    /// </summary>
    private async Task SmartCacheClearAsync()
    {
        try
        {
            var entityName = typeof(TItem).Name;
            
            // 第一步：常规清理
            _logger.LogDebug("开始智能缓存清理第一步：常规清理 {EntityType}", entityName);
            var initialCleared = 0;
            var patterns = GetAllRelatedCachePatterns();
            
            foreach (var pattern in patterns)
            {
                try
                {
                    var cleared = await _cacheService.RemoveByPatternAsync(pattern);
                    initialCleared += cleared;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "常规清理模式失败: {Pattern}", pattern);
                }
            }

            // 第二步：验证清理效果
            await Task.Delay(50);
            
            if (_cacheService is MultiLevelCacheService multiLevelService)
            {
                var stats = await multiLevelService.GetStatisticsAsync();
                
                // 检查是否还有大量缓存项（简化的检查）
                if (stats.Count > 0)
                {
                    var hasMany = stats.Values.Any(stat => stat != null);
                    if (hasMany)
                    {
                        _logger.LogWarning("智能缓存清理：检测到仍有缓存项，启动增强清理");
                        await EmergencyCacheClearAsync();
                    }
                }
            }

            _logger.LogDebug("智能缓存清理完成: {EntityType}, 常规清理: {Cleared} 项", entityName, initialCleared);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "智能缓存清理失败: {EntityType}", typeof(TItem).Name);
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
