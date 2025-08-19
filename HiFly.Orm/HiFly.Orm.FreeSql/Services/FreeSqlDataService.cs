// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using FreeSql;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace HiFly.Orm.FreeSql.Services;

/// <summary>
/// FreeSql 数据服务实现
/// </summary>
/// <typeparam name="TItem">实体类型</typeparam>
public class FreeSqlDataService<TItem> : IHiFlyDataService<TItem>
    where TItem : class, new()
{
    /// <summary>
    /// FreeSql 实例
    /// </summary>
    protected readonly IFreeSql _freeSql;

    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger<FreeSqlDataService<TItem>> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="freeSql">FreeSql 实例</param>
    /// <param name="logger">日志记录器</param>
    public FreeSqlDataService(
        IFreeSql freeSql,
        ILogger<FreeSqlDataService<TItem>> logger)
    {
        _freeSql = freeSql ?? throw new ArgumentNullException(nameof(freeSql));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 查询数据
    /// </summary>
    public virtual async Task<QueryData<TItem>> OnQueryAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters = null,
        bool isTree = false)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            // 参数验证
            if (options.PageItems <= 0)
            {
                _logger.LogWarning("页面大小必须大于0，当前值: {PageItems}", options.PageItems);
                return new QueryData<TItem> { TotalCount = 0, Items = [] };
            }

            // 检查是否为树形表格，使用不同的数据加载策略
            if (isTree)
            {
                return await GetTreeQueryDataAsync(options, propertyFilterParameters);
            }
            else
            {
                return await GetStandardQueryDataAsync(options, propertyFilterParameters);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询数据时发生错误，选项: {@Options}", options);

            // 返回空结果而不是抛出异常
            return new QueryData<TItem>
            {
                TotalCount = 0,
                Items = [],
                IsSorted = false,
                IsFiltered = false,
                IsAdvanceSearch = false,
                IsSearch = false
            };
        }
    }

    /// <summary>
    /// 保存数据
    /// </summary>
    public virtual async Task<bool> OnSaveAsync(TItem item, ItemChangedType changedType)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            if (changedType == ItemChangedType.Add)
            {
                return await HandleAddAsync(item);
            }
            else // ItemChangedType.Update
            {
                return await HandleUpdateAsync(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存数据时发生错误，实体: {EntityType}, 变更类型: {ChangeType}",
                typeof(TItem).Name, changedType);
            return false;
        }
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    public virtual async Task<bool> OnDeleteAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemsList = items.ToList();
        if (itemsList.Count == 0)
        {
            return true; // 空集合视为成功
        }

        try
        {
            // 检查是否为树形结构
            var isTreeStructure = HasTreeStructure();

            if (isTreeStructure)
            {
                return await HandleTreeDeleteAsync(itemsList);
            }
            else
            {
                return await HandleStandardDeleteAsync(itemsList);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除数据时发生错误，实体: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 获取标准表格查询数据
    /// </summary>
    private async Task<QueryData<TItem>> GetStandardQueryDataAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters)
    {
        // 创建查询
        var query = _freeSql.Select<TItem>();

        // 应用过滤条件 - 简化实现
        if (propertyFilterParameters != null)
        {
            query = ApplySimpleFilter(query, propertyFilterParameters);
        }

        // 先获取总数
        var totalCount = await query.CountAsync();
        if (totalCount == 0)
        {
            return CreateEmptyQueryData(options);
        }

        // 应用排序
        if (!string.IsNullOrEmpty(options.SortName))
        {
            query = ApplySort(query, options.SortName, options.SortOrder);
        }
        else
        {
            // 默认排序，确保结果一致性
            query = ApplyDefaultSort(query);
        }

        // 应用分页
        var skipCount = (options.PageIndex - 1) * options.PageItems;
        var items = await query
            .Skip(skipCount)
            .Take(options.PageItems)
            .ToListAsync();

        return new QueryData<TItem>
        {
            TotalCount = (int)totalCount, // 转换为int
            Items = items,
            IsSorted = options.SortOrder != SortOrder.Unset,
            IsFiltered = options.Filters.Count != 0,
            IsAdvanceSearch = options.AdvanceSearches.Count != 0,
            IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
        };
    }

    /// <summary>
    /// 获取树形表格数据
    /// </summary>
    private async Task<QueryData<TItem>> GetTreeQueryDataAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters)
    {
        try
        {
            // 验证树形结构必需的属性
            var (idProp, parentIdProp) = ValidateTreeProperties();

            // 1. 构建根节点查询
            var rootQuery = _freeSql.Select<TItem>();

            // 应用根节点筛选条件（ParentId为null）
            rootQuery = ApplyRootNodeFilter(rootQuery, parentIdProp);

            // 应用排序
            if (!string.IsNullOrEmpty(options.SortName))
            {
                rootQuery = ApplySort(rootQuery, options.SortName, options.SortOrder);
            }
            else
            {
                rootQuery = ApplyDefaultSort(rootQuery);
            }

            // 获取根节点总数
            var totalCount = await rootQuery.CountAsync();
            if (totalCount == 0)
            {
                return CreateEmptyQueryData(options);
            }

            // 分页查询根节点
            var skipCount = (options.PageIndex - 1) * options.PageItems;
            var pagedRoots = await rootQuery
                .Skip(skipCount)
                .Take(options.PageItems)
                .ToListAsync();

            // 2. 为每个根节点加载完整的子树
            var allItems = new List<TItem>();
            foreach (var root in pagedRoots)
            {
                allItems.Add(root);
                await LoadChildNodesAsync(root, allItems, idProp, parentIdProp);
            }

            return new QueryData<TItem>
            {
                TotalCount = (int)totalCount,  // 转换为int
                Items = allItems,         // 返回包含所有子节点的集合
                IsSorted = options.SortOrder != SortOrder.Unset,
                IsFiltered = options.Filters.Count != 0,
                IsAdvanceSearch = options.AdvanceSearches.Count != 0,
                IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取树形表格数据时发生错误");

            // 降级到标准查询
            return await GetStandardQueryDataAsync(options, propertyFilterParameters);
        }
    }

    /// <summary>
    /// 应用简单过滤
    /// </summary>
    private static ISelect<TItem> ApplySimpleFilter(ISelect<TItem> query, PropertyFilterParameters filters)
    {
        try
        {
            return ApplyFilterRecursive(query, filters);
        }
        catch (Exception)
        {
            // 过滤失败时返回原查询
            return query;
        }
    }

    /// <summary>
    /// 递归应用过滤条件
    /// </summary>
    private static ISelect<TItem> ApplyFilterRecursive(ISelect<TItem> query, PropertyFilterParameters filter)
    {
        // 应用当前级别的过滤条件
        query = ApplySingleFilter(query, filter);

        // 应用子过滤条件
        if (filter.Filters?.Any() == true)
        {
            foreach (var subFilter in filter.Filters)
            {
                if (filter.FilterLogic == FilterLogic.And)
                {
                    query = ApplyFilterRecursive(query, subFilter);
                }
                else // FilterLogic.Or
                {
                    // 对于OR逻辑，需要更复杂的处理
                    // 这里先简化处理，实际应用中可以考虑使用Expression.OrElse
                    query = ApplyFilterRecursive(query, subFilter);
                }
            }
        }

        return query;
    }

    /// <summary>
    /// 应用单个过滤条件
    /// </summary>
    private static ISelect<TItem> ApplySingleFilter(ISelect<TItem> query, PropertyFilterParameters filter)
    {
        try
        {
            // 确定字段名称
            var fieldName = !string.IsNullOrEmpty(filter.ReferenceTypeField)
                ? filter.ReferenceTypeField
                : filter.ValueTypeField;

            if (string.IsNullOrEmpty(fieldName) || filter.MatchValue == null)
            {
                return query;
            }

            // 验证字段是否存在
            var property = typeof(TItem).GetProperty(fieldName);
            if (property == null)
            {
                return query;
            }

            // 根据过滤动作应用不同的条件
            return filter.FilterAction switch
            {
                FilterAction.Equal => ApplyEqualFilter(query, fieldName, filter.MatchValue),
                FilterAction.NotEqual => ApplyNotEqualFilter(query, fieldName, filter.MatchValue),
                FilterAction.GreaterThan => ApplyGreaterThanFilter(query, fieldName, filter.MatchValue),
                FilterAction.GreaterThanOrEqual => ApplyGreaterThanOrEqualFilter(query, fieldName, filter.MatchValue),
                FilterAction.LessThan => ApplyLessThanFilter(query, fieldName, filter.MatchValue),
                FilterAction.LessThanOrEqual => ApplyLessThanOrEqualFilter(query, fieldName, filter.MatchValue),
                FilterAction.Contains => ApplyContainsFilter(query, fieldName, filter.MatchValue),
                FilterAction.NotContains => ApplyNotContainsFilter(query, fieldName, filter.MatchValue),


                _ => query
            };
        }
        catch (Exception)
        {
            // 单个过滤条件失败时，返回原查询
            return query;
        }
    }

    /// <summary>
    /// 应用等于过滤
    /// </summary>
    private static ISelect<TItem> ApplyEqualFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var equal = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(equal, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用不等于过滤
    /// </summary>
    private static ISelect<TItem> ApplyNotEqualFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var notEqual = Expression.NotEqual(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(notEqual, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用大于过滤
    /// </summary>
    private static ISelect<TItem> ApplyGreaterThanFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var greaterThan = Expression.GreaterThan(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(greaterThan, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用大于等于过滤
    /// </summary>
    private static ISelect<TItem> ApplyGreaterThanOrEqualFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(greaterThanOrEqual, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用小于过滤
    /// </summary>
    private static ISelect<TItem> ApplyLessThanFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var lessThan = Expression.LessThan(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(lessThan, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用小于等于过滤
    /// </summary>
    private static ISelect<TItem> ApplyLessThanOrEqualFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var lessThanOrEqual = Expression.LessThanOrEqual(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(lessThanOrEqual, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用包含过滤（仅适用于字符串）
    /// </summary>
    private static ISelect<TItem> ApplyContainsFilter(ISelect<TItem> query, string fieldName, object value)
    {
        if (value is not string stringValue)
            return query;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var constant = Expression.Constant(stringValue);
        var contains = Expression.Call(property, containsMethod!, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(contains, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用不包含过滤（仅适用于字符串）
    /// </summary>
    private static ISelect<TItem> ApplyNotContainsFilter(ISelect<TItem> query, string fieldName, object value)
    {
        if (value is not string stringValue)
            return query;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var constant = Expression.Constant(stringValue);
        var contains = Expression.Call(property, containsMethod!, constant);
        var notContains = Expression.Not(contains);
        var lambda = Expression.Lambda<Func<TItem, bool>>(notContains, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用开始于过滤（仅适用于字符串）
    /// </summary>
    private static ISelect<TItem> ApplyStartsWithFilter(ISelect<TItem> query, string fieldName, object value)
    {
        if (value is not string stringValue)
            return query;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        var constant = Expression.Constant(stringValue);
        var startsWith = Expression.Call(property, startsWithMethod!, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(startsWith, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用结束于过滤（仅适用于字符串）
    /// </summary>
    private static ISelect<TItem> ApplyEndsWithFilter(ISelect<TItem> query, string fieldName, object value)
    {
        if (value is not string stringValue)
            return query;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        var constant = Expression.Constant(stringValue);
        var endsWith = Expression.Call(property, endsWithMethod!, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(endsWith, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    private static ISelect<TItem> ApplySort(ISelect<TItem> query, string sortName, SortOrder sortOrder)
    {
        if (string.IsNullOrEmpty(sortName))
            return query;

        try
        {
            // FreeSql 的 OrderBy 方法支持字符串，但 OrderByDescending 需要表达式
            if (sortOrder == SortOrder.Asc)
            {
                return query.OrderBy(sortName);
            }
            else if (sortOrder == SortOrder.Desc)
            {
                // 对于降序，我们使用 OrderBy + Desc 的方式
                // 或者先升序再反转，但这里我们用反射来构建表达式
                return ApplyDescendingSort(query, sortName);
            }
            else
            {
                return query;
            }
        }
        catch (Exception)
        {
            // 排序失败时返回原查询
            return query;
        }
    }

    /// <summary>
    /// 应用降序排序
    /// </summary>
    private static ISelect<TItem> ApplyDescendingSort(ISelect<TItem> query, string sortName)
    {
        try
        {
            // 构建 lambda 表达式: x => x.PropertyName
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, sortName);

            // 将属性转换为 object 类型以匹配 OrderByDescending<object> 签名
            var converted = Expression.Convert(property, typeof(object));
            var lambda = Expression.Lambda<Func<TItem, object>>(converted, parameter);

            return query.OrderByDescending(lambda);
        }
        catch (Exception)
        {
            // 如果构建表达式失败，使用默认排序
            return query.OrderBy(sortName);
        }
    }

    /// <summary>
    /// 应用默认排序
    /// </summary>
    protected virtual ISelect<TItem> ApplyDefaultSort(ISelect<TItem> query)
    {
        try
        {
            // 尝试按常见字段排序
            var createTimeProp = typeof(TItem).GetProperty("CreateTime");
            if (createTimeProp != null)
            {
                return query.OrderBy("CreateTime");
            }

            var idProp = typeof(TItem).GetProperty("Id");
            if (idProp != null)
            {
                return query.OrderBy("Id");
            }

            return query;
        }
        catch (Exception)
        {
            // 如果排序失败，返回原查询
            return query;
        }
    }

    /// <summary>
    /// 应用根节点过滤条件
    /// </summary>
    private static ISelect<TItem> ApplyRootNodeFilter(ISelect<TItem> query, PropertyInfo parentIdProp)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, parentIdProp);
        var nullValue = Expression.Constant(null, parentIdProp.PropertyType);
        var equalExpression = Expression.Equal(property, nullValue);
        var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

        return query.Where(lambda);
    }

    /// <summary>
    /// 检查是否具有树形结构
    /// </summary>
    private static bool HasTreeStructure()
    {
        var idProp = typeof(TItem).GetProperty("Id");
        var parentIdProp = typeof(TItem).GetProperty("ParentId");
        return idProp != null && parentIdProp != null;
    }

    /// <summary>
    /// 验证树形结构必需的属性
    /// </summary>
    private static (PropertyInfo idProp, PropertyInfo parentIdProp) ValidateTreeProperties()
    {
        var idProp = typeof(TItem).GetProperty("Id");
        var parentIdProp = typeof(TItem).GetProperty("ParentId");

        if (idProp == null)
        {
            throw new InvalidOperationException($"树形表格要求 {typeof(TItem).Name} 类有 Id 属性");
        }

        if (parentIdProp == null)
        {
            throw new InvalidOperationException($"树形表格要求 {typeof(TItem).Name} 类有 ParentId 属性");
        }

        return (idProp, parentIdProp);
    }

    /// <summary>
    /// 创建空的查询数据
    /// </summary>
    private static QueryData<TItem> CreateEmptyQueryData(QueryPageOptions options)
    {
        return new QueryData<TItem>
        {
            TotalCount = 0,
            Items = [],
            IsSorted = options.SortOrder != SortOrder.Unset,
            IsFiltered = options.Filters.Count != 0,
            IsAdvanceSearch = options.AdvanceSearches.Count != 0,
            IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
        };
    }

    /// <summary>
    /// 处理添加操作
    /// </summary>
    private async Task<bool> HandleAddAsync(TItem item)
    {
        try
        {
            var result = await _freeSql.Insert(item).ExecuteAffrowsAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加实体时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 处理更新操作
    /// </summary>
    private async Task<bool> HandleUpdateAsync(TItem item)
    {
        try
        {
            var result = await _freeSql.Update<TItem>().SetSource(item).ExecuteAffrowsAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新实体时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 处理标准删除
    /// </summary>
    private async Task<bool> HandleStandardDeleteAsync(List<TItem> items)
    {
        try
        {
            var result = await _freeSql.Delete<TItem>().Where(items).ExecuteAffrowsAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除实体时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 处理树形删除
    /// </summary>
    private async Task<bool> HandleTreeDeleteAsync(List<TItem> items)
    {
        try
        {
            var (idProp, parentIdProp) = ValidateTreeProperties();
            var allItemsToDelete = new List<TItem>();

            foreach (var item in items)
            {
                allItemsToDelete.Add(item);
                await CollectChildrenForDeletion(item, allItemsToDelete, idProp, parentIdProp);
            }

            var result = await _freeSql.Delete<TItem>().Where(allItemsToDelete).ExecuteAffrowsAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "树形删除时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 加载子节点
    /// </summary>
    private async Task LoadChildNodesAsync(TItem parent, List<TItem> collector, PropertyInfo idProp, PropertyInfo parentIdProp)
    {
        try
        {
            var parentId = idProp.GetValue(parent);
            if (parentId == null) return;

            // 构建查询表达式
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);

            // 处理类型转换
            Expression valueExpression;
            if (parentIdProp.PropertyType == idProp.PropertyType)
            {
                valueExpression = Expression.Constant(parentId, parentIdProp.PropertyType);
            }
            else if (Nullable.GetUnderlyingType(parentIdProp.PropertyType) == idProp.PropertyType)
            {
                // ParentId 是可空类型，Id 不是
                valueExpression = Expression.Convert(Expression.Constant(parentId, idProp.PropertyType), parentIdProp.PropertyType);
            }
            else
            {
                // 尝试类型转换
                valueExpression = Expression.Convert(Expression.Constant(parentId, idProp.PropertyType), parentIdProp.PropertyType);
            }

            var equalExpression = Expression.Equal(property, valueExpression);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            var children = await _freeSql.Select<TItem>().Where(lambda).ToListAsync();

            foreach (var child in children)
            {
                collector.Add(child);
                await LoadChildNodesAsync(child, collector, idProp, parentIdProp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载子节点时发生错误: {EntityType}", typeof(TItem).Name);
        }
    }

    /// <summary>
    /// 收集要删除的子节点
    /// </summary>
    private async Task CollectChildrenForDeletion(TItem parent, List<TItem> collector, PropertyInfo idProp, PropertyInfo parentIdProp)
    {
        try
        {
            var parentId = idProp.GetValue(parent);
            if (parentId == null) return;

            // 构建查询表达式
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);

            // 处理类型转换
            Expression valueExpression;
            if (parentIdProp.PropertyType == idProp.PropertyType)
            {
                valueExpression = Expression.Constant(parentId, idProp.PropertyType);
            }
            else if (Nullable.GetUnderlyingType(parentIdProp.PropertyType) == idProp.PropertyType)
            {
                // ParentId 是可空类型，Id 不是
                valueExpression = Expression.Convert(Expression.Constant(parentId, idProp.PropertyType), parentIdProp.PropertyType);
            }
            else
            {
                // 尝试类型转换
                valueExpression = Expression.Convert(Expression.Constant(parentId, idProp.PropertyType), parentIdProp.PropertyType);
            }

            var equalExpression = Expression.Equal(property, valueExpression);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            var children = await _freeSql.Select<TItem>().Where(lambda).ToListAsync();

            foreach (var child in children)
            {
                collector.Add(child);
                await CollectChildrenForDeletion(child, collector, idProp, parentIdProp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收集要删除的子节点时发生错误: {EntityType}", typeof(TItem).Name);
        }
    }

    /// <summary>
    /// 资源释放
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
