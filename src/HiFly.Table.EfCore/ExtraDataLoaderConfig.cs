// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore;

namespace HiFly.Table;

/// <summary>
/// 额外数据加载器配置 - 用于配置查询完成后需要额外加载的数据
/// 设计理念类似于 IncludeNavigationConfig，但用于处理无法通过 EF Include 加载的数据（如 NotMapped 属性）
/// </summary>
/// <typeparam name="TItem">实体类型</typeparam>
public class ExtraDataLoaderConfig<TItem> : IExtraDataLoaderConfig<TItem> where TItem : class
{
    /// <summary>
    /// 数据加载器列表
    /// </summary>
    private List<Func<DbContext, IEnumerable<TItem>, Task>> Loaders { get; } = [];

    /// <summary>
    /// 创建空配置
    /// </summary>
    public static ExtraDataLoaderConfig<TItem> Create() => new();

    /// <summary>
    /// 添加数据加载器
    /// </summary>
    /// <param name="loadAsync">异步加载委托，接收 DbContext 和实体集合</param>
    /// <returns>当前配置实例（支持链式调用）</returns>
    public ExtraDataLoaderConfig<TItem> Add(Func<DbContext, IEnumerable<TItem>, Task> loadAsync)
    {
        Loaders.Add(loadAsync);
        return this;
    }

    /// <summary>
    /// 添加数据加载器（不需要 DbContext）
    /// </summary>
    /// <param name="loadAsync">异步加载委托</param>
    /// <returns>当前配置实例（支持链式调用）</returns>
    public ExtraDataLoaderConfig<TItem> Add(Func<IEnumerable<TItem>, Task> loadAsync)
    {
        Loaders.Add((_, items) => loadAsync(items));
        return this;
    }

    /// <summary>
    /// 添加关联数据加载器 - 根据外键关系加载关联数据到实体的指定属性
    /// </summary>
    /// <typeparam name="TRelated">关联实体类型</typeparam>
    /// <param name="foreignKeyPropertyName">关联实体的外键属性名（如 "EntityId"）</param>
    /// <param name="primaryKeySelector">主实体的主键选择器</param>
    /// <param name="dataSetter">数据设置器</param>
    /// <param name="queryCustomizer">可选的查询自定义器（添加 Include、Where、OrderBy 等）</param>
    /// <returns>当前配置实例（支持链式调用）</returns>
    public ExtraDataLoaderConfig<TItem> AddRelatedData<TRelated>(
        string foreignKeyPropertyName,
        Func<TItem, Guid> primaryKeySelector,
        Action<TItem, ICollection<TRelated>> dataSetter,
        Func<IQueryable<TRelated>, IQueryable<TRelated>>? queryCustomizer = null)
        where TRelated : class
    {
        // 获取外键属性用于内存分组
        var foreignKeyProperty = typeof(TRelated).GetProperty(foreignKeyPropertyName)
            ?? throw new InvalidOperationException($"类型 {typeof(TRelated).Name} 没有属性 {foreignKeyPropertyName}");

        Func<TRelated, Guid> foreignKeySelector = related => (Guid)foreignKeyProperty.GetValue(related)!;

        Loaders.Add(async (dbContext, items) =>
        {
            var itemList = items.ToList();
            if (itemList.Count == 0) return;

            var primaryKeys = itemList.Select(primaryKeySelector).ToList();

            // 构建基础查询，使用 EF.Property 在数据库层面过滤
            IQueryable<TRelated> query = dbContext.Set<TRelated>()
                .AsNoTracking()
                .Where(r => primaryKeys.Contains(EF.Property<Guid>(r, foreignKeyPropertyName)));

            // 应用自定义查询（Include、Where、OrderBy 等）
            if (queryCustomizer != null)
            {
                query = queryCustomizer(query);
            }

            var relatedData = await query.ToListAsync();

            // 按外键分组
            var groupedData = relatedData
                .GroupBy(foreignKeySelector)
                .ToDictionary(g => g.Key, g => (ICollection<TRelated>)g.ToList());

            // 填充到主实体
            foreach (var item in itemList)
            {
                var key = primaryKeySelector(item);
                if (groupedData.TryGetValue(key, out var data))
                {
                    dataSetter(item, data);
                }
            }
        });

        return this;
    }

    /// <summary>
    /// 执行所有数据加载器
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="items">待加载数据的实体集合</param>
    public async Task LoadAllAsync(DbContext dbContext, IEnumerable<TItem> items)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0) return;

        foreach (var loader in Loaders)
        {
            try
            {
                await loader(dbContext, itemList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtraDataLoaderConfig] 加载器执行失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 是否有配置的加载器
    /// </summary>
    public bool HasLoaders => Loaders.Count > 0;
}
