// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

namespace HiFly.Tables.Cache.Interfaces;

/// <summary>
/// 缓存服务接口
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// 获取缓存项
    /// </summary>
    /// <typeparam name="T">缓存项类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存项，如果不存在则返回null</returns>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// 设置缓存项
    /// </summary>
    /// <typeparam name="T">缓存项类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiry">过期时间</param>
    /// <returns>是否设置成功</returns>
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;

    /// <summary>
    /// 删除缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否删除成功</returns>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// 检查缓存项是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 批量删除缓存项
    /// </summary>
    /// <param name="pattern">缓存键模式</param>
    /// <returns>删除的项数</returns>
    Task<int> RemoveByPatternAsync(string pattern);

    /// <summary>
    /// 获取缓存项的剩余生存时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>剩余时间，如果不存在或无过期时间则返回null</returns>
    Task<TimeSpan?> GetTimeToLiveAsync(string key);

    /// <summary>
    /// 刷新缓存项的过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expiry">新的过期时间</param>
    /// <returns>是否刷新成功</returns>
    Task<bool> RefreshAsync(string key, TimeSpan expiry);
}
