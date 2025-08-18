// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Tables.Cache.Interfaces;

namespace HiFly.Tables.Cache.Interfaces;

/// <summary>
/// Redis缓存服务接口
/// </summary>
public interface IRedisCacheService : ICacheService
{
    /// <summary>
    /// 获取Redis数据库
    /// </summary>
    /// <returns>Redis数据库实例</returns>
    Task<object?> GetDatabaseAsync();

    /// <summary>
    /// 按模式查找键
    /// </summary>
    /// <param name="pattern">键模式</param>
    /// <returns>匹配的键列表</returns>
    Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);

    /// <summary>
    /// 批量删除指定的键
    /// </summary>
    /// <param name="keys">要删除的键列表</param>
    /// <returns>删除的项数</returns>
    Task<int> RemoveKeysAsync(IEnumerable<string> keys);

    /// <summary>
    /// 清空数据库中的所有键
    /// </summary>
    /// <returns>是否成功</returns>
    Task<bool> FlushDatabaseAsync();

    /// <summary>
    /// 获取数据库信息和统计
    /// </summary>
    /// <returns>数据库信息</returns>
    Task<Dictionary<string, object>> GetDatabaseInfoAsync();

    /// <summary>
    /// 执行Lua脚本进行批量操作
    /// </summary>
    /// <param name="script">Lua脚本</param>
    /// <param name="keys">键列表</param>
    /// <param name="values">值列表</param>
    /// <returns>脚本执行结果</returns>
    Task<object?> ExecuteScriptAsync(string script, string[]? keys = null, object[]? values = null);

}
