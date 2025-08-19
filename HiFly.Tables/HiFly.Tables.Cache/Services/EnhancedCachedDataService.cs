// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HiFly.Tables.Cache.Services;

/// <summary>
/// 增强版带缓存数据服务装饰器
/// </summary>
/// <typeparam name="TItem">实体类型</typeparam>
/// <remarks>
/// 这是一个功能完整的缓存装饰器，相比基础版本具有以下特点：
/// - 自动并发冲突处理和重试机制
/// - 指数退避策略
/// - 优化的缓存清理策略
/// - 完善的错误处理和日志记录
/// - 适合对数据一致性要求较高的生产环境
/// </remarks>
public class EnhancedCachedDataService<TItem>(
    IHiFlyDataService<TItem> baseService,
    IMultiLevelCacheService cacheService,
    TableCacheKeyGenerator keyGenerator,
    ILogger<EnhancedCachedDataService<TItem>> logger) : IHiFlyDataService<TItem>
    where TItem : class, new()
{
    private readonly IHiFlyDataService<TItem> _baseService = baseService ?? throw new ArgumentNullException(nameof(baseService));
    private readonly IMultiLevelCacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly TableCacheKeyGenerator _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
    private readonly ILogger<EnhancedCachedDataService<TItem>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 查询数据（带智能缓存）
    /// </summary>
    public async Task<QueryData<TItem>> OnQueryAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters = null,
        bool isTree = false)
    {
        ArgumentNullException.ThrowIfNull(options);

        // 1. 生成缓存键
        var cacheKey = GenerateCacheKey(options, propertyFilterParameters, isTree);

        try
        {
            // 2. 尝试从缓存获取
            var cachedResult = await TryGetFromCacheAsync(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("✅ 增强缓存命中: {EntityType}, Key: {CacheKey}", typeof(TItem).Name, cacheKey);
                return cachedResult;
            }

            // 3. 缓存未命中，从数据库查询
            _logger.LogDebug("❌ 增强缓存未命中，查询数据库: {EntityType}, Key: {CacheKey}", typeof(TItem).Name, cacheKey);
            var result = await _baseService.OnQueryAsync(options, propertyFilterParameters, isTree);

            // 4. 缓存查询结果
            await TryCacheResultAsync(cacheKey, result, options);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询数据时发生错误，实体: {EntityType}", typeof(TItem).Name);
            // 出错时直接调用基础服务，不缓存错误结果
            return await _baseService.OnQueryAsync(options, propertyFilterParameters, isTree);
        }
    }

    /// <summary>
    /// 保存数据（带并发冲突处理和同步缓存清理）
    /// </summary>
    public async Task<bool> OnSaveAsync(TItem item, ItemChangedType changedType)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            // 1. 执行保存操作，使用并发冲突处理
            var result = await ExecuteSaveWithConcurrencyHandlingAsync(item, changedType);

            // 2. 保存成功后，同步清理相关缓存（确保在返回前完成）
            if (result)
            {
                await OptimizedClearRelatedCacheAsync("Save");
                _logger.LogInformation("💾 保存成功，缓存已清理: {EntityType}", typeof(TItem).Name);
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
    /// 删除数据（带并发冲突处理和同步缓存清理）
    /// </summary>
    public async Task<bool> OnDeleteAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        try
        {
            // 1. 执行删除操作，使用并发冲突处理
            var result = await ExecuteDeleteWithConcurrencyHandlingAsync(items);

            // 2. 删除成功后，同步清理相关缓存（确保在返回前完成）
            if (result)
            {
                await OptimizedClearRelatedCacheAsync("Delete");
                _logger.LogInformation("🗑️ 删除成功，缓存已清理: {EntityType}", typeof(TItem).Name);
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
    /// 生成缓存键
    /// </summary>
    private string GenerateCacheKey(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters,
        bool isTree)
    {
        try
        {
            return _keyGenerator.GenerateQueryKey<TItem>(options, propertyFilterParameters, isTree);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "生成缓存键失败，使用简化键: {EntityType}", typeof(TItem).Name);
            // 使用简化的缓存键作为后备方案
            return $"HiFly:Tables:Query:{typeof(TItem).Name}:{options.PageIndex}:{options.PageItems}";
        }
    }

    /// <summary>
    /// 尝试从缓存获取数据
    /// </summary>
    private async Task<QueryData<TItem>?> TryGetFromCacheAsync(string cacheKey)
    {
        try
        {
            return await _cacheService.GetAsync<QueryData<TItem>>(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从缓存获取数据失败: {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// 尝试缓存查询结果
    /// </summary>
    private async Task TryCacheResultAsync(string cacheKey, QueryData<TItem> result, QueryPageOptions options)
    {
        try
        {
            // 只缓存有效的查询结果
            if (result.TotalCount > 0 && result.Items.Any())
            {
                var expiration = GetCacheExpiration(options);
                await _cacheService.SetAsync(cacheKey, result, expiration);
                _logger.LogDebug("📦 结果已缓存: {EntityType}, Key: {CacheKey}, Count: {Count}", 
                    typeof(TItem).Name, cacheKey, result.Items.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "缓存查询结果失败: {CacheKey}", cacheKey);
            // 缓存失败不影响查询结果
        }
    }

    /// <summary>
    /// 获取缓存过期时间
    /// </summary>
    private TimeSpan GetCacheExpiration(QueryPageOptions options)
    {
        try
        {
            return _keyGenerator.GetQueryCacheExpiration(typeof(TItem), options);
        }
        catch
        {
            // 默认15分钟过期
            return TimeSpan.FromMinutes(15);
        }
    }

    /// <summary>
    /// 优化的相关缓存清理（平衡性能与彻底性）
    /// </summary>
    private async Task OptimizedClearRelatedCacheAsync(string operation)
    {
        try
        {
            var entityName = typeof(TItem).Name;
            _logger.LogDebug("🧹 开始优化缓存清理: {EntityType}, 操作: {Operation}", entityName, operation);

            var startTime = DateTime.Now;

            // 使用精确的缓存模式，避免过度清理
            var patterns = new[]
            {
                $"HiFly:Tables:Query:{entityName}:*", // 查询缓存
                _keyGenerator.GetEntityCachePattern<TItem>() // 实体缓存
            };

            var totalCleared = 0;
            var clearTasks = new List<Task<int>>();

            // 并行清理多个模式，提高速度
            foreach (var pattern in patterns)
            {
                clearTasks.Add(ClearSinglePatternAsync(pattern));
            }

            var clearResults = await Task.WhenAll(clearTasks);
            totalCleared = clearResults.Sum();

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            _logger.LogInformation("✨ 优化缓存清理完成: {EntityType}, 总清理: {Total} 项, 耗时: {Duration}ms", 
                entityName, totalCleared, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "优化缓存清理失败: {EntityType}", typeof(TItem).Name);
        }
    }

    /// <summary>
    /// 清理单个缓存模式
    /// </summary>
    private async Task<int> ClearSinglePatternAsync(string pattern)
    {
        try
        {
            var cleared = await _cacheService.RemoveByPatternAsync(pattern);
            if (cleared > 0)
            {
                _logger.LogDebug("🧽 清理缓存模式: {Pattern}, 数量: {Count}", pattern, cleared);
            }
            return cleared;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理缓存模式失败: {Pattern}", pattern);
            return 0;
        }
    }

    /// <summary>
    /// 执行带并发处理的保存操作
    /// </summary>
    private async Task<bool> ExecuteSaveWithConcurrencyHandlingAsync(TItem item, ItemChangedType changedType)
    {
        const int maxRetries = 3;
        var currentRetry = 0;

        while (currentRetry < maxRetries)
        {
            try
            {
                return await _baseService.OnSaveAsync(item, changedType);
            }
            catch (Exception ex) when (IsConcurrencyException(ex))
            {
                currentRetry++;
                _logger.LogWarning("保存操作发生并发冲突（重试 {Retry}/{MaxRetries}): {EntityType}", 
                    currentRetry, maxRetries, typeof(TItem).Name);

                if (currentRetry >= maxRetries)
                {
                    _logger.LogError("保存操作达到最大重试次数: {EntityType}", typeof(TItem).Name);
                    throw;
                }

                // 清理缓存，确保下次获取最新数据
                await OptimizedClearRelatedCacheAsync("ConcurrencyRetry");

                // 指数退避延迟
                var delay = CalculateBackoffDelay(currentRetry);
                await Task.Delay(delay);
            }
        }

        return false;
    }

    /// <summary>
    /// 执行带并发处理的删除操作
    /// </summary>
    private async Task<bool> ExecuteDeleteWithConcurrencyHandlingAsync(IEnumerable<TItem> items)
    {
        const int maxRetries = 3;
        var currentRetry = 0;

        while (currentRetry < maxRetries)
        {
            try
            {
                return await _baseService.OnDeleteAsync(items);
            }
            catch (Exception ex) when (IsConcurrencyException(ex))
            {
                currentRetry++;
                _logger.LogWarning("删除操作发生并发冲突（重试 {Retry}/{MaxRetries}): {EntityType}", 
                    currentRetry, maxRetries, typeof(TItem).Name);

                if (currentRetry >= maxRetries)
                {
                    _logger.LogError("删除操作达到最大重试次数: {EntityType}", typeof(TItem).Name);
                    
                    // 对于删除操作，并发冲突可能意味着数据已被删除
                    // 清理缓存并返回成功
                    await OptimizedClearRelatedCacheAsync("ConcurrencyDelete");
                    return true;
                }

                // 清理缓存，确保下次获取最新数据
                await OptimizedClearRelatedCacheAsync("ConcurrencyRetry");

                // 指数退避延迟
                var delay = CalculateBackoffDelay(currentRetry);
                await Task.Delay(delay);
            }
        }

        return false;
    }

    /// <summary>
    /// 判断是否为并发冲突异常
    /// </summary>
    private static bool IsConcurrencyException(Exception ex)
    {
        return ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase) || 
               ex.Message.Contains("并发", StringComparison.OrdinalIgnoreCase) ||
               ex.GetType().Name.Contains("Concurrency", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("version", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("timestamp", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 计算指数退避延迟
    /// </summary>
    private static TimeSpan CalculateBackoffDelay(int retryCount)
    {
        // 基础延迟: 100ms, 200ms, 400ms
        var baseDelayMs = 100 * Math.Pow(2, retryCount - 1);
        
        // 添加随机抖动，避免惊群效应
        var jitter = Random.Shared.Next(0, (int)(baseDelayMs * 0.1));
        
        return TimeSpan.FromMilliseconds(baseDelayMs + jitter);
    }

    /// <summary>
    /// 手动清理所有相关缓存
    /// </summary>
    public async Task<bool> ManualClearCacheAsync()
    {
        try
        {
            await OptimizedClearRelatedCacheAsync("Manual");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手动清理缓存失败: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetCacheStatsAsync()
    {
        try
        {
            var stats = await _cacheService.GetStatisticsAsync();
            var pattern = _keyGenerator.GetEntityCachePattern<TItem>();

            return new Dictionary<string, object>
            {
                ["EntityType"] = typeof(TItem).Name,
                ["CacheType"] = "Enhanced",
                ["EntityPattern"] = pattern,
                ["QueryPattern"] = $"HiFly:Tables:Query:{typeof(TItem).Name}:*",
                ["Statistics"] = stats
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存统计信息时发生错误");
            return new Dictionary<string, object>
            {
                ["Error"] = ex.Message,
                ["EntityType"] = typeof(TItem).Name,
                ["CacheType"] = "Enhanced"
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
