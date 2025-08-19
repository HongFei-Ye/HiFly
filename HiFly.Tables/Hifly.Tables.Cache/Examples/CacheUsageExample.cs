// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

/*
 * HiFly.Tables 内存缓存使用示例
 * 
 * 此文件展示了如何配置和使用Table的内存缓存功能
 */

using BootstrapBlazor.Components;
using Hifly.Tables.Cache.Extensions;
using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Cache.Services;
using HiFly.Tables.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HiFly.Tables.Cache.Examples;

/// <summary>
/// 缓存使用示例 - 内存缓存版本
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

        // 2. 添加内存缓存服务
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

        // 2. 添加内存缓存服务
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

        // 2. 添加内存缓存服务
        services.AddTableCache(configuration);

        // 3. 为特定实体添加缓存
        services.AddCachedDataService<YourEntity>();
    }

    /// <summary>
    /// 示例配置文件内容 (appsettings.json)
    /// </summary>
    public static string GetExampleConfiguration()
    {
        return """
        {
          "Cache": {
            "DefaultExpirationMinutes": 30,
            "KeyPrefix": "HiFly:Tables:",
            "EnableStatistics": true,
            "CompressionThreshold": 1024,
            "MemoryCache": {
              "MaxItems": 10000,
              "SizeLimitMB": 100,
              "ExpirationScanFrequencySeconds": 60,
              "CompactionPercentage": 0.25
            }
          }
        }
        """;
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

        // 基本查询（会被缓存）
        var queryOptions = new QueryPageOptions
        {
            PageIndex = 1,
            PageItems = 20
        };

        var data = await DataService.OnQueryAsync(queryOptions);
        
        // 后续相同的查询会从缓存中获取，提升性能
        var cachedData = await DataService.OnQueryAsync(queryOptions);
    }

    /// <summary>
    /// 手动缓存管理示例
    /// </summary>
    private async Task CacheManagementExample()
    {
        // 如果需要直接访问缓存服务
        if (DataService is CachedDataService<YourEntity> cachedService)
        {
            // 获取缓存统计
            var stats = await cachedService.GetCacheStatsAsync();
            
            // 强制清理所有相关缓存
            await cachedService.ForceClearAllCacheAsync();
            
            // 预热常用查询
            var commonQueries = new List<QueryPageOptions>
            {
                new() { PageIndex = 1, PageItems = 20 },
                new() { PageIndex = 2, PageItems = 20 }
            };
            await cachedService.WarmupCacheAsync(commonQueries);
        }
    }

    /// <summary>
    /// 标准的Table组件操作示例
    /// </summary>
    private async Task<QueryData<YourEntity>> OnQueryAsync(QueryPageOptions options)
    {
        // 自动使用缓存的查询
        return await DataService.OnQueryAsync(options);
    }

    private async Task<bool> OnSaveAsync(YourEntity item, ItemChangedType changedType)
    {
        // 保存时自动清理相关缓存
        return await DataService.OnSaveAsync(item, changedType);
    }

    private async Task<bool> OnDeleteAsync(IEnumerable<YourEntity> items)
    {
        // 删除时自动清理相关缓存
        return await DataService.OnDeleteAsync(items);
    }
}

/// <summary>
/// 示例实体类
/// </summary>
public class YourEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}
