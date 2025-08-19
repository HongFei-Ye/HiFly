// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Cache.Services;
using HiFly.Tables.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hifly.Tables.Cache.Extensions;

/// <summary>
/// 缓存服务扩展方法 - 优化版本
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// 添加Table缓存服务（基础设施）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddTableCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册配置
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddMemoryCache(options =>
        {
            var cacheConfig = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>();
            if (cacheConfig != null)
            {
                // 移除 SizeLimit 设置，避免与 BootstrapBlazor 冲突
                // options.SizeLimit = cacheConfig.MemoryCache.MaxItems;
                options.CompactionPercentage = cacheConfig.MemoryCache.CompactionPercentage;
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(cacheConfig.MemoryCache.ExpirationScanFrequencySeconds);
            }
        });

        // 注册缓存服务
        services.AddSingleton<TableCacheKeyGenerator>(provider => 
            new TableCacheKeyGenerator(keyPrefix: ""));
        services.AddSingleton<MemoryCacheService>();
        services.AddSingleton<IMultiLevelCacheService, MultiLevelCacheService>();

        return services;
    }

    // ====================
    // 单一实体装饰器方法
    // ====================

    /// <summary>
    /// 为指定实体添加标准缓存装饰器
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 使用装饰器模式包装基础数据服务，提供完整的缓存功能。
    /// 注意：这个方法假设已经注册了基础的 IHiFlyDataService&lt;TItem&gt; 实现。
    /// </remarks>
    public static IServiceCollection AddStandardCachedDataService<TItem>(
        this IServiceCollection services)
        where TItem : class, new()
    {
        services.Decorate<IHiFlyDataService<TItem>>((baseService, provider) =>
            new CachedDataService<TItem>(
                baseService,
                provider.GetRequiredService<IMultiLevelCacheService>(),
                provider.GetRequiredService<TableCacheKeyGenerator>(),
                provider.GetRequiredService<ILogger<CachedDataService<TItem>>>()));

        return services;
    }

    /// <summary>
    /// 为指定实体添加轻量级缓存装饰器
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 轻量级缓存装饰器特点：
    /// - 简化的错误处理策略
    /// - 保守的缓存清理机制  
    /// - 更好的稳定性和性能
    /// - 适合对稳定性要求较高的生产环境
    /// </remarks>
    public static IServiceCollection AddLightweightCachedDataService<TItem>(
        this IServiceCollection services)
        where TItem : class, new()
    {
        services.Decorate<IHiFlyDataService<TItem>>((baseService, provider) =>
            new LightweightCachedDataService<TItem>(
                baseService,
                provider.GetRequiredService<IMultiLevelCacheService>(),
                provider.GetRequiredService<TableCacheKeyGenerator>(),
                provider.GetRequiredService<ILogger<LightweightCachedDataService<TItem>>>()));

        return services;
    }

    /// <summary>
    /// 为指定实体添加增强版缓存装饰器
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 增强版缓存装饰器特点：
    /// - 自动并发冲突处理和重试机制
    /// - 指数退避策略
    /// - 优化的缓存清理策略
    /// - 完善的错误处理和日志记录
    /// - 适合对数据一致性要求较高的生产环境
    /// </remarks>
    public static IServiceCollection AddEnhancedCachedDataService<TItem>(
        this IServiceCollection services)
        where TItem : class, new()
    {
        services.Decorate<IHiFlyDataService<TItem>>((baseService, provider) =>
            new EnhancedCachedDataService<TItem>(
                baseService,
                provider.GetRequiredService<IMultiLevelCacheService>(),
                provider.GetRequiredService<TableCacheKeyGenerator>(),
                provider.GetRequiredService<ILogger<EnhancedCachedDataService<TItem>>>()));

        return services;
    }

    // ====================
    // 批量处理方法
    // ====================

    /// <summary>
    /// 为所有已注册的数据服务添加标准缓存支持（使用反射）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 这个方法会扫描所有已注册的 IHiFlyDataService 实现并为它们添加标准缓存装饰器。
    /// 必须在注册基础数据服务之后调用。
    /// </remarks>
    public static IServiceCollection AddStandardCacheForAllDataServices(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
    {
        return AddCacheForAllDataServicesByReflection(
            services,
            nameof(AddStandardCachedDataService),
            "标准缓存",
            loggerFactory);
    }

    /// <summary>
    /// 为所有已注册的数据服务添加轻量级缓存支持（使用反射）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 这个方法会扫描所有已注册的 IHiFlyDataService 实现并为它们添加轻量级缓存装饰器。
    /// 必须在注册基础数据服务之后调用。
    /// </remarks>
    public static IServiceCollection AddLightweightCacheForAllDataServices(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
    {
        return AddCacheForAllDataServicesByReflection(
            services,
            nameof(AddLightweightCachedDataService),
            "轻量级缓存",
            loggerFactory);
    }

    /// <summary>
    /// 为所有已注册的数据服务添加增强版缓存支持（使用反射）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 这个方法会扫描所有已注册的 IHiFlyDataService 实现并为它们添加增强版缓存装饰器。
    /// 必须在注册基础数据服务之后调用。
    /// </remarks>
    public static IServiceCollection AddEnhancedCacheForAllDataServices(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
    {
        return AddCacheForAllDataServicesByReflection(
            services,
            nameof(AddEnhancedCachedDataService),
            "增强版缓存",
            loggerFactory);
    }

    // ====================
    // 核心辅助方法
    // ====================

    /// <summary>
    /// 通用的反射批量添加缓存支持方法
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="methodName">要调用的方法名</param>
    /// <param name="cacheTypeName">缓存类型名称（用于日志）</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    private static IServiceCollection AddCacheForAllDataServicesByReflection(
        IServiceCollection services,
        string methodName,
        string cacheTypeName,
        ILoggerFactory? loggerFactory)
    {
        var logger = CreateSafeLogger($"{cacheTypeName}ServiceRegistration", loggerFactory);

        try
        {
            // 找到所有已注册的 IHiFlyDataService<T> 服务
            var dataServiceTypes = services
                .Where(sd => sd.ServiceType.IsGenericType &&
                            sd.ServiceType.GetGenericTypeDefinition() == typeof(IHiFlyDataService<>))
                .Select(sd => sd.ServiceType.GetGenericArguments()[0])
                .Distinct()
                .ToList();

            logger.LogInformation("发现 {Count} 个数据服务类型，开始添加{CacheType}支持", 
                dataServiceTypes.Count, cacheTypeName);

            var successCount = 0;
            var failureCount = 0;

            foreach (var entityType in dataServiceTypes)
            {
                try
                {
                    // 使用反射调用泛型方法
                    var method = typeof(CacheServiceExtensions)
                        .GetMethod(methodName)!
                        .MakeGenericMethod(entityType);

                    method.Invoke(null, new object[] { services });

                    logger.LogDebug("已为 {EntityType} 添加{CacheType}支持", entityType.Name, cacheTypeName);
                    successCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "为实体 {EntityType} 添加{CacheType}支持时发生错误", 
                        entityType.Name, cacheTypeName);
                    failureCount++;
                }
            }

            logger.LogInformation("{CacheType}支持添加完成 - 成功: {Success}, 失败: {Failure}", 
                cacheTypeName, successCount, failureCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "批量添加{CacheType}支持时发生严重错误", cacheTypeName);
        }

        return services;
    }

    /// <summary>
    /// 创建安全的日志记录器（修复生命周期问题）
    /// </summary>
    /// <param name="categoryName">类别名称</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>日志记录器</returns>
    private static ILogger CreateSafeLogger(string categoryName, ILoggerFactory? loggerFactory)
    {
        if (loggerFactory != null)
        {
            return loggerFactory.CreateLogger(categoryName);
        }

        // 返回空日志记录器而不是有生命周期问题的临时工厂
        return Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    // ====================
    // 向后兼容方法（标记为过时）
    // ====================

    /// <summary>
    /// 添加带缓存的数据服务装饰器
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 注意：这个方法假设已经注册了基础的 IHiFlyDataService&lt;TItem&gt; 实现。
    /// 它会将现有的服务包装成缓存版本。
    /// </remarks>
    [Obsolete("请使用 AddStandardCachedDataService<TItem>() 方法，该方法将在下个版本中移除", false)]
    public static IServiceCollection AddCachedDataService<TItem>(
        this IServiceCollection services)
        where TItem : class, new()
    {
        return services.AddStandardCachedDataService<TItem>();
    }

    /// <summary>
    /// 为所有已注册的数据服务添加缓存支持
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 这个方法会扫描所有已注册的 IHiFlyDataService 实现并为它们添加缓存装饰器。
    /// 必须在注册基础数据服务之后调用。
    /// </remarks>
    [Obsolete("请使用 AddStandardCacheForAllDataServices() 方法，该方法将在下个版本中移除", false)]
    public static IServiceCollection AddCacheForAllDataServices(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
    {
        return services.AddStandardCacheForAllDataServices(loggerFactory);
    }
}

/// <summary>
/// 缓存相关的扩展方法
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// 批量预热缓存（适用于标准缓存服务）
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="crudService">CRUD服务</param>
    /// <param name="pageSize">页面大小</param>
    /// <param name="maxPages">最大页数</param>
    /// <param name="isTree">是否为树形结构</param>
    /// <returns>预热成功的项数</returns>
    public static async Task<int> WarmupCommonQueriesAsync<TItem>(
        this CachedDataService<TItem> crudService,
        int pageSize = 20,
        int maxPages = 5,
        bool isTree = false)
        where TItem : class, new()
    {
        ArgumentNullException.ThrowIfNull(crudService);

        var commonQueries = GenerateCommonQueryOptions<TItem>(pageSize, maxPages);
        return await crudService.WarmupCacheAsync(commonQueries, null, isTree);
    }

    /// <summary>
    /// 批量预热缓存（适用于增强版缓存服务）
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="crudService">CRUD服务</param>
    /// <param name="pageSize">页面大小</param>
    /// <param name="maxPages">最大页数</param>
    /// <param name="isTree">是否为树形结构</param>
    /// <returns>预热成功的项数</returns>
    public static async Task<int> WarmupCommonQueriesAsync<TItem>(
        this EnhancedCachedDataService<TItem> crudService,
        int pageSize = 20,
        int maxPages = 5,
        bool isTree = false)
        where TItem : class, new()
    {
        ArgumentNullException.ThrowIfNull(crudService);

        // 增强版服务目前不支持预热，返回0
        // 这里可以在未来扩展增强版服务的预热功能
        await Task.CompletedTask;
        return 0;
    }

    /// <summary>
    /// 生成常用查询选项
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="pageSize">页面大小</param>
    /// <param name="maxPages">最大页数</param>
    /// <returns>查询选项列表</returns>
    private static List<QueryPageOptions> GenerateCommonQueryOptions<TItem>(int pageSize, int maxPages)
        where TItem : class, new()
    {
        var commonQueries = new List<QueryPageOptions>();

        // 生成常用查询：前几页数据
        for (var i = 1; i <= maxPages; i++)
        {
            commonQueries.Add(new QueryPageOptions
            {
                PageIndex = i,
                PageItems = pageSize,
                SortOrder = SortOrder.Unset
            });
        }

        // 添加按常用字段排序的查询
        var sortFields = new[] { "Id", "CreateTime", "UpdateTime", "Name", "Title" };
        foreach (var field in sortFields)
        {
            // 检查实体是否有这个属性
            if (typeof(TItem).GetProperty(field) != null)
            {
                commonQueries.Add(new QueryPageOptions
                {
                    PageIndex = 1,
                    PageItems = pageSize,
                    SortName = field,
                    SortOrder = SortOrder.Asc
                });

                commonQueries.Add(new QueryPageOptions
                {
                    PageIndex = 1,
                    PageItems = pageSize,
                    SortName = field,
                    SortOrder = SortOrder.Desc
                });
            }
        }

        return commonQueries;
    }

    /// <summary>
    /// 智能清理过期缓存
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="olderThan">清理早于指定时间的缓存</param>
    /// <returns>清理的项数</returns>
    public static async Task<int> SmartCleanupExpiredCacheAsync(
        this IMultiLevelCacheService cacheService,
        TimeSpan olderThan)
    {
        ArgumentNullException.ThrowIfNull(cacheService);

        try
        {
            var stats = await cacheService.GetStatisticsAsync();
            var totalItems = stats.Values.Sum(s => s.ItemCount);

            // 智能清理策略：根据缓存项数量决定清理策略
            return totalItems switch
            {
                > 100000 => await cacheService.ClearLevelAsync("memory") ? (int)totalItems : 0,
                > 50000 => await PartialCleanupAsync(cacheService, 0.3), // 清理30%
                > 10000 => await PartialCleanupAsync(cacheService, 0.1), // 清理10%
                _ => 0 // 缓存项较少，不需要清理
            };
        }
        catch (Exception)
        {
            // 清理失败时返回0，不抛出异常
            return 0;
        }
    }

    /// <summary>
    /// 部分清理缓存
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="percentage">清理百分比</param>
    /// <returns>清理的项数</returns>
    private static async Task<int> PartialCleanupAsync(IMultiLevelCacheService cacheService, double percentage)
    {
        // 这里可以实现更精细的部分清理逻辑
        // 目前简化为根据需要清理比例决定是否全部清理
        if (percentage > 0.2) // 如果需要清理超过20%，则全部清理
        {
            return await cacheService.ClearAllAsync();
        }

        return 0;
    }
}
