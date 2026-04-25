// SPDX-License-Identifier: Apache-2.0

using BootstrapBlazor.Components;
using FreeSql;
using FreeSql.Internal.Model;

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P2 — <see cref="ITItemDataSource{TItem}"/> 的 FreeSQL 实现。
/// <para>
/// 与 <c>EfTItemDataSource&lt;TContext, TItem&gt;</c> 对等：FreeSQL 路径下 <c>IFreeSql</c>
/// 单例承担"DbContextFactory"角色，每次 op 用同一 IFreeSql 跑一条独立 SQL（FreeSQL 内部
/// 自维护连接池）。
/// </para>
/// <para>
/// 当前覆盖：分页 query / 文本搜索 / 排序 / 单值 Add / Update / Delete。<b>未覆盖</b>：
/// EF 路径下 PropertyFilterParameters 树解析、ExtraDataLoaderConfig、IsTree、复杂 Include。
/// 这些等到首个真实 FreeSQL BC 落地后按需扩展（参考 EfTItemDataSource 实现）。
/// </para>
/// </summary>
public sealed class FreeSqlTItemDataSource<TItem> : ITItemDataSource<TItem>
    where TItem : class, new()
{
    private readonly IFreeSql _freeSql;

    public FreeSqlTItemDataSource(IFreeSql freeSql)
    {
        _freeSql = freeSql ?? throw new ArgumentNullException(nameof(freeSql));
    }

    public async Task<QueryData<TItem>> QueryAsync(QueryPageOptions options, CancellationToken cancellationToken = default)
    {
        // FreeSQL 默认 no-tracking，不需要显式调用
        var query = _freeSql.Select<TItem>();

        if (!string.IsNullOrWhiteSpace(options.SearchText))
        {
            // 文本搜索：FreeSQL 在 string 字段上做 Contains（生成 LIKE %x%）
            // 复杂多列 OR 链由 BC 自定义 ITItemDataSource 覆写
            query = query.WhereDynamicFilter(BuildSearchFilter(options.SearchText));
        }

        if (!string.IsNullOrWhiteSpace(options.SortName))
        {
            query = query.OrderByPropertyName(options.SortName, options.SortOrder != SortOrder.Desc);
        }

        var total = (int)await query.CountAsync(cancellationToken);

        var skip = (options.PageIndex - 1) * options.PageItems;
        var items = await query.Skip(skip).Take(options.PageItems).ToListAsync(cancellationToken);

        return new QueryData<TItem>
        {
            Items = items,
            TotalCount = total,
            IsSorted = !string.IsNullOrWhiteSpace(options.SortName),
            IsFiltered = !string.IsNullOrWhiteSpace(options.SearchText),
            IsSearch = !string.IsNullOrWhiteSpace(options.SearchText),
        };
    }

    public async Task<int> SaveAsync(TItem item, ItemChangedType changedType, CancellationToken cancellationToken = default)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

        return changedType switch
        {
            ItemChangedType.Add => (int)await _freeSql.Insert(item).ExecuteAffrowsAsync(cancellationToken),
            ItemChangedType.Update => (int)await _freeSql.Update<TItem>().SetSource(item).ExecuteAffrowsAsync(cancellationToken),
            _ => 0,
        };
    }

    public async Task<int> DeleteAsync(IEnumerable<TItem> items, CancellationToken cancellationToken = default)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));

        var list = items.ToList();
        if (list.Count == 0) return 0;

        var affected = 0;
        foreach (var item in list)
        {
            affected += (int)await _freeSql.Delete<TItem>().Where(item).ExecuteAffrowsAsync(cancellationToken);
        }
        return affected;
    }

    private static DynamicFilterInfo BuildSearchFilter(string searchText)
    {
        // 逐 string 属性 OR Contains —— 与 EfTItemDataSource 行为对齐
        var stringProps = typeof(TItem).GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead)
            .ToList();

        if (stringProps.Count == 0)
        {
            // 无 string 属性时返回一个永真过滤（让 query 不报错）
            return new DynamicFilterInfo { Logic = DynamicFilterLogic.Or, Filters = [] };
        }

        return new DynamicFilterInfo
        {
            Logic = DynamicFilterLogic.Or,
            Filters = stringProps.Select(p => new DynamicFilterInfo
            {
                Field = p.Name,
                Operator = DynamicFilterOperator.Contains,
                Value = searchText,
            }).ToList(),
        };
    }
}
