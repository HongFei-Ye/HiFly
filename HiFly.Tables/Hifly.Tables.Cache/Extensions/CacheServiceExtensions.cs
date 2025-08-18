// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Cache.Services;
using HiFly.Tables.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 缓存服务扩展方法
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// 添加Table缓存服务
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
        services.AddSingleton<TableCacheKeyGenerator>();
        services.AddSingleton<MemoryCacheService>();
        services.AddSingleton<IMultiLevelCacheService, MultiLevelCacheService>();

        // 根据配置决定是否启用分布式缓存
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>();
        if (cacheOptions?.EnableDistributedCache == true && !string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.RedisConnectionString;
                options.InstanceName = "HiFly.Tables";
            });
        }

        return services;
    }

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
    public static IServiceCollection AddCachedDataService<TItem>(
        this IServiceCollection services)
        where TItem : class, new()
    {
        // 使用装饰器模式，将带缓存的服务包装现有服务
        services.Decorate<IHiFlyDataService<TItem>>((baseService, provider) =>
            new CachedDataService<TItem>(
                baseService,
                provider.GetRequiredService<IMultiLevelCacheService>(),
                provider.GetRequiredService<TableCacheKeyGenerator>(),
                provider.GetRequiredService<ILogger<CachedDataService<TItem>>>()));

        return services;
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
    public static IServiceCollection AddCacheForAllDataServices(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger("CachedServiceRegistration") ??
                     CreateConsoleLogger("CachedServiceRegistration");

        // 找到所有已注册的 IHiFlyDataService<T> 服务
        var dataServiceTypes = services
            .Where(sd => sd.ServiceType.IsGenericType &&
                        sd.ServiceType.GetGenericTypeDefinition() == typeof(IHiFlyDataService<>))
            .Select(sd => sd.ServiceType.GetGenericArguments()[0])
            .Distinct()
            .ToList();

        logger.LogInformation("发现 {Count} 个数据服务类型，开始添加缓存支持", dataServiceTypes.Count);

        foreach (var entityType in dataServiceTypes)
        {
            try
            {
                // 使用反射调用泛型方法
                var method = typeof(CacheServiceExtensions)
                    .GetMethod(nameof(AddCachedDataService))!
                    .MakeGenericMethod(entityType);

                method.Invoke(null, new object[] { services });

                logger.LogInformation("已为 {EntityType} 添加缓存支持", entityType.Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "为实体 {EntityType} 添加缓存支持时发生错误", entityType.Name);
            }
        }

        logger.LogInformation("缓存支持添加完成");
        return services;
    }

    /// <summary>
    /// 为所有已注册的数据服务添加缓存装饰器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCacheForAllDataServices(this IServiceCollection services)
    {
        // 获取所有已注册的 IHiFlyDataService<> 服务
        var dataServiceDescriptors = services
            .Where(descriptor => descriptor.ServiceType.IsGenericType &&
                                descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IHiFlyDataService<>))
            .ToList();

        foreach (var descriptor in dataServiceDescriptors)
        {
            var entityType = descriptor.ServiceType.GetGenericArguments()[0];
            var cachedServiceType = typeof(CachedDataService<>).MakeGenericType(entityType);

            // 移除原始服务注册
            services.Remove(descriptor);

            // 重新注册原始服务（不使用接口）- 修复构造函数参数问题
            var originalImplementationType = descriptor.ImplementationType;
            if (originalImplementationType != null)
            {
                services.Add(new ServiceDescriptor(
                    originalImplementationType,
                    provider =>
                    {
                        // 正确地创建 EfDataService 实例，传递正确的构造函数参数
                        if (originalImplementationType.IsGenericType && 
                            originalImplementationType.GetGenericTypeDefinition().Name == "EfDataService`2")
                        {
                            var contextType = originalImplementationType.GetGenericArguments()[0];
                            var itemType = originalImplementationType.GetGenericArguments()[1];
                            
                            // 获取所需的服务
                            var dbContextFactoryType = typeof(IDbContextFactory<>).MakeGenericType(contextType);
                            var dbContextFactory = provider.GetRequiredService(dbContextFactoryType);
                            
                            var loggerType = typeof(ILogger<>).MakeGenericType(originalImplementationType);
                            var logger = provider.GetRequiredService(loggerType);
                            
                            return Activator.CreateInstance(originalImplementationType, dbContextFactory, logger)!;
                        }
                        
                        // 对于其他类型，尝试通过 DI 容器创建
                        return ActivatorUtilities.CreateInstance(provider, originalImplementationType);
                    },
                    descriptor.Lifetime));
            }

            // 注册缓存装饰器服务
            services.Add(new ServiceDescriptor(
                descriptor.ServiceType,
                provider =>
                {
                    var originalService = provider.GetRequiredService(originalImplementationType!);
                    var cacheService = provider.GetRequiredService<IMultiLevelCacheService>();
                    var keyGenerator = provider.GetRequiredService<TableCacheKeyGenerator>();
                    var logger = provider.GetRequiredService(typeof(ILogger<>).MakeGenericType(cachedServiceType));

                    return Activator.CreateInstance(cachedServiceType, originalService, cacheService, keyGenerator, logger)!;
                },
                descriptor.Lifetime));
        }

        return services;
    }

    /// <summary>
    /// 创建控制台日志记录器
    /// </summary>
    private static ILogger CreateConsoleLogger(string categoryName)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        });
        return loggerFactory.CreateLogger(categoryName);
    }
}

/// <summary>
/// 缓存相关的扩展方法
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// 批量预热缓存
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
        var commonQueries = new List<QueryPageOptions>();

        // 生成常用查询：前几页数据
        for (int i = 1; i <= maxPages; i++)
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

        return await crudService.WarmupCacheAsync(commonQueries, null, isTree);
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    /// <param name="cacheService">缓存服务</param>
    /// <param name="olderThan">清理早于指定时间的缓存</param>
    /// <returns>清理的项数</returns>
    public static async Task<int> CleanupExpiredCacheAsync(
        this IMultiLevelCacheService cacheService,
        TimeSpan olderThan)
    {
        try
        {
            // 这里需要具体实现支持按时间清理
            // 当前的接口设计主要支持按模式清理
            var stats = await cacheService.GetStatisticsAsync();
            var totalItems = stats.Values.Sum(s => s.ItemCount);

            // 简单的清理策略：如果缓存项过多，清理部分
            if (totalItems > 50000)
            {
                await cacheService.ClearLevelAsync("level1");
                return (int)totalItems;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}
