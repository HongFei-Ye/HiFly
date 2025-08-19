// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

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
    /// 内存缓存配置
    /// </summary>
    public MemoryCacheConfig MemoryCache { get; set; } = new();

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
