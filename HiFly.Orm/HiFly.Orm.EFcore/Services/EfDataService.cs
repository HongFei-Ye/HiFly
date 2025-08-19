// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Orm.EFcore.Extensions;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace HiFly.Orm.EFcore.Services;

/// <summary>
/// Entity Framework Core 数据服务实现
/// </summary>
/// <typeparam name="TContext">数据库上下文类型</typeparam>
/// <typeparam name="TItem">实体类型</typeparam>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="dbContextFactory">数据库上下文工厂</param>
/// <param name="logger">日志记录器</param>
public class EfDataService<TContext, TItem>(
    IDbContextFactory<TContext> dbContextFactory,
    ILogger<EfDataService<TContext, TItem>> logger) : IHiFlyDataService<TItem>
    where TContext : DbContext
    where TItem : class, new()
{
    /// <summary>
    /// 数据库上下文工厂
    /// </summary>
    protected readonly IDbContextFactory<TContext> _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));

    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger<EfDataService<TContext, TItem>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// 查询数据
    /// </summary>
    public virtual async Task<QueryData<TItem>> OnQueryAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters = null,
        bool isTree = false)
    {
        ArgumentNullException.ThrowIfNull(options);

        // 处理过滤与搜索逻辑
        var finalFilterParameters = BuildFilterParameters(options, propertyFilterParameters);

        try
        {
            // 参数验证
            if (options.PageItems <= 0)
            {
                _logger.LogWarning("页面大小必须大于0，当前值: {PageItems}", options.PageItems);
                return new QueryData<TItem> { TotalCount = 0, Items = [] };
            }

            using var context = _dbContextFactory.CreateDbContext();

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
    /// 保存数据
    /// </summary>
    public virtual async Task<bool> OnSaveAsync(TItem item, ItemChangedType changedType)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            using var context = _dbContextFactory.CreateDbContext();

            // 获取实体类型元数据
            var entityType = context.Model.FindEntityType(typeof(TItem))
                ?? throw new InvalidOperationException($"实体类型 {typeof(TItem).Name} 不在模型中。");

            // 获取主键属性
            var primaryKey = entityType.FindPrimaryKey()
                ?? throw new InvalidOperationException($"实体类型 {typeof(TItem).Name} 没有定义主键。");

            if (changedType == ItemChangedType.Add)
            {
                return await EfSaveExtensions.HandleAddAsync(context, item, primaryKey, _logger);
            }
            else // ItemChangedType.Update
            {
                return await EfSaveExtensions.HandleUpdateAsync(context, item, primaryKey, _logger);
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
            using var context = _dbContextFactory.CreateDbContext();

            // 检查是否为树形结构
            var isTreeStructure = HasTreeStructure();

            if (isTreeStructure)
            {
                return await EfDeleteExtensions.HandleTreeDeleteAsync(context, itemsList, _logger);
            }
            else
            {
                return await EfDeleteExtensions.HandleStandardDeleteAsync(context, itemsList, _logger);
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
        TContext context,
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters)
    {
        // 使用 AsNoTracking 提升只读查询性能
        var baseQuery = context.Set<TItem>().AsNoTracking();

        // 应用过滤条件 - 使用完整的过滤功能
        if (propertyFilterParameters != null)
        {
            baseQuery = baseQuery.ApplySmartFilter(propertyFilterParameters);
        }

        // 先获取总数
        var totalCount = await baseQuery.CountAsync();
        if (totalCount == 0)
        {
            return CreateEmptyQueryData(options);
        }

        // 应用排序
        if (!string.IsNullOrEmpty(options.SortName))
        {
            baseQuery = baseQuery.ApplySort(options.SortName, options.SortOrder);
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
        PropertyFilterParameters? propertyFilterParameters)
    {
        try
        {
            // 验证树形结构必需的属性
            var (idProp, parentIdProp) = ValidateTreeProperties();

            // 1. 构建根节点查询
            var rootQuery = context.Set<TItem>().AsNoTracking();

            // 应用根节点筛选条件（ParentId为null）
            rootQuery = ApplyRootNodeFilter(rootQuery, parentIdProp);

            // 应用排序
            if (!string.IsNullOrEmpty(options.SortName))
            {
                rootQuery = rootQuery.ApplySort(options.SortName, options.SortOrder);
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
                await EfTreeExtensions.LoadChildNodesAsync(context, root, allItems, idProp, parentIdProp, _logger);
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
            return await GetStandardQueryDataAsync(context, options, propertyFilterParameters);
        }
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
        GC.SuppressFinalize(this);
    }
}
