// SPDX-License-Identifier: Apache-2.0

using BootstrapBlazor.Components;

namespace HiFly.Table.DataSources;

/// <summary>
/// TItemTable 的数据源抽象（v5.5 §7.6 TItemTable HTTP 模式）。
/// <para>
/// 把 <c>Platform.UI.Shared/TItemTable.razor.cs</c> 里原来 36 处直接 <c>DbContext.Set&lt;TItem&gt;()</c>
/// / <c>SaveChangesAsync</c> / <c>IQueryable</c> 的调用挪到本接口背后，让 TItemTable 可以在两种运行环境中
/// 复用：
/// </para>
/// <list type="bullet">
///   <item><b>co-located EF 模式</b>（BC.Host 内嵌 BC.UI）：<see cref="EfTItemDataSource{TContext, TItem}"/>
///     注入 <c>IDbContextFactory&lt;TContext&gt;</c>，保留原 LINQ / Include / 租户过滤 / 软删除 / 审计链路。</item>
///   <item><b>独立 UI.Host 模式</b>（C9 合规）：HttpTItemDataSource（P1 交付）走 <c>POST /api/v1/{bc}/_ui-table/{typeName}/*</c>
///     generic 端点，后端平台提供 <c>UiTableEndpoints.MapUiTableEndpoints&lt;TContext&gt;()</c>。</item>
/// </list>
/// <para>
/// 接口刻意用 BootstrapBlazor 原生 <see cref="QueryPageOptions"/> —— HTTP 实现会再把它序列化成 wire DTO，
/// EF 实现零转换直接消费，UI 调用方完全不感知底层哪种模式。
/// </para>
/// </summary>
public interface ITItemDataSource<TItem>
    where TItem : class, new()
{
    /// <summary>
    /// 分页 / 过滤 / 排序查询。HTTP 实现会在服务端评估，EF 实现在 co-located 进程内评估。
    /// 返回的 <see cref="QueryData{TItem}.Items"/> 对 HTTP 实现必须是已物化的列表（不能是 IQueryable）。
    /// </summary>
    Task<QueryData<TItem>> QueryAsync(QueryPageOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存 Add / Update。返回影响的记录数（EF 实现下即 SaveChangesAsync 的返回值）。
    /// 并发冲突 / 约束违反由具体实现 throw；上层 TItemTable 捕获后 SwalService 提示。
    /// </summary>
    Task<int> SaveAsync(TItem item, ItemChangedType changedType, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除。返回实际删除行数。传入的 items 可能只是 View 行（未 tracked），实现需按主键匹配查实体再删。
    /// </summary>
    Task<int> DeleteAsync(IEnumerable<TItem> items, CancellationToken cancellationToken = default);

    // 注：树模式（TItemTable.IsTree=true）由 QueryAsync / DeleteAsync 实现**内部分支**处理。
    // 当前 TItemTable 的树交互不在 HTTP wire 上传 parentId —— 根节点一次拿全，子节点的展开由 BootstrapBlazor
    // OnTreeExpand 回调单独处理；接口保持 3 方法即可覆盖现有 82 页行为。
}
