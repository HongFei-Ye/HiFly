# HiFly.Table.EfCore

EF Core adapter for [HiFly.Table.Abstractions](../HiFly.Table.Abstractions). Adds `EfTItemDataSourceFactory` to `TItemDataSourceRegistry.Default` automatically via `[ModuleInitializer]` — your BC.Host needs **0 changes** beyond:

```csharp
// 1. Register your DbContext + DbContextFactory (standard EF setup)
services.AddDbContext<MyDbContext>(opt => opt.UseNpgsql(cs));
services.AddDbContextFactory<MyDbContext>(opt => opt.UseNpgsql(cs));

// 2. Register the hint (one of two ways):

// Option A — explicit:
services.AddPlatformUiTableEfContext<MyDbContext>();

// Option B — if you use Platform.Infrastructure.AddPlatformDbProviderFromConfig<TContext>(),
//   the hint is registered automatically; nothing else to do.
```

```razor
@using HiFly.Table

<TItemTable TItem="MyEntity">
    <TableColumns>
        <TableColumn @bind-Field="@context.Name" Text="Name" />
        <TableColumn @bind-Field="@context.Status" Text="Status" />
    </TableColumns>
</TItemTable>
```

The `EfTItemDataSourceFactory.TryCreate` method:
1. Receives `typeof(object)` sentinel from `TItemTable<TItem>.OnInitializedAsync`
2. Reads the registered `PrimaryDbContextHint` to learn the BC's DbContext type
3. Reflects to obtain `IDbContextFactory<TContext>` from DI
4. Constructs `EfTItemDataSource<TContext, TItem>` with EF-specific options (Include / ExtraDataLoader / IsTree / ...)

## What's included

- `EfTItemDataSource<TContext, TItem>` — EF Core impl of `ITItemDataSource<TItem>` (LINQ + Include + tenant filter + soft-delete + audit chain preserved)
- `EfTItemDataSourceOptions<TItem>` — per-page config (IncludeNavigationConfig / PropertyFilterParameters / IsTree / ExtraDataLoader / CacheSync)
- `EfTItemDataSourceFactory` — `ITItemDataSourceFactory` impl, hint-aware
- `ExtraDataLoaderConfig<TItem>` — DbContext-aware extra data loader (NotMapped properties / multi-table sub-queries)
- `EfCoreServiceCollectionExtensions.AddPlatformUiTableEfContext<TContext>()` — DI hint registration
- `EfCoreModuleInitializer` — auto-registers `EfTItemDataSourceFactory` to `TItemDataSourceRegistry.Default`

## License

Apache-2.0
