# HiFly.Table.Abstractions

Provider-agnostic `TItemTable<TItem>` for [BootstrapBlazor](https://www.blazor.zone/) — **0 ORM dependencies**, works in Blazor Server **and** Blazor WebAssembly with any data backend (HTTP / EF Core / FreeSQL / Dapper / Custom).

## What's included

- **`TItemTable<TItem>`** — single-arg Razor component inheriting `BootstrapBlazor.Components.Table<TItem>` directly; all 100+ BB Table parameters (ShowSearch / IsTree / EditMode / TableColumns / etc.) automatically inherited
- **`ITItemDataSource<TItem>`** — provider-agnostic data source interface (Query / Save / Delete)
- **`HttpTItemDataSource<TItem>`** — REST adapter, talks to `_ui-table/{TItem}/(query|save|delete)` endpoint
- **`ITItemDataSourceFactory`** + **`TItemDataSourceRegistry`** — pluggable factory abstraction; ORM packages (`HiFly.Table.EfCore`, `HiFly.Table.FreeSql`) auto-register via `[ModuleInitializer]`
- **`PrimaryDbContextHint`** — DI marker recording the BC's primary DbContext type
- **`PropertyFilterParameters`** / **`IncludeNavigationConfig`** / **`IExtraDataLoaderConfig<TItem>`** / **`FilterFieldType`** — config types used by data sources

## Usage

### Blazor Server / WASM via HTTP

```csharp
// Program.cs (UI host, no DbContext)
services.AddHttpClient<HttpTItemDataSource<MyItem>>(c =>
    c.BaseAddress = new Uri("https://api.example.com/api/v1/mybc/"));
services.AddScoped<ITItemDataSource<MyItem>>(sp =>
    sp.GetRequiredService<HttpTItemDataSource<MyItem>>());
```

```razor
@using HiFly.Table

<TItemTable TItem="MyItem">
    <TableColumns>
        <TableColumn @bind-Field="@context.Name" Text="Name" />
    </TableColumns>
</TItemTable>
```

### Co-located EF Core (use `HiFly.Table.EfCore` package)

```csharp
// Program.cs (BC.Host with DbContext)
services.AddDbContext<MyDbContext>(opt => opt.UseNpgsql(cs));
services.AddDbContextFactory<MyDbContext>(opt => opt.UseNpgsql(cs));
services.AddPlatformUiTableEfContext<MyDbContext>();  // hint registration
```

The `[ModuleInitializer]` in `HiFly.Table.EfCore` auto-registers `EfTItemDataSourceFactory` to `TItemDataSourceRegistry.Default`, so `<TItemTable TItem="MyItem">` resolves the right `IDbContextFactory<MyDbContext>` at runtime.

## License

Apache-2.0
