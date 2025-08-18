// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Interfaces;
using HiFly.Tables.Cache.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HiFly.Tables.Cache.Extensions;

/// <summary>
/// Redis缓存服务注册扩展
/// </summary>
public static class RedisCacheServiceExtensions
{
    /// <summary>
    /// 添加Redis缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddRedisCacheService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 获取缓存配置
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>();
        
        if (cacheOptions?.EnableDistributedCache == true && !string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
        {
            // 注册Redis连接
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                return ConnectionMultiplexer.Connect(cacheOptions.RedisConnectionString);
            });

            // 注册Redis缓存服务
            services.AddSingleton<IRedisCacheService, RedisCacheService>();

            // 注册标准分布式缓存
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.RedisConnectionString;
                options.InstanceName = "HiFly.Tables";
            });
        }

        return services;
    }

    /// <summary>
    /// 添加完整的多级缓存服务（包含Redis支持）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMultiLevelCacheWithRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 添加基础缓存服务
        services.AddTableCache(configuration);
        
        // 添加Redis缓存服务
        services.AddRedisCacheService(configuration);

        // 重新注册多级缓存服务以包含Redis支持
        services.AddSingleton<IMultiLevelCacheService>(provider =>
        {
            var memoryCache = provider.GetRequiredService<MemoryCacheService>();
            var distributedCache = provider.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            var redisCache = provider.GetService<IRedisCacheService>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MultiLevelCacheService>>();
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheOptions>>();

            return new MultiLevelCacheService(memoryCache, logger, options, distributedCache, redisCache);
        });

        return services;
    }

    /// <summary>
    /// 添加Redis缓存健康检查（简化版本）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    /// <remarks>
    /// 简化版健康检查，不依赖外部健康检查包
    /// </remarks>
    public static IServiceCollection AddRedisCacheHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>();
        
        if (cacheOptions?.EnableDistributedCache == true && !string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
        {
            // 注册简化的健康检查服务
            services.AddSingleton<IRedisCacheHealthService, RedisCacheHealthService>();
        }

        return services;
    }
}

/// <summary>
/// Redis缓存健康检查服务接口
/// </summary>
public interface IRedisCacheHealthService
{
    /// <summary>
    /// 检查Redis缓存健康状态
    /// </summary>
    /// <returns>健康检查结果</returns>
    Task<(bool IsHealthy, string Message, Dictionary<string, object>? Data)> CheckHealthAsync();
}

/// <summary>
/// Redis缓存健康检查服务实现
/// </summary>
public class RedisCacheHealthService : IRedisCacheHealthService
{
    private readonly IRedisCacheService? _redisCacheService;

    public RedisCacheHealthService(IRedisCacheService? redisCacheService)
    {
        _redisCacheService = redisCacheService;
    }

    public async Task<(bool IsHealthy, string Message, Dictionary<string, object>? Data)> CheckHealthAsync()
    {
        try
        {
            if (_redisCacheService == null)
            {
                return (false, "Redis缓存服务未配置", null);
            }

            var info = await _redisCacheService.GetDatabaseInfoAsync();
            var isConnected = info.GetValueOrDefault("IsConnected", false);

            if (isConnected is true)
            {
                return (true, "Redis缓存连接正常", info);
            }
            else
            {
                return (false, "Redis缓存连接失败", info);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Redis缓存健康检查异常: {ex.Message}", new Dictionary<string, object>
            {
                ["Exception"] = ex.GetType().Name,
                ["Message"] = ex.Message
            });
        }
    }
}
