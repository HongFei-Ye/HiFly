// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Cache.Services;
using HiFly.Tables.Cache.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HiFly.Tables.Cache.Examples;

/// <summary>
/// Redis缓存模式删除使用示例
/// </summary>
public class RedisCachePatternDeleteExample
{
    private readonly IMultiLevelCacheService _multiLevelCache;
    private readonly IRedisCacheService _redisCache;
    private readonly ILogger<RedisCachePatternDeleteExample> _logger;

    public RedisCachePatternDeleteExample(
        IMultiLevelCacheService multiLevelCache,
        IRedisCacheService redisCache,
        ILogger<RedisCachePatternDeleteExample> logger)
    {
        _multiLevelCache = multiLevelCache;
        _redisCache = redisCache;
        _logger = logger;
    }

    /// <summary>
    /// 演示基础模式删除功能
    /// </summary>
    public async Task DemoBasicPatternDelete()
    {
        _logger.LogInformation("=== 基础模式删除演示 ===");

        // 1. 创建测试数据
        await CreateTestData();

        // 2. 演示不同的模式删除方式
        await DemoPatternDeleteMethods();

        // 3. 验证删除结果
        await VerifyDeletionResults();
    }

    /// <summary>
    /// 创建测试数据
    /// </summary>
    private async Task CreateTestData()
    {
        _logger.LogInformation("创建测试缓存数据...");

        var testData = new (string Key, object Value)[]
        {
            ("user:1001", new { Id = 1001, Name = "张三", Email = "zhang@example.com" }),
            ("user:1002", new { Id = 1002, Name = "李四", Email = "li@example.com" }),
            ("user:1003", new { Id = 1003, Name = "王五", Email = "wang@example.com" }),
            ("product:2001", new { Id = 2001, Name = "商品A", Price = 99.99 }),
            ("product:2002", new { Id = 2002, Name = "商品B", Price = 149.99 }),
            ("order:3001", new { Id = 3001, UserId = 1001, ProductId = 2001, Quantity = 2 }),
            ("order:3002", new { Id = 3002, UserId = 1002, ProductId = 2002, Quantity = 1 }),
            ("cache:temp:session1", new { SessionId = "session1", Data = "临时数据1" }),
            ("cache:temp:session2", new { SessionId = "session2", Data = "临时数据2" }),
        };

        foreach (var (key, value) in testData)
        {
            await _multiLevelCache.SetAsync(key, value, TimeSpan.FromHours(1));
        }

        _logger.LogInformation("创建了 {Count} 个测试缓存项", testData.Length);
    }

    /// <summary>
    /// 演示不同的模式删除方法
    /// </summary>
    private async Task DemoPatternDeleteMethods()
    {
        _logger.LogInformation("演示不同的模式删除方法...");

        // 1. 基础模式删除 - 删除所有用户缓存
        _logger.LogInformation("1. 使用基础模式删除所有用户缓存 (user:*)");
        var deletedUsers = await _multiLevelCache.RemoveByPatternAsync("user:*");
        _logger.LogInformation("删除了 {Count} 个用户缓存项", deletedUsers);

        // 2. Lua脚本高性能删除 - 删除所有产品缓存
        if (_multiLevelCache is MultiLevelCacheService multiLevel)
        {
            _logger.LogInformation("2. 使用Lua脚本删除所有产品缓存 (product:*)");
            var deletedProducts = await multiLevel.RemoveByPatternWithLuaAsync("product:*");
            _logger.LogInformation("删除了 {Count} 个产品缓存项", deletedProducts);
        }

        // 3. SCAN安全删除 - 删除临时缓存
        if (_multiLevelCache is MultiLevelCacheService multiLevelScan)
        {
            _logger.LogInformation("3. 使用SCAN安全删除临时缓存 (cache:temp:*)");
            var deletedTemp = await multiLevelScan.RemoveByPatternWithScanAsync("cache:temp:*");
            _logger.LogInformation("删除了 {Count} 个临时缓存项", deletedTemp);
        }

        // 4. 直接使用Redis服务删除订单缓存
        _logger.LogInformation("4. 直接使用Redis服务删除订单缓存 (order:*)");
        var deletedOrders = await _redisCache.RemoveByPatternAsync("order:*");
        _logger.LogInformation("删除了 {Count} 个订单缓存项", deletedOrders);
    }

    /// <summary>
    /// 验证删除结果
    /// </summary>
    private async Task VerifyDeletionResults()
    {
        _logger.LogInformation("验证删除结果...");

        var testKeys = new[]
        {
            "user:1001", "user:1002", "user:1003",
            "product:2001", "product:2002",
            "order:3001", "order:3002",
            "cache:temp:session1", "cache:temp:session2"
        };

        foreach (var key in testKeys)
        {
            var exists = await _multiLevelCache.ExistsAsync(key);
            _logger.LogInformation("缓存键 {Key} 存在: {Exists}", key, exists);
        }
    }

    /// <summary>
    /// 演示高级批量删除功能
    /// </summary>
    public async Task DemoAdvancedBatchDelete()
    {
        _logger.LogInformation("=== 高级批量删除演示 ===");

        // 创建大量测试数据
        await CreateLargeTestDataset();

        // 演示不同删除策略的性能
        await BenchmarkDeletionMethods();

        // 清理测试数据
        await CleanupTestData();
    }

    /// <summary>
    /// 创建大量测试数据
    /// </summary>
    private async Task CreateLargeTestDataset()
    {
        _logger.LogInformation("创建大量测试数据...");

        var tasks = new List<Task>();
        
        // 创建1000个用户缓存
        for (int i = 1; i <= 1000; i++)
        {
            var key = $"large:user:{i:D4}";
            var value = new { Id = i, Name = $"User{i}", Email = $"user{i}@example.com" };
            tasks.Add(_multiLevelCache.SetAsync(key, value, TimeSpan.FromHours(2)));
        }

        // 创建500个产品缓存
        for (int i = 1; i <= 500; i++)
        {
            var key = $"large:product:{i:D4}";
            var value = new { Id = i, Name = $"Product{i}", Price = i * 10.0 };
            tasks.Add(_multiLevelCache.SetAsync(key, value, TimeSpan.FromHours(2)));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("创建了 {Count} 个大量测试缓存项", tasks.Count);
    }

    /// <summary>
    /// 基准测试删除方法
    /// </summary>
    private async Task BenchmarkDeletionMethods()
    {
        _logger.LogInformation("基准测试不同删除方法的性能...");

        if (_multiLevelCache is not MultiLevelCacheService multiLevel)
        {
            _logger.LogWarning("需要MultiLevelCacheService实例来进行性能测试");
            return;
        }

        // 测试1: 基础删除方法
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        var deleted1 = await multiLevel.RemoveByPatternAsync("large:user:*");
        sw1.Stop();
        _logger.LogInformation("基础删除: {Count} 项, 耗时: {Time}ms", deleted1, sw1.ElapsedMilliseconds);

        // 测试2: Lua脚本删除
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        var deleted2 = await multiLevel.RemoveByPatternWithLuaAsync("large:product:*");
        sw2.Stop();
        _logger.LogInformation("Lua脚本删除: {Count} 项, 耗时: {Time}ms", deleted2, sw2.ElapsedMilliseconds);
    }

    /// <summary>
    /// 清理测试数据
    /// </summary>
    private async Task CleanupTestData()
    {
        _logger.LogInformation("清理所有测试数据...");
        
        var patterns = new[] { "large:*", "user:*", "product:*", "order:*", "cache:*" };
        
        foreach (var pattern in patterns)
        {
            var deleted = await _multiLevelCache.RemoveByPatternAsync(pattern);
            _logger.LogInformation("清理模式 {Pattern}: {Count} 项", pattern, deleted);
        }
    }

    /// <summary>
    /// 演示缓存统计和监控
    /// </summary>
    public async Task DemoCacheStatistics()
    {
        _logger.LogInformation("=== 缓存统计演示 ===");

        // 获取多级缓存统计
        var stats = await _multiLevelCache.GetStatisticsAsync();
        foreach (var (level, stat) in stats)
        {
            _logger.LogInformation("缓存层级 {Level}: 命中{Hit}, 未命中{Miss}, 命中率{Rate:P}", 
                level, stat.HitCount, stat.MissCount, stat.HitRate);
        }

        // 获取Redis详细信息
        var redisInfo = await _redisCache.GetDatabaseInfoAsync();
        _logger.LogInformation("Redis连接状态: {Connected}", redisInfo.GetValueOrDefault("IsConnected"));
        _logger.LogInformation("Redis数据库ID: {DatabaseId}", redisInfo.GetValueOrDefault("DatabaseId"));
    }
}

/// <summary>
/// 服务注册示例
/// </summary>
public static class ServiceRegistrationExample
{
    /// <summary>
    /// 配置Redis缓存服务
    /// </summary>
    public static IServiceCollection ConfigureRedisCacheServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 方式1: 使用扩展方法注册完整的多级缓存（推荐）
        services.AddMultiLevelCacheWithRedis(configuration);

        // 方式2: 手动注册各个服务
        // services.AddTableCache(configuration);
        // services.AddRedisCacheService(configuration);

        // 添加健康检查
        services.AddRedisCacheHealthChecks(configuration);

        // 注册示例服务
        services.AddScoped<RedisCachePatternDeleteExample>();

        return services;
    }

    /// <summary>
    /// 示例配置文件内容
    /// </summary>
    public static string GetExampleConfiguration()
    {
        return """
        {
          "Cache": {
            "DefaultExpirationMinutes": 30,
            "EnableDistributedCache": true,
            "RedisConnectionString": "localhost:6379",
            "KeyPrefix": "HiFly:Tables:",
            "EnableStatistics": true,
            "CompressionThreshold": 1024,
            "MemoryCache": {
              "MaxItems": 10000,
              "SizeLimitMB": 100,
              "ExpirationScanFrequencySeconds": 60,
              "CompactionPercentage": 0.25
            },
            "DistributedCache": {
              "DefaultExpirationMinutes": 60,
              "SlidingExpirationMinutes": 20,
              "DatabaseIndex": 0,
              "EnableCompression": true,
              "ConnectTimeout": 5,
              "SyncTimeout": 5
            }
          }
        }
        """;
    }
}
