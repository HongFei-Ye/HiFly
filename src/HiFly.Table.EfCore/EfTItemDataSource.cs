// SPDX-License-Identifier: Apache-2.0

using BootstrapBlazor.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using HiFly.Table.Extensions;
using System.Linq.Expressions;
using System.Reflection;

namespace HiFly.Table.DataSources;

/// <summary>
/// <see cref="ITItemDataSource{TItem}"/> 的 EF 实现 —— co-located 模式下（BC.UI 挂在 BC.Host 内嵌），
/// 封装 TItemTable 原来 45 处直接 DbContext 调用，保留 LINQ / Include / 租户过滤 / 软删除 / 审计链路。
/// <para>
/// 每个 TItemTable 实例在 <c>OnInitializedAsync</c> 时 <c>new EfTItemDataSource&lt;TContext, TItem&gt;(dbFactory, options)</c>
/// 自用（非 DI-singleton），Options 携带该页特有配置（Include 链、IsTree、ExtraDataLoader、CacheSync）。
/// </para>
/// </summary>
public sealed class EfTItemDataSource<TContext, TItem> : ITItemDataSource<TItem>
    where TContext : DbContext
    where TItem : class, new()
{
    private readonly IDbContextFactory<TContext> _dbFactory;
    private readonly EfTItemDataSourceOptions<TItem> _options;

    public EfTItemDataSource(IDbContextFactory<TContext> dbFactory, EfTItemDataSourceOptions<TItem>? options = null)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _options = options ?? new EfTItemDataSourceOptions<TItem>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Query
    // ─────────────────────────────────────────────────────────────────────

    public async Task<QueryData<TItem>> QueryAsync(QueryPageOptions options, CancellationToken cancellationToken = default)
    {
        var finalFilterParameters = BuildFilterParameters(options, _options.PropertyFilterParameters);
        var hasExtraDataLoader = _options.ExtraDataLoader?.HasLoaders == true;

        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (_options.IsTree)
        {
            var parentIdProp = typeof(TItem).GetProperty("ParentId")
                ?? throw new InvalidOperationException($"树形表格要求{typeof(TItem).Name}类有ParentId属性");

            var hasFilter = finalFilterParameters != null &&
                             (options.Searches.Count > 0 ||
                              finalFilterParameters.Filters?.Count > 0 ||
                              options.AdvanceSearches.Count > 0);

            QueryData<TItem> treeResult;
            if (hasFilter)
            {
                treeResult = await SearchTreeWithFilter(context, options, finalFilterParameters, parentIdProp, cancellationToken);
            }
            else
            {
                treeResult = await LoadTreeRootNodes(context, options, parentIdProp, cancellationToken);
            }

            if (hasExtraDataLoader)
            {
                await _options.ExtraDataLoader!.LoadAllAsync(context, treeResult.Items);
            }

            return treeResult;
        }

        // 普通表格
        IQueryable<TItem> query = context.Set<TItem>();
        query = ApplyIncludes(query);

        query = query
            .AsNoTracking()
            .AutoFilter(finalFilterParameters)
            .Sort(options.SortName!, options.SortOrder, !string.IsNullOrEmpty(options.SortName))
            .Count(out var count)
            .Page((options.PageIndex - 1) * options.PageItems, options.PageItems);

        if (hasExtraDataLoader)
        {
            var itemList = await query.ToListAsync(cancellationToken);
            await _options.ExtraDataLoader!.LoadAllAsync(context, itemList);

            return new QueryData<TItem>
            {
                TotalCount = count,
                Items = itemList,
                IsSorted = options.SortOrder != SortOrder.Unset,
                IsFiltered = options.Filters.Count != 0,
                IsAdvanceSearch = options.AdvanceSearches.Count != 0,
                IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0,
            };
        }

        // 物化为 List —— 原代码把 IQueryable 直接塞进 QueryData.Items（懒加载），切 HTTP 实现后必物化，
        // 这里 EF 模式也物化以对齐行为、避免 Context dispose 后 BootstrapBlazor Table 再枚举出错。
        var materialized = await query.ToListAsync(cancellationToken);
        return new QueryData<TItem>
        {
            TotalCount = count,
            Items = materialized,
            IsSorted = options.SortOrder != SortOrder.Unset,
            IsFiltered = options.Filters.Count != 0,
            IsAdvanceSearch = options.AdvanceSearches.Count != 0,
            IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0,
        };
    }

    private async Task<QueryData<TItem>> LoadTreeRootNodes(
        DbContext context, QueryPageOptions options, PropertyInfo parentIdProp, CancellationToken ct)
    {
        IQueryable<TItem> rootQuery = context.Set<TItem>();
        rootQuery = ApplyIncludes(rootQuery);
        rootQuery = rootQuery.AsNoTracking();

        // ParentId == null 构建
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, parentIdProp);
        var nullValue = Expression.Constant(null, parentIdProp.PropertyType);
        var equalExpression = Expression.Equal(property, nullValue);
        var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

        rootQuery = rootQuery.Where(lambda);

        if (!string.IsNullOrEmpty(options.SortName))
        {
            rootQuery = rootQuery.Sort(options.SortName, options.SortOrder);
        }

        var totalCount = await rootQuery.CountAsync(ct);
        var pagedRootItems = await rootQuery
            .Skip((options.PageIndex - 1) * options.PageItems)
            .Take(options.PageItems)
            .ToListAsync(ct);

        return new QueryData<TItem>
        {
            TotalCount = totalCount,
            Items = pagedRootItems,
            IsSorted = options.SortOrder != SortOrder.Unset,
            IsFiltered = false,
            IsAdvanceSearch = false,
            IsSearch = false,
        };
    }

    private async Task<QueryData<TItem>> SearchTreeWithFilter(
        DbContext context,
        QueryPageOptions options,
        PropertyFilterParameters? filterParams,
        PropertyInfo parentIdProp,
        CancellationToken ct)
    {
        try
        {
            var idProp = typeof(TItem).GetProperty("Id")
                ?? throw new InvalidOperationException($"{typeof(TItem).Name} 必须有 Id 属性");

            // 1. 搜索所有匹配节点
            IQueryable<TItem> allMatchingNodes = context.Set<TItem>().AsNoTracking();
            if (filterParams != null)
            {
                allMatchingNodes = allMatchingNodes.AutoFilter(filterParams);
            }

            var matchingIds = await allMatchingNodes
                .Select(e => EF.Property<Guid>(e, "Id"))
                .ToListAsync(ct);

            if (matchingIds.Count == 0)
            {
                return new QueryData<TItem>
                {
                    TotalCount = 0,
                    Items = [],
                    IsSorted = options.SortOrder != SortOrder.Unset,
                    IsFiltered = true,
                    IsSearch = true,
                };
            }

            // 2. 匹配 + 所有祖先
            var allRelatedIds = new HashSet<Guid>(matchingIds);
            IQueryable<TItem> allNodesQuery = context.Set<TItem>();
            allNodesQuery = ApplyIncludes(allNodesQuery);
            var allNodes = await allNodesQuery.AsNoTracking().ToListAsync(ct);

            foreach (var matchId in matchingIds)
            {
                AddAncestors(matchId, allNodes, parentIdProp, idProp, allRelatedIds);
            }

            // 3. 过滤 + 排序 + 分页（只对根节点分页）
            var relatedNodes = allNodes
                .Where(n => allRelatedIds.Contains((Guid)idProp.GetValue(n)!))
                .ToList();

            // 只返回根节点
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);
            var nullValue = Expression.Constant(null, parentIdProp.PropertyType);
            var equalExpression = Expression.Equal(property, nullValue);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);
            var compiled = lambda.Compile();

            var rootNodes = relatedNodes.Where(compiled).ToList();

            if (!string.IsNullOrEmpty(options.SortName))
            {
                var sortProp = typeof(TItem).GetProperty(options.SortName);
                if (sortProp != null)
                {
                    rootNodes = options.SortOrder == SortOrder.Desc
                        ? [.. rootNodes.OrderByDescending(n => sortProp.GetValue(n))]
                        : [.. rootNodes.OrderBy(n => sortProp.GetValue(n))];
                }
            }

            var totalCount = rootNodes.Count;
            var pagedRootNodes = rootNodes
                .Skip((options.PageIndex - 1) * options.PageItems)
                .Take(options.PageItems)
                .ToList();

            return new QueryData<TItem>
            {
                TotalCount = totalCount,
                Items = pagedRootNodes,
                IsSorted = options.SortOrder != SortOrder.Unset,
                IsFiltered = true,
                IsSearch = true,
                IsAdvanceSearch = options.AdvanceSearches.Count > 0,
            };
        }
        catch
        {
            // 树搜索失败回退到根节点
            return await LoadTreeRootNodes(context, options, parentIdProp, ct);
        }
    }

    private static void AddAncestors(
        Guid nodeId,
        List<TItem> allNodes,
        PropertyInfo parentIdProp,
        PropertyInfo idProp,
        HashSet<Guid> collectedIds)
    {
        var current = allNodes.FirstOrDefault(n =>
            ((Guid)idProp.GetValue(n)!).Equals(nodeId));

        while (current != null)
        {
            var parentIdValue = parentIdProp.GetValue(current);
            if (parentIdValue == null) break;

            var parentId = (Guid)parentIdValue;
            if (!collectedIds.Add(parentId)) break;

            current = allNodes.FirstOrDefault(n =>
                ((Guid)idProp.GetValue(n)!).Equals(parentId));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Save
    // ─────────────────────────────────────────────────────────────────────

    public async Task<int> SaveAsync(TItem item, ItemChangedType changedType, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var entityType = context.Model.FindEntityType(typeof(TItem))
            ?? throw new InvalidOperationException($"实体类型 {typeof(TItem).Name} 不在模型中。");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"实体类型 {typeof(TItem).Name} 没有定义主键。");

        var isCompositeKey = primaryKey.Properties.Count > 1;

        if (changedType == ItemChangedType.Add)
        {
            var entityExists = false;
            var keyValues = primaryKey.Properties
                .Select(p => typeof(TItem).GetProperty(p.Name)?.GetValue(item))
                .ToArray();

            if (!keyValues.Any(v => v == null))
            {
                if (!isCompositeKey)
                {
                    var existingEntity = await context.Set<TItem>().FindAsync(keyValues, cancellationToken);
                    entityExists = existingEntity != null;
                }
                else
                {
                    var existingEntity = await FindEntityWithComplexKey(context, item, primaryKey, cancellationToken);
                    entityExists = existingEntity != null;
                }
            }

            if (entityExists)
            {
                throw new InvalidOperationException("当前数据已存在，禁止再次加入！");
            }

            await context.Set<TItem>().AddAsync(item, cancellationToken);
            return await context.SaveChangesAsync(cancellationToken);
        }

        // Update
        TItem? existing = null;
        var updateKeyValues = primaryKey.Properties
            .Select(p => typeof(TItem).GetProperty(p.Name)?.GetValue(item))
            .ToArray();

        if (!updateKeyValues.Any(v => v == null))
        {
            if (!isCompositeKey)
            {
                existing = await context.Set<TItem>().FindAsync(updateKeyValues, cancellationToken);
            }
            else
            {
                existing = await FindEntityWithComplexKey(context, item, primaryKey, cancellationToken);
            }
        }

        if (existing != null)
        {
            context.Entry(existing).CurrentValues.SetValues(item);
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<TItem?> FindEntityWithComplexKey(DbContext context, TItem item, IKey primaryKey, CancellationToken ct)
    {
        try
        {
            var keyValues = new List<object?>();
            var keyProps = primaryKey.Properties.Select(p => p.Name).ToArray();

            var keyDict = new Dictionary<string, object?>();
            foreach (var propName in keyProps)
            {
                var prop = typeof(TItem).GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(item);
                    if (value != null)
                    {
                        keyDict.Add(propName, value);
                        keyValues.Add(value);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            // 1. 先尝试 Find（支持复合主键）
            try
            {
                var entity = await context.Set<TItem>().FindAsync([.. keyValues], ct);
                if (entity != null) return entity;
            }
            catch { /* Find 失败 fallback 到 LINQ */ }

            // 2. LINQ fallback —— 动态属性匹配
            IQueryable<TItem> query = context.Set<TItem>();
            foreach (var kvp in keyDict)
            {
                var propName = kvp.Key;
                var propValue = kvp.Value;
                query = query.Where(e => EF.Property<object>(e, propName)!.Equals(propValue));
            }

            return await query.FirstOrDefaultAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Delete
    // ─────────────────────────────────────────────────────────────────────

    public async Task<int> DeleteAsync(IEnumerable<TItem> items, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (_options.IsTree)
        {
            // 树模式：收集选中项 + 所有后代
            var allIdsToDelete = new HashSet<string>();
            foreach (var item in items)
            {
                var itemId = GetEntityId(item);
                if (itemId != null) allIdsToDelete.Add(itemId);
                await CollectChildNodeIdsForDelete(context, item, allIdsToDelete, cancellationToken);
            }

            var idsToDelete = allIdsToDelete.Select(id => Guid.Parse(id)).ToList();
            var entitiesToDelete = await context.Set<TItem>()
                .Where(e => idsToDelete.Contains(EF.Property<Guid>(e, "Id")))
                .ToListAsync(cancellationToken);

            if (entitiesToDelete.Count == 0)
            {
                throw new InvalidOperationException("未找到要删除的数据，可能已被其他用户删除");
            }

            context.Set<TItem>().RemoveRange(entitiesToDelete);
            return await context.SaveChangesAsync(cancellationToken);
        }

        // 普通表格删除：传入的 items 可能未被当前 context tracked，按主键重查再删
        var materializedItems = items as IList<TItem> ?? items.ToList();
        if (materializedItems.Count == 0) return 0;

        var entityType = context.Model.FindEntityType(typeof(TItem))
            ?? throw new InvalidOperationException($"实体类型 {typeof(TItem).Name} 不在模型中。");
        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"实体类型 {typeof(TItem).Name} 没有定义主键。");
        var isCompositeKey = primaryKey.Properties.Count > 1;

        var toDelete = new List<TItem>();
        foreach (var item in materializedItems)
        {
            TItem? tracked = null;
            var keyValues = primaryKey.Properties
                .Select(p => typeof(TItem).GetProperty(p.Name)?.GetValue(item))
                .ToArray();

            if (!keyValues.Any(v => v == null))
            {
                tracked = isCompositeKey
                    ? await FindEntityWithComplexKey(context, item, primaryKey, cancellationToken)
                    : await context.Set<TItem>().FindAsync(keyValues, cancellationToken);
            }
            if (tracked != null) toDelete.Add(tracked);
        }

        if (toDelete.Count == 0) return 0;
        context.Set<TItem>().RemoveRange(toDelete);
        return await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task CollectChildNodeIdsForDelete(DbContext context, TItem parent, HashSet<string> idCollector, CancellationToken ct)
    {
        try
        {
            var idProp = typeof(TItem).GetProperty("Id");
            var parentIdProp = typeof(TItem).GetProperty("ParentId");

            if (idProp == null || parentIdProp == null) return;

            var parentId = idProp.GetValue(parent)?.ToString();
            if (string.IsNullOrEmpty(parentId)) return;

            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);
            var value = Expression.Constant(parentId, parentIdProp.PropertyType);
            var equalExpression = Expression.Equal(property, value);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            var childIds = await context.Set<TItem>()
                .AsNoTracking()
                .Where(lambda)
                .Select(e => EF.Property<Guid>(e, "Id").ToString())
                .ToListAsync(ct);

            foreach (var childId in childIds)
            {
                if (idCollector.Add(childId))
                {
                    var tempChild = new TItem();
                    idProp.SetValue(tempChild, Guid.Parse(childId));
                    await CollectChildNodeIdsForDelete(context, tempChild, idCollector, ct);
                }
            }
        }
        catch
        {
            // 错误但继续 —— 与 TItemTable 原行为一致
        }
    }

    private static string? GetEntityId(TItem item)
    {
        var idProp = typeof(TItem).GetProperty("Id");
        return idProp?.GetValue(item)?.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Include 辅助 —— 从 TItemTable 原逻辑直接搬迁
    // ─────────────────────────────────────────────────────────────────────

    private IQueryable<TItem> ApplyIncludes(IQueryable<TItem> query)
    {
        if (_options.IncludeNavigationConfig == null) return query;

        // 情况 1：单根 Include
        if (!string.IsNullOrEmpty(_options.IncludeNavigationConfig.PropertyName))
        {
            query = query.Include(_options.IncludeNavigationConfig.PropertyName);
            if (_options.IncludeNavigationConfig.ThenIncludes is { Count: > 0 })
            {
                query = ApplyNestedThenIncludes(query, _options.IncludeNavigationConfig.PropertyName, _options.IncludeNavigationConfig.ThenIncludes);
            }
        }
        // 情况 2：多平级 Include（无 PropertyName，只有 ThenIncludes）
        else if (_options.IncludeNavigationConfig.ThenIncludes is { Count: > 0 })
        {
            foreach (var config in _options.IncludeNavigationConfig.ThenIncludes)
            {
                if (!string.IsNullOrEmpty(config.PropertyName))
                {
                    query = query.Include(config.PropertyName);
                    if (config.ThenIncludes is { Count: > 0 })
                    {
                        query = ApplyNestedThenIncludes(query, config.PropertyName, config.ThenIncludes);
                    }
                }
            }
        }

        return query;
    }

    private static IQueryable<TItem> ApplyNestedThenIncludes(
        IQueryable<TItem> query, string basePath, List<IncludeNavigationConfig> configs)
    {
        foreach (var config in configs)
        {
            if (!string.IsNullOrEmpty(config.PropertyName))
            {
                var fullPath = $"{basePath}.{config.PropertyName}";
                query = query.Include(fullPath);
                if (config.ThenIncludes is { Count: > 0 })
                {
                    query = ApplyNestedThenIncludes(query, fullPath, config.ThenIncludes);
                }
            }
        }
        return query;
    }

    /// <summary>
    /// 合并 QueryPageOptions.Searches 推出的 filter 与页级 PropertyFilterParameters（不同来源的过滤条件叠加）。
    /// 对齐 TItemTable 原 BuildFilterParameters 行为：一方为空 → 另一方；都有 → 新建 Parameters 集装两者。
    /// </summary>
    private static PropertyFilterParameters? BuildFilterParameters(
        QueryPageOptions options, PropertyFilterParameters? pageLevel)
    {
        var searches = options.ToFilter();
        if (searches != null)
        {
            var searchParameters = searches.ToPropertyFilterParameters();
            if (pageLevel == null) return searchParameters;

            var merged = new PropertyFilterParameters();
            merged.Add(pageLevel);
            merged.Add(searchParameters);
            return merged;
        }

        return pageLevel;
    }
}
