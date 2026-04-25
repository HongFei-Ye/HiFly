// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P2 — Platform.UI.Table.FreeSql 程序集加载时自动注册 <see cref="FreeSqlTItemDataSourceFactory"/>
/// 到 <see cref="TItemDataSourceRegistry.Default"/>。
/// <para>
/// 与 EfCoreModuleInitializer 一样使用 <c>??=</c> 不覆盖已注册的 fallback —— BC.Host 引用顺序决定优先级。
/// 同时声明 <see cref="ITItemDataSourceFactory"/> 时建议消费侧 <c>services.AddSingleton</c> 显式覆盖。
/// </para>
/// </summary>
internal static class FreeSqlModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        TItemDataSourceRegistry.Default ??= FreeSqlTItemDataSourceFactory.Instance;
    }
}
