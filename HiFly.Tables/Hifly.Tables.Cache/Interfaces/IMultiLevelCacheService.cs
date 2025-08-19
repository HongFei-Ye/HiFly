// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

namespace HiFly.Tables.Cache.Interfaces;

/// <summary>
/// 多级缓存服务接口
/// </summary>
public interface IMultiLevelCacheService : ICacheService
{
    /// <summary>
    /// 获取缓存层级统计信息
    /// </summary>
    /// <returns>每层的命中统计</returns>
    Task<Dictionary<string, CacheStatistics>> GetStatisticsAsync();

    /// <summary>
    /// 清空指定层级的缓存
    /// </summary>
    /// <param name="level">缓存层级名称</param>
    /// <returns>是否清空成功</returns>
    Task<bool> ClearLevelAsync(string level);

    /// <summary>
    /// 清空所有层级的缓存
    /// </summary>
    /// <returns>清空的项数</returns>
    Task<int> ClearAllAsync();

    /// <summary>
    /// 预热缓存
    /// </summary>
    /// <param name="keys">要预热的缓存键列表</param>
    /// <param name="dataLoader">数据加载器</param>
    /// <returns>预热成功的项数</returns>
    Task<int> WarmupAsync<T>(IEnumerable<string> keys, Func<string, Task<T?>> dataLoader) where T : class;

    /// <summary>
    /// 获取所有缓存键
    /// </summary>
    /// <returns>所有缓存键列表</returns>
    Task<List<string>> GetAllKeysAsync();

    /// <summary>
    /// 获取匹配模式的缓存键
    /// </summary>
    /// <param name="pattern">模式，支持通配符*</param>
    /// <returns>匹配的缓存键列表</returns>
    Task<List<string>> GetKeysByPatternAsync(string pattern);
}

/// <summary>
/// 缓存统计信息
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// 命中次数
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// 未命中次数
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// 总请求次数
    /// </summary>
    public long TotalRequests => HitCount + MissCount;

    /// <summary>
    /// 命中率
    /// </summary>
    public double HitRate => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;

    /// <summary>
    /// 缓存项数量
    /// </summary>
    public long ItemCount { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
