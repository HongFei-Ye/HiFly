// SPDX-License-Identifier: Apache-2.0

using BootstrapBlazor.Components;
using HiFly.Table;

namespace HiFly.Table.DataSources;

/// <summary>
/// <see cref="EfTItemDataSource{TContext, TItem}"/> 构造时传入的每页配置。
/// TItemTable 在 OnInitializedAsync 时把自己的参数填进来；所有可为 null 的字段表示未配置，实现内部按默认处理。
/// </summary>
public sealed class EfTItemDataSourceOptions<TItem>
    where TItem : class, new()
{
    /// <summary>导航属性 Include 配置（对应 TItemTable.IncludeNavigationConfig 参数）。</summary>
    public IncludeNavigationConfig? IncludeNavigationConfig { get; init; }

    /// <summary>页级过滤参数（对应 TItemTable.PropertyFilterParameters 参数）；与 QueryPageOptions.Searches 合并后进 AutoFilter。</summary>
    public PropertyFilterParameters? PropertyFilterParameters { get; init; }

    /// <summary>树形模式开关（对应 TItemTable.IsTree 参数）。true 时走 <see cref="ITItemDataSource{TItem}.QueryTreeAsync"/> 分支。</summary>
    public bool IsTree { get; init; }

    /// <summary>查询后的额外数据加载器（NotMapped 属性 / 多表分组）；co-located 模式下直接消费 DbContext 做 sub-query。</summary>
    public ExtraDataLoaderConfig<TItem>? ExtraDataLoader { get; init; }

    /// <summary>保存成功后做缓存失效的平台级服务；为空时跳过缓存同步。</summary>
    public ICacheSyncService? CacheSyncService { get; init; }
}
