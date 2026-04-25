// SPDX-License-Identifier: Apache-2.0

namespace HiFly.Table;

/// <summary>
/// v5.5 §7.7 P1 — 额外数据加载器抽象（marker interface）。
/// <para>
/// 让 TItemTable 的 <c>[Parameter] ExtraDataLoaderConfig</c> 在 Abstractions 包里只依赖一个空接口，
/// 具体的 EF 实现（<c>ExtraDataLoaderConfig&lt;TItem&gt;</c>）放在 Platform.UI.Table.EfCore，
/// 把 LINQ + IQueryable 的 EF 重依赖隔离开。
/// </para>
/// <para>
/// 消费侧 razor 0 改动 —— 旧 <c>new ExtraDataLoaderConfig&lt;X&gt;().Add(...)</c> 仍合法，因为
/// EfCore 包里的具体类实现了这个接口。
/// </para>
/// </summary>
public interface IExtraDataLoaderConfig<TItem>
    where TItem : class
{
}
