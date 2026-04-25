// SPDX-License-Identifier: Apache-2.0

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P1 — 数据源 fallback 工厂抽象。
/// <para>
/// 当 DI 容器没有直接注册 <see cref="ITItemDataSource{TItem}"/> 时（比如独立 UI.Host 没注册 HttpTItemDataSource），
/// TItemTable 会从 DI / <see cref="TItemDataSourceRegistry.Default"/> 拿一个工厂，让具体 ORM 实现（EF / FreeSQL / Dapper）
/// 反射创建对应的 ITItemDataSource。
/// </para>
/// <para>
/// 这个抽象让 <c>Platform.UI.Table.Abstractions</c> 包不需要 ProjectReference 任何 ORM —— 抽象层零 EF / 零 FreeSQL，
/// WASM 项目可单独引用 Abstractions 不带任何 ORM 依赖。
/// </para>
/// </summary>
public interface ITItemDataSourceFactory
{
    /// <summary>
    /// 尝试根据 <paramref name="contextType"/>（一般是 DbContext / IFreeSql / DataSource 类型）创建数据源。
    /// 不适用时返回 <c>null</c>，TItemTable 会逐个询问已注册的工厂。
    /// </summary>
    ITItemDataSource<TItem>? TryCreate<TItem>(
        Type contextType,
        IServiceProvider serviceProvider,
        TItemTableConfiguration<TItem> configuration)
        where TItem : class, new();
}
