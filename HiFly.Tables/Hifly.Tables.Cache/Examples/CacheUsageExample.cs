// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

/*
 * HiFly.Tables 动态多级混合缓存使用示例
 * 
 * 此文件展示了如何配置和使用Table的多级缓存功能
 */

using BootstrapBlazor.Components;
using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Cache.Services;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HiFly.Tables.Cache.Examples;

/// <summary>
/// 缓存使用示例 - 新架构版本
/// </summary>
public class CacheUsageExample
{
    /// <summary>
    /// 在Program.cs中配置缓存服务（EF Core）
    /// </summary>
    public static void ConfigureEfCoreWithCache(IServiceCollection services, IConfiguration configuration)
    {
        // 注意：这需要引用 HiFly.Orm.EFcore 包
        // 1. 添加 EF Core 数据服务
        // services.AddAllEfDataServices<YourDbContext>();

        // 2. 添加缓存服务
        services.AddTableCache(configuration);

        // 3. 为所有数据服务添加缓存装饰器
        services.AddCacheForAllDataServices();
    }

    /// <summary>
    /// 在Program.cs中配置缓存服务（FreeSql）
    /// </summary>
    public static void ConfigureFreeSqlWithCache(IServiceCollection services, IConfiguration configuration)
    {
        // 注意：这需要引用 HiFly.Orm.FreeSql 包
        // 1. 添加 FreeSql 数据服务
        // var assemblies = new[] { typeof(YourEntity).Assembly };
        // services.AddFreeSqlSqlite("Data Source=app.db", assemblies);

        // 2. 添加缓存服务
        services.AddTableCache(configuration);

        // 3. 为所有数据服务添加缓存装饰器
        services.AddCacheForAllDataServices();
    }

    /// <summary>
    /// 手动为特定实体添加缓存
    /// </summary>
    public static void ConfigureSpecificEntityCache(IServiceCollection services, IConfiguration configuration)
    {
        // 注意：这需要先注册基础数据服务
        // 1. 先注册基础数据服务
        // services.AddFreeSqlDataService<YourEntity>();

        // 2. 添加缓存服务
        services.AddTableCache(configuration);

        // 3. 为特定实体添加缓存
        services.AddCachedDataService<YourEntity>();
    }
}

/// <summary>
/// Blazor 组件使用示例
/// </summary>
public class YourTableComponent : ComponentBase
{
    [Inject] private IHiFlyDataService<YourEntity> DataService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // 如果注入的是缓存版本，以下操作会自动使用缓存
        // 如果注入的是普通版本，会直接访问数据库

        // 预热缓存（如果是 CachedDataService）
        if (DataService is CachedDataService<YourEntity> cachedService)
        {
            await WarmupCache(cachedService);
        }
    }

    private async Task WarmupCache(CachedDataService<YourEntity> cachedService)
    {
        // 预热常用查询
        await cachedService.WarmupCommonQueriesAsync(pageSize: 20, maxPages: 3);

        // 获取缓存统计
        var stats = await cachedService.GetCacheStatsAsync();
        System.Console.WriteLine($"缓存统计: {stats}");
    }

    private async Task<QueryData<YourEntity>> OnQueryAsync(QueryPageOptions options)
    {
        // 这个查询会自动使用缓存（如果配置了缓存）
        return await DataService.OnQueryAsync(options);
    }

    private async Task<bool> OnSaveAsync(YourEntity item, ItemChangedType changedType)
    {
        // 保存成功后会自动清除相关缓存（如果配置了缓存）
        return await DataService.OnSaveAsync(item, changedType);
    }

    private async Task<bool> OnDeleteAsync(IEnumerable<YourEntity> items)
    {
        // 删除成功后会自动清除相关缓存（如果配置了缓存）
        return await DataService.OnDeleteAsync(items);
    }
}

/// <summary>
/// 手动控制缓存示例
/// </summary>
public class ManualCacheExample
{
    private readonly IMultiLevelCacheService _cacheService;
    private readonly TableCacheKeyGenerator _keyGenerator;

    public ManualCacheExample(
        IMultiLevelCacheService cacheService,
        TableCacheKeyGenerator keyGenerator)
    {
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
    }

    public async Task ManualCacheOperations()
    {
        // 手动设置缓存
        var queryOptions = new QueryPageOptions { PageIndex = 1, PageItems = 20 };
        var queryData = new QueryData<YourEntity>
        {
            TotalCount = 100,
            Items = new List<YourEntity>()
        };

        var key = _keyGenerator.GenerateQueryKey<YourEntity>(queryOptions, null, false);
        await _cacheService.SetAsync(key, queryData, TimeSpan.FromMinutes(30));

        // 手动获取缓存
        var cachedData = await _cacheService.GetAsync<QueryData<YourEntity>>(key);

        // 清除特定实体的所有缓存
        var pattern = _keyGenerator.GetEntityCachePattern<YourEntity>();
        await _cacheService.RemoveByPatternAsync(pattern);

        // 获取缓存统计信息
        var statistics = await _cacheService.GetStatisticsAsync();
        System.Console.WriteLine($"缓存级别数: {statistics.Count}");
    }
}

/// <summary>
/// 树形结构缓存示例
/// </summary>
public class TreeCacheExample : ComponentBase
{
    [Inject] private IHiFlyDataService<YourTreeEntity> TreeDataService { get; set; } = default!;

    public async Task<QueryData<YourTreeEntity>> QueryTreeDataAsync(QueryPageOptions options)
    {
        // 树形结构查询会自动缓存完整的子树
        return await TreeDataService.OnQueryAsync(options, null, isTree: true);
    }

    public async Task WarmupTreeCacheAsync()
    {
        if (TreeDataService is CachedDataService<YourTreeEntity> cachedService)
        {
            // 预热树形结构的常用查询
            var queries = new[]
            {
                new QueryPageOptions { PageIndex = 1, PageItems = 50 }, // 根节点
                new QueryPageOptions { PageIndex = 1, PageItems = 100 } // 更多根节点
            };

            await cachedService.WarmupCacheAsync(queries, null, isTree: true);
        }
    }
}

/// <summary>
/// 缓存配置示例
/// </summary>
public class CacheConfigurationExample
{
    /// <summary>
    /// appsettings.json 缓存配置示例
    /// </summary>
    public static string GetCacheConfigurationExample()
    {
        return @"
{
  ""Cache"": {
    ""DefaultExpirationMinutes"": 30,
    ""EnableDistributedCache"": false,
    ""RedisConnectionString"": ""localhost:6379"",
    ""MemoryCache"": {
      ""MaxItems"": 10000,
      ""CompactionPercentage"": 0.25,
      ""ExpirationScanFrequencySeconds"": 60
    },
    ""DistributedCache"": {
      ""DefaultExpirationMinutes"": 60,
      ""SlidingExpirationMinutes"": 30
    },
    ""EntityCacheSettings"": {
      ""YourEntity"": {
        ""ExpirationMinutes"": 15,
        ""EnableCache"": true
      },
      ""YourTreeEntity"": {
        ""ExpirationMinutes"": 45,
        ""EnableCache"": true
      }
    }
  }
}";
    }

    /// <summary>
    /// 自定义缓存配置
    /// </summary>
    public static void ConfigureCustomCache(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTableCache(configuration);

        // 自定义缓存配置
        services.PostConfigure<HiFly.Tables.Cache.Configuration.CacheOptions>(options =>
        {
            options.DefaultExpirationMinutes = 60; // 默认过期时间
            options.EnableDistributedCache = true; // 启用分布式缓存
        });
    }
}

// 示例实体类
public class YourEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
}

// 示例树形实体类
public class YourTreeEntity
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
}

// 示例数据库上下文（仅用于 EF Core）
public class YourDbContext
{
    // 注意：这只是示例，实际使用时需要继承真正的 DbContext
    // 并且需要引用 Microsoft.EntityFrameworkCore
    // 示例：
    // public class YourDbContext : Microsoft.EntityFrameworkCore.DbContext
    // {
    //     public Microsoft.EntityFrameworkCore.DbSet<YourEntity> YourEntities { get; set; }
    //     public Microsoft.EntityFrameworkCore.DbSet<YourTreeEntity> YourTreeEntities { get; set; }
    //
    //     public YourDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<YourDbContext> options) : base(options)
    //     {
    //     }
    // }
}
