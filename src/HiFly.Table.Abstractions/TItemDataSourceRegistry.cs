// SPDX-License-Identifier: Apache-2.0

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P1 — TItemTable 的 ORM-fallback 工厂全局注册表。
/// <para>
/// 让 EfCore / FreeSQL 等具体 ORM 包通过 <c>[ModuleInitializer]</c> 自动注册自己（程序集加载时一次性触发），
/// BC.Host 0 改动即可启用对应路径。
/// </para>
/// <para>
/// TItemTable 的 OnInitializedAsync 优先级：
/// <list type="number">
///   <item>DI 注册的 <see cref="ITItemDataSource{TItem}"/>（独立 UI.Host: HttpTItemDataSource）</item>
///   <item>DI 注册的 <see cref="ITItemDataSourceFactory"/>（显式 BC.Host 启用某 ORM）</item>
///   <item><see cref="Default"/> —— ORM 包通过 ModuleInitializer 自动设置</item>
/// </list>
/// </para>
/// <para>
/// WASM 项目不引用任何 ORM 包时，<see cref="Default"/> 保持 <c>null</c>，TItemTable 强制走 HTTP 路径。
/// </para>
/// </summary>
public static class TItemDataSourceRegistry
{
    /// <summary>
    /// 默认 fallback 工厂。EfCore / FreeSQL 包通过 <c>[ModuleInitializer]</c> 自动注册；多个 ORM 包同时加载时
    /// 后注册的会覆盖前者（按 BC.Host 引用 ORM 顺序决定，单 ORM 场景不会有冲突）。
    /// </summary>
    public static ITItemDataSourceFactory? Default { get; set; }
}
