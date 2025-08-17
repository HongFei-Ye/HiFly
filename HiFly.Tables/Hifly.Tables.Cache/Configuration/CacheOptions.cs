// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Tables.Cache.Configuration;

/// <summary>
/// 缓存配置选项
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// 配置节点名称
    /// </summary>
    public const string SectionName = "Cache";

    /// <summary>
    /// 默认过期时间（分钟）
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 是否启用分布式缓存
    /// </summary>
    public bool EnableDistributedCache { get; set; } = false;

    /// <summary>
    /// Redis连接字符串
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// 内存缓存配置
    /// </summary>
    public MemoryCacheConfig MemoryCache { get; set; } = new();

    /// <summary>
    /// 分布式缓存配置
    /// </summary>
    public DistributedCacheConfig DistributedCache { get; set; } = new();

    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "HiFly:Tables:";

    /// <summary>
    /// 是否启用缓存统计
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// 缓存压缩阈值（字节）
    /// </summary>
    public int CompressionThreshold { get; set; } = 1024;
}

/// <summary>
/// 内存缓存配置
/// </summary>
public class MemoryCacheConfig
{
    /// <summary>
    /// 最大缓存项数
    /// </summary>
    public int MaxItems { get; set; } = 10000;

    /// <summary>
    /// 内存限制（MB）
    /// </summary>
    public int SizeLimitMB { get; set; } = 100;

    /// <summary>
    /// 扫描频率（秒）
    /// </summary>
    public int ExpirationScanFrequencySeconds { get; set; } = 60;

    /// <summary>
    /// 压缩率（0.0-1.0）
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.25;
}

/// <summary>
/// 分布式缓存配置
/// </summary>
public class DistributedCacheConfig
{
    /// <summary>
    /// 默认过期时间（分钟）
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 滑动过期时间（分钟）
    /// </summary>
    public int SlidingExpirationMinutes { get; set; } = 20;

    /// <summary>
    /// Redis数据库索引
    /// </summary>
    public int DatabaseIndex { get; set; } = 0;

    /// <summary>
    /// 是否启用数据压缩
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 5;

    /// <summary>
    /// 同步超时时间（秒）
    /// </summary>
    public int SyncTimeout { get; set; } = 5;
}
