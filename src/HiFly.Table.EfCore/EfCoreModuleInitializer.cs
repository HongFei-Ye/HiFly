// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P1 — Platform.UI.Table.EfCore 程序集加载时自动注册 <see cref="EfTItemDataSourceFactory"/>
/// 到 <see cref="TItemDataSourceRegistry.Default"/>。
/// <para>
/// BC.Host 只要 ProjectReference 本包（包括通过 Platform.UI.Shared 间接引用），
/// CLR 加载本程序集时即触发 <c>[ModuleInitializer]</c>，无需任何 <c>services.Add*</c> 调用。
/// </para>
/// <para>
/// WASM 项目不引用本包时，模块初始化器永不触发，<see cref="TItemDataSourceRegistry.Default"/> 保持
/// <c>null</c>，TItemTable 强制走 HTTP 路径——bundle 不含 EF Core。
/// </para>
/// </summary>
internal static class EfCoreModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // 多 ORM 包同时加载时遵守"先到先得"——后加载的不覆盖已注册的，让消费侧通过引用顺序控制偏好。
        // BC.Host 显式 services.AddSingleton<ITItemDataSourceFactory>(...) 优先级仍高于此 fallback。
        TItemDataSourceRegistry.Default ??= EfTItemDataSourceFactory.Instance;
    }
}
