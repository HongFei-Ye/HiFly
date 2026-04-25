// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HiFly.Table.DataSources;

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P4 — Platform.UI.Table.EfCore DI 注册扩展。
/// </summary>
public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// 注册本 BC 主 DbContext 到 <see cref="EfTItemTableContextHint"/>，让单泛型
    /// <c>TItemTable&lt;TItem&gt;</c> 在 EF co-located 模式下能找到正确的 DbContext。
    /// <para>
    /// 用法（在 BC.Host Program.cs，<c>AddPlatformDbProviderFromConfig&lt;TContext&gt;</c> 后调用）：
    /// <code>
    /// builder.Services.AddPlatformDbProviderFromConfig&lt;IamDbContext&gt;(...);
    /// builder.Services.AddPlatformUiTableEfContext&lt;IamDbContext&gt;();
    /// </code>
    /// </para>
    /// <para>
    /// 独立 UI.Host（HTTP 模式）不需要调用此扩展 —— HttpTItemDataSource 直接接管。
    /// </para>
    /// </summary>
    public static IServiceCollection AddPlatformUiTableEfContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.TryAddSingleton(new PrimaryDbContextHint(typeof(TContext)));
        return services;
    }
}
