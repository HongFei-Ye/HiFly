// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore;
using HiFly.Table.DataSources;
using HiFly.Table;

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.7 P1 — <see cref="ITItemDataSourceFactory"/> 的 EF Core 实现。
/// <para>
/// 由 <c>Platform.UI.Table.EfCore</c> 包通过 <see cref="EfCoreModuleInitializer"/> 自动设到
/// <see cref="TItemDataSourceRegistry.Default"/>，BC.Host 0 改动即可启用 EF 路径。
/// </para>
/// </summary>
public sealed class EfTItemDataSourceFactory : ITItemDataSourceFactory
{
    /// <summary>共享单例（factory 无状态，避免每次 OnInit 都 new）。</summary>
    public static EfTItemDataSourceFactory Instance { get; } = new();

    /// <inheritdoc />
    public ITItemDataSource<TItem>? TryCreate<TItem>(
        Type contextType,
        IServiceProvider serviceProvider,
        TItemTableConfiguration<TItem> configuration)
        where TItem : class, new()
    {
        // §7.7 P4 — 单泛型 TItemTable<TItem> 传 typeof(object)，从 DI hint 反推 DbContext 类型
        if (contextType == typeof(object))
        {
            var hint = serviceProvider.GetService(typeof(PrimaryDbContextHint)) as PrimaryDbContextHint;
            if (hint is null)
            {
                return null;
            }
            contextType = hint.ContextType;
        }

        if (!typeof(DbContext).IsAssignableFrom(contextType))
        {
            return null;
        }

        var factoryType = typeof(IDbContextFactory<>).MakeGenericType(contextType);
        var dbContextFactory = serviceProvider.GetService(factoryType);
        if (dbContextFactory is null)
        {
            return null;
        }

        var efOptions = new EfTItemDataSourceOptions<TItem>
        {
            IncludeNavigationConfig = configuration.IncludeNavigationConfig,
            PropertyFilterParameters = configuration.PropertyFilterParameters,
            IsTree = configuration.IsTree,
            ExtraDataLoader = configuration.ExtraDataLoader as ExtraDataLoaderConfig<TItem>,
            CacheSyncService = configuration.CacheSyncService,
        };

        var dataSourceType = typeof(EfTItemDataSource<,>).MakeGenericType(contextType, typeof(TItem));
        return (ITItemDataSource<TItem>)Activator.CreateInstance(dataSourceType, dbContextFactory, efOptions)!;
    }
}
