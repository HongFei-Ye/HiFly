// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Core.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HiFly.Tables.Cache.Services;

/// <summary>
/// Table缓存键生成器
/// </summary>
public class TableCacheKeyGenerator
{
    private readonly string _keyPrefix;

    public TableCacheKeyGenerator(string keyPrefix = "")
    {
        // 不设置默认前缀，让 MemoryCacheService 统一管理前缀
        _keyPrefix = string.IsNullOrEmpty(keyPrefix) ? "" : keyPrefix;
    }

    /// <summary>
    /// 生成查询缓存键
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="options">查询选项</param>
    /// <param name="filterParameters">过滤参数</param>
    /// <param name="isTree">是否为树形结构</param>
    /// <param name="additionalKeys">额外的键值对</param>
    /// <returns>缓存键</returns>
    public string GenerateQueryKey<TItem>(
        QueryPageOptions options,
        PropertyFilterParameters? filterParameters = null,
        bool isTree = false,
        Dictionary<string, object>? additionalKeys = null)
        where TItem : class
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(_keyPrefix);
        keyBuilder.Append("Query:");
        keyBuilder.Append(typeof(TItem).Name);
        keyBuilder.Append(":");

        // 添加查询参数
        var queryData = new
        {
            PageIndex = options.PageIndex,
            PageItems = options.PageItems,
            SortName = options.SortName,
            SortOrder = options.SortOrder,
            IsTree = isTree,
            Filters = options.Filters?.Select(f => new
            {
                FieldKey = GetFieldKey(f),
                FieldValue = GetFieldValue(f),
                FilterAction = GetFilterAction(f)
            }).OrderBy(f => f.FieldKey),
            Searches = options.Searches?.Select(s => new
            {
                FieldKey = GetFieldKey(s),
                FieldValue = GetFieldValue(s)
            }).OrderBy(s => s.FieldKey),
            AdvanceSearches = options.AdvanceSearches?.Select(a => new
            {
                FieldKey = GetFieldKey(a),
                FieldValue = GetFieldValue(a)
            }).OrderBy(a => a.FieldKey),
            CustomerSearches = options.CustomerSearches?.Select((cs, index) => new
            {
                Index = index,
                Type = cs.GetType().Name,
                Value = cs.ToString()
            }).OrderBy(c => c.Index),
            FilterParameters = filterParameters != null ? SerializeFilterParameters(filterParameters) : null,
            AdditionalKeys = additionalKeys?.OrderBy(k => k.Key)
        };

        var json = JsonSerializer.Serialize(queryData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // 生成哈希以缩短键长度
        var hash = ComputeHash(json);
        keyBuilder.Append(hash);

        return keyBuilder.ToString();
    }

    /// <summary>
    /// 生成实体缓存键
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="id">实体ID</param>
    /// <returns>缓存键</returns>
    public string GenerateEntityKey<TItem>(object id) where TItem : class
    {
        return $"{_keyPrefix}Entity:{typeof(TItem).Name}:{id}";
    }

    /// <summary>
    /// 生成实体列表缓存键
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="ids">实体ID列表</param>
    /// <returns>缓存键</returns>
    public string GenerateEntityListKey<TItem>(IEnumerable<object> ids) where TItem : class
    {
        var sortedIds = ids.OrderBy(id => id.ToString()).ToList();
        var idsJson = JsonSerializer.Serialize(sortedIds);
        var hash = ComputeHash(idsJson);
        return $"{_keyPrefix}EntityList:{typeof(TItem).Name}:{hash}";
    }

    /// <summary>
    /// 生成树形结构缓存键
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="parentId">父级ID</param>
    /// <param name="depth">深度</param>
    /// <returns>缓存键</returns>
    public string GenerateTreeKey<TItem>(object? parentId = null, int depth = -1) where TItem : class
    {
        var parentIdStr = parentId?.ToString() ?? "root";
        return $"{_keyPrefix}Tree:{typeof(TItem).Name}:{parentIdStr}:depth{depth}";
    }

    /// <summary>
    /// 生成统计信息缓存键
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="filterParameters">过滤参数</param>
    /// <returns>缓存键</returns>
    public string GenerateStatsKey<TItem>(PropertyFilterParameters? filterParameters = null) where TItem : class
    {
        var filterHash = filterParameters != null ? ComputeHash(SerializeFilterParameters(filterParameters)) : "all";
        return $"{_keyPrefix}Stats:{typeof(TItem).Name}:{filterHash}";
    }

    /// <summary>
    /// 生成无效化模式
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <returns>无效化模式</returns>
    public string GenerateInvalidationPattern<TItem>() where TItem : class
    {
        return $"{_keyPrefix}*:{typeof(TItem).Name}:*";
    }

    /// <summary>
    /// 生成表级别无效化模式
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <returns>表级别无效化模式</returns>
    public string GenerateTablePattern<TItem>() where TItem : class
    {
        return $"{_keyPrefix}*{typeof(TItem).Name}*";
    }

    /// <summary>
    /// 获取字段键
    /// </summary>
    private static string GetFieldKey(IFilterAction filter)
    {
        try
        {
            // 使用反射获取字段键
            var fieldKeyProperty = filter.GetType().GetProperty("FieldKey");
            return fieldKeyProperty?.GetValue(filter)?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取字段值
    /// </summary>
    private static object? GetFieldValue(IFilterAction filter)
    {
        try
        {
            var fieldValueProperty = filter.GetType().GetProperty("FieldValue");
            return fieldValueProperty?.GetValue(filter);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取过滤动作
    /// </summary>
    private static object? GetFilterAction(IFilterAction filter)
    {
        try
        {
            var filterActionProperty = filter.GetType().GetProperty("FilterAction");
            return filterActionProperty?.GetValue(filter);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 序列化过滤参数
    /// </summary>
    private static string SerializeFilterParameters(PropertyFilterParameters filterParameters)
    {
        try
        {
            return JsonSerializer.Serialize(filterParameters, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 计算字符串的MD5哈希
    /// </summary>
    private static string ComputeHash(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 获取实体缓存模式
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <returns>实体缓存模式</returns>
    public string GetEntityCachePattern<TItem>() where TItem : class
    {
        return $"{_keyPrefix}*:{typeof(TItem).Name}:*";
    }

    /// <summary>
    /// 获取查询缓存过期时间
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="options">查询选项</param>
    /// <returns>过期时间</returns>
    public TimeSpan GetQueryCacheExpiration(Type entityType, QueryPageOptions options)
    {
        // 根据查询类型和条件动态调整过期时间
        var baseMinutes = 30;

        // 如果有搜索或过滤条件，缓存时间较短
        if (options.Searches.Count > 0 || options.Filters.Count > 0 || options.AdvanceSearches.Count > 0)
        {
            baseMinutes = 10;
        }

        // 如果是第一页，可能是热点数据，延长缓存时间
        if (options.PageIndex == 1)
        {
            baseMinutes = Math.Min(baseMinutes * 2, 60);
        }

        return TimeSpan.FromMinutes(baseMinutes);
    }
}
