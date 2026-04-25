// SPDX-License-Identifier: Apache-2.0

using FreeSql;

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P2 — <see cref="ITItemDataSourceFactory"/> 的 FreeSQL 实现。
/// <para>
/// 由 <c>Platform.UI.Table.FreeSql</c> 包通过 <see cref="FreeSqlModuleInitializer"/> 自动设到
/// <see cref="TItemDataSourceRegistry.Default"/>。FreeSQL 路径下 TContext 期望声明为
/// <see cref="IFreeSql"/>（或继承 IFreeSql 的接口/类），TryCreate 时检查 typeof(TContext)
/// 是否能匹配上 DI 注册的 IFreeSql 实例。
/// </para>
/// </summary>
public sealed class FreeSqlTItemDataSourceFactory : ITItemDataSourceFactory
{
    public static FreeSqlTItemDataSourceFactory Instance { get; } = new();

    /// <inheritdoc />
    public ITItemDataSource<TItem>? TryCreate<TItem>(
        Type contextType,
        IServiceProvider serviceProvider,
        TItemTableConfiguration<TItem> configuration)
        where TItem : class, new()
    {
        // FreeSQL 路径要求 TContext 是 IFreeSql 或其子接口/实现
        if (!typeof(IFreeSql).IsAssignableFrom(contextType))
        {
            return null;
        }

        var freeSql = serviceProvider.GetService(contextType) as IFreeSql
                      ?? serviceProvider.GetService(typeof(IFreeSql)) as IFreeSql;
        if (freeSql is null)
        {
            return null;
        }

        return new FreeSqlTItemDataSource<TItem>(freeSql);
    }
}
