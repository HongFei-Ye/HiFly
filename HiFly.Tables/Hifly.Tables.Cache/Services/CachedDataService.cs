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
public class CachedDataService<TItem> : IHiFlyDataService<TItem>
    where TItem : class, new()
{
    private readonly IHiFlyDataService<TItem> _baseService;
    private readonly IMultiLevelCacheService _cacheService;
    private readonly TableCacheKeyGenerator _keyGenerator;
    private readonly ILogger<CachedDataService<TItem>> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="baseService">基础数据服务</param>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="keyGenerator">缓存键生成器</param>
    /// <param name="logger">日志记录器</param>
    public CachedDataService(
        IHiFlyDataService<TItem> baseService,
        IMultiLevelCacheService cacheService,
        TableCacheKeyGenerator keyGenerator,
        ILogger<CachedDataService<TItem>> logger)
    {
        _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
                // 清理相关缓存
                await ClearRelatedCacheAsync();
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
                // 清理相关缓存
                await ClearRelatedCacheAsync();
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
    /// 资源释放
    /// </summary>
    public void Dispose()
    {
        _baseService?.Dispose();
        GC.SuppressFinalize(this);
    }
}
