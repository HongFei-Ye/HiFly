// SPDX-License-Identifier: Apache-2.0

using BootstrapBlazor.Components;

namespace HiFly.Table;

/// <summary>
/// 缓存同步服务接口
/// </summary>
public interface ICacheSyncService
{
    /// <summary>
    /// 根据实体类型和操作类型清理相关缓存
    /// </summary>
    Task InvalidateCacheAsync<T>(ItemChangedType operation, T? entity = null) where T : class;

    /// <summary>
    /// 清理指定实体类型的所有相关缓存
    /// </summary>
    Task InvalidateEntityCacheAsync<T>() where T : class;

    /// <summary>
    /// 清理所有缓存
    /// </summary>
    Task InvalidateAllCacheAsync();

    /// <summary>
    /// 清理指定键的缓存
    /// </summary>
    Task InvalidateCacheKeyAsync(string cacheKey);
}
