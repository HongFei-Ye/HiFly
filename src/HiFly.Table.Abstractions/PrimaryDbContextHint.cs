// SPDX-License-Identifier: Apache-2.0

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P4 — BC.Host 主 DbContext 类型的 DI 提示。
/// <para>
/// 用法：跨层组件（如 <c>Platform.UI.Table.EfCore.EfTItemDataSourceFactory</c>）需要在不知道
/// TContext 类型参数时获取 BC 的主 DbContext —— 通过 <c>sp.GetService&lt;PrimaryDbContextHint&gt;()</c>
/// 拿到。
/// </para>
/// <para>
/// 自动注册路径：<c>Platform.Infrastructure.PlatformDbProviderExtensions.AddPlatformDbProviderFromConfig&lt;TDbContext&gt;()</c>
/// 内部调 <c>services.TryAddSingleton(new PrimaryDbContextHint(typeof(TDbContext)))</c>。
/// 显式注册：<c>services.AddPlatformUiTableEfContext&lt;MyDbContext&gt;()</c>（在 Platform.UI.Table.EfCore）。
/// </para>
/// <para>
/// 多 DbContext BC 应只注册主 DbContext（一般是 BC 的核心业务库）。其他 DbContext 通过
/// 显式 TContext 类型参数（消费侧 razor / 直接构造 EfTItemDataSource）路径访问。
/// </para>
/// </summary>
/// <param name="ContextType">该 BC 的主 DbContext 类型（如 <c>typeof(IamDbContext)</c>）。</param>
public sealed record PrimaryDbContextHint(Type ContextType);
