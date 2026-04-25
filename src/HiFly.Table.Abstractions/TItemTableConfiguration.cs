// SPDX-License-Identifier: Apache-2.0

using HiFly.Table;
using HiFly.Table;

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P1 — TItemTable 通过 <see cref="ITItemDataSourceFactory"/> 传给具体 ORM 实现的配置载体。
/// <para>
/// 字段都在 Abstractions 包里，零 EF / FreeSQL 依赖；具体 ORM 实现（如 EfTItemDataSourceOptions）从这个 POCO
/// 抽取自己关心的字段并做相应映射。
/// </para>
/// </summary>
public sealed class TItemTableConfiguration<TItem>
    where TItem : class
{
    /// <summary>导航属性 Include 配置（仅 EF 路径生效）。</summary>
    public IncludeNavigationConfig? IncludeNavigationConfig { get; init; }

    /// <summary>页级过滤参数（与 QueryPageOptions.Searches 合并）。</summary>
    public PropertyFilterParameters? PropertyFilterParameters { get; init; }

    /// <summary>树形模式开关（对应 TItemTable.IsTree 参数）。</summary>
    public bool IsTree { get; init; }

    /// <summary>查询后的额外数据加载（NotMapped 属性 / 多表 sub-query）；ORM 实现可 cast 成自己具体类型。</summary>
    public IExtraDataLoaderConfig<TItem>? ExtraDataLoader { get; init; }

    /// <summary>保存成功后做缓存失效的平台级服务。</summary>
    public ICacheSyncService? CacheSyncService { get; init; }
}
