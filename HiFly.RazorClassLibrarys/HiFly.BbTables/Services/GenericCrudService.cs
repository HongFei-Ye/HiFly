// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.BbTables.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace HiFly.BbTables.Services;

/// <summary>
/// 泛型 CRUD 服务基类
/// </summary>
/// <typeparam name="TContext">数据库上下文类型</typeparam>
/// <typeparam name="TItem">实体类型</typeparam>
public class GenericCrudService<TContext, TItem>(
    IDbContextFactory<TContext> dbContextFactory,
    ILogger<GenericCrudService<TContext, TItem>> logger) : IDisposable
    where TContext : DbContext
    where TItem : class, new()
{
    protected readonly IDbContextFactory<TContext> _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    protected readonly ILogger<GenericCrudService<TContext, TItem>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 默认查询方法，支持普通表格和树形表格
    /// </summary>
    /// <param name="options">查询选项</param>
    /// <param name="propertyFilterParameters">属性过滤参数</param>
    /// <param name="isTree">是否为树形表格</param>
    /// <returns>查询数据</returns>
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

            using var context = _dbContextFactory.CreateDbContext();

            // 处理过滤与搜索逻辑
            var finalFilterParameters = GenericCrudService<TContext, TItem>.BuildFilterParameters(options, propertyFilterParameters);

            // 检查是否为树形表格，使用不同的数据加载策略
            if (isTree)
            {
                return await GetTreeQueryDataAsync(context, options, finalFilterParameters);
            }
            else
            {
                return await GetStandardQueryDataAsync(context, options, finalFilterParameters);
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
    /// 构建过滤参数
    /// </summary>
    private static PropertyFilterParameters? BuildFilterParameters(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters)
    {
        PropertyFilterParameters? finalFilterParameters = null;

        var searches = options.ToFilter();
        if (searches != null)
        {
            var searchParameters = searches.ToPropertyFilterParameters();
            if (propertyFilterParameters == null)
            {
                finalFilterParameters = searchParameters;
            }
            else
            {
                // 避免修改原始参数，创建新实例
                finalFilterParameters = new PropertyFilterParameters();
                finalFilterParameters.Add(propertyFilterParameters);
                finalFilterParameters.Add(searchParameters);
            }
        }
        else
        {
            finalFilterParameters = propertyFilterParameters;
        }

        return finalFilterParameters;
    }

    /// <summary>
    /// 获取标准表格查询数据
    /// </summary>
    private async Task<QueryData<TItem>> GetStandardQueryDataAsync(
        TContext context,
        QueryPageOptions options,
        PropertyFilterParameters? finalFilterParameters)
    {
        // 使用 AsNoTracking 提升只读查询性能
        var baseQuery = context.Set<TItem>().AsNoTracking();

        // 应用过滤条件
        if (finalFilterParameters != null)
        {
            baseQuery = baseQuery.AutoFilter(finalFilterParameters);
        }

        // 先获取总数
        var totalCount = await baseQuery.CountAsync();
        if (totalCount == 0)
        {
            return GenericCrudService<TContext, TItem>.CreateEmptyQueryData(options);
        }

        // 应用排序
        if (!string.IsNullOrEmpty(options.SortName))
        {
            baseQuery = baseQuery.Sort(options.SortName, options.SortOrder, true);
        }
        else
        {
            // 默认排序，确保结果一致性
            baseQuery = ApplyDefaultSort(baseQuery);
        }

        // 应用分页
        var skipCount = (options.PageIndex - 1) * options.PageItems;
        var items = await baseQuery
            .Skip(skipCount)
            .Take(options.PageItems)
            .ToListAsync();

        return new QueryData<TItem>
        {
            TotalCount = totalCount,
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
        TContext context,
        QueryPageOptions options,
        PropertyFilterParameters? filterParams)
    {
        try
        {
            // 验证树形结构必需的属性
            var (idProp, parentIdProp) = GenericCrudService<TContext, TItem>.ValidateTreeProperties();

            // 1. 构建根节点查询
            var rootQuery = context.Set<TItem>().AsNoTracking();

            // 应用过滤条件
            if (filterParams != null)
            {
                rootQuery = rootQuery.AutoFilter(filterParams);
            }

            // 应用根节点筛选条件（ParentId为null）
            rootQuery = GenericCrudService<TContext, TItem>.ApplyRootNodeFilter(rootQuery, parentIdProp);

            // 应用排序
            if (!string.IsNullOrEmpty(options.SortName))
            {
                rootQuery = rootQuery.Sort(options.SortName, options.SortOrder);
            }
            else
            {
                rootQuery = ApplyDefaultSort(rootQuery);
            }

            // 获取根节点总数
            var totalCount = await rootQuery.CountAsync();
            if (totalCount == 0)
            {
                return GenericCrudService<TContext, TItem>.CreateEmptyQueryData(options);
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
                await LoadChildNodesAsync(context, root, allItems, idProp, parentIdProp);
            }

            return new QueryData<TItem>
            {
                TotalCount = totalCount,  // 总数是根节点的数量
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
            return await GetStandardQueryDataAsync(context, options, filterParams);
        }
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
    /// 应用根节点过滤条件
    /// </summary>
    private static IQueryable<TItem> ApplyRootNodeFilter(
        IQueryable<TItem> query,
        PropertyInfo parentIdProp)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, parentIdProp);
        var nullValue = Expression.Constant(null, parentIdProp.PropertyType);
        var equalExpression = Expression.Equal(property, nullValue);
        var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

        return query.Where(lambda);
    }

    /// <summary>
    /// 应用默认排序
    /// </summary>
    protected virtual IQueryable<TItem> ApplyDefaultSort(IQueryable<TItem> query)
    {
        // 尝试按常见字段排序
        var createTimeProp = typeof(TItem).GetProperty("CreateTime");
        if (createTimeProp != null)
        {
            return query.OrderBy(x => EF.Property<object>(x, "CreateTime"));
        }

        var idProp = typeof(TItem).GetProperty("Id");
        if (idProp != null)
        {
            return query.OrderBy(x => EF.Property<object>(x, "Id"));
        }

        return query;
    }

    /// <summary>
    /// 递归加载子节点
    /// </summary>
    private async Task LoadChildNodesAsync(
        TContext context,
        TItem parent,
        List<TItem> collector,
        PropertyInfo idProp,
        PropertyInfo parentIdProp,
        int maxDepth = 10,
        int currentDepth = 0)
    {
        // 防止无限递归
        if (currentDepth >= maxDepth)
        {
            _logger.LogWarning("树形结构递归深度超过最大限制 {MaxDepth}", maxDepth);
            return;
        }

        var parentIdValue = idProp.GetValue(parent);
        if (parentIdValue == null)
            return;

        try
        {
            // 构建查询表达式以查找子节点
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);

            // 处理类型转换
            var convertedValue = CreateConvertedExpression(parentIdValue, idProp, parentIdProp);
            var equalExpression = Expression.Equal(property, convertedValue);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            // 查询子节点
            var children = await context.Set<TItem>()
                .AsNoTracking()
                .Where(lambda)
                .ToListAsync();

            // 添加子节点并继续递归
            foreach (var child in children)
            {
                // 防止循环引用
                if (!collector.Any(x => GenericCrudService<TContext, TItem>.IsEqual(idProp.GetValue(x), idProp.GetValue(child))))
                {
                    collector.Add(child);
                    await LoadChildNodesAsync(context, child, collector, idProp, parentIdProp, maxDepth, currentDepth + 1);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载子节点时发生错误，父节点ID: {ParentId}", parentIdValue);
        }
    }

    /// <summary>
    /// 创建类型转换表达式
    /// </summary>
    private static Expression CreateConvertedExpression(
        object parentIdValue,
        PropertyInfo idProp,
        PropertyInfo parentIdProp)
    {
        var value = Expression.Constant(parentIdValue, idProp.PropertyType);

        // 如果 ParentId 是可空类型，需要将 Id 值转换为可空类型
        if (Nullable.GetUnderlyingType(parentIdProp.PropertyType) != null)
        {
            return Expression.Convert(value, parentIdProp.PropertyType);
        }

        // 如果类型不匹配，尝试转换
        if (idProp.PropertyType != parentIdProp.PropertyType)
        {
            return Expression.Convert(value, parentIdProp.PropertyType);
        }

        return value;
    }

    /// <summary>
    /// 比较两个对象是否相等
    /// </summary>
    private static bool IsEqual(object? obj1, object? obj2)
    {
        if (obj1 == null && obj2 == null) return true;
        if (obj1 == null || obj2 == null) return false;
        return obj1.Equals(obj2);
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
    /// 资源释放
    /// </summary>
    public virtual void Dispose()
    {
        // 基类实现为空，子类可以重写
    }



}

/// <summary>
/// 标记接口，用于标识需要 CRUD 服务的实体
/// </summary>
public interface ICrudEntity
{
}
