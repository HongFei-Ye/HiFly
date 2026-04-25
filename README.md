# HiFly.Table

Provider-agnostic `TItemTable<TItem>` for [BootstrapBlazor](https://www.blazor.zone/) — a pluggable Blazor table component that works in **Blazor Server** *and* **Blazor WebAssembly**, with **any data backend** (HTTP / EF Core / FreeSQL / Dapper / Custom).

## Packages

| Package | Description | NuGet |
|---|---|---|
| **HiFly.Table.Abstractions** | Provider-agnostic abstractions; **0 ORM dependencies**; Server + WASM compatible | `0.1.0-preview` |
| **HiFly.Table.EfCore** | EF Core adapter; auto-registers via `[ModuleInitializer]` | `0.1.0-preview` |
| **HiFly.Table.FreeSql** | FreeSQL adapter; auto-registers via `[ModuleInitializer]` | `0.1.0-preview` |

## Quick start

### Blazor Server / WASM via HTTP

```csharp
// Program.cs (independent UI host, no DbContext)
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

### Co-located EF Core

```csharp
// Program.cs (BC.Host with DbContext)
services.AddDbContext<MyDbContext>(opt => opt.UseNpgsql(cs));
services.AddDbContextFactory<MyDbContext>(opt => opt.UseNpgsql(cs));
services.AddPlatformUiTableEfContext<MyDbContext>();  // hint registration
```

The `[ModuleInitializer]` in `HiFly.Table.EfCore` auto-registers `EfTItemDataSourceFactory` to `TItemDataSourceRegistry.Default`, so `<TItemTable TItem="MyItem">` resolves the right `IDbContextFactory<MyDbContext>` at runtime.

### Co-located FreeSQL

```csharp
services.AddSingleton<IFreeSql>(_ => new FreeSqlBuilder()
    .UseConnectionString(DataType.PostgreSQL, "Host=...;...")
    .Build());
// HiFly.Table.FreeSql ModuleInitializer auto-registers FreeSqlTItemDataSourceFactory
```

## Architecture

```
TItemTable<TItem>.OnInitializedAsync
   ↓
1) DI ITItemDataSource<TItem>  (HTTP first; user-registered)
   ↓ if not present
2) DI ITItemDataSourceFactory  (explicit BC.Host registration)
   ↓ if not present
3) TItemDataSourceRegistry.Default  (ORM packages auto-set via [ModuleInitializer])
   ↓ if not present
   throw InvalidOperationException
```

ORM-specific factories receive `typeof(object)` from `TItemTable<TItem>` and resolve the actual `DbContext` type via `PrimaryDbContextHint` registered in DI by the BC.Host's primary `AddDbContext`/`AddPlatformDbProviderFromConfig` call.

## Solution layout

- `src/HiFly.Table.Abstractions/` — Razor library, no ORM deps
- `src/HiFly.Table.EfCore/` — EF Core adapter
- `src/HiFly.Table.FreeSql/` — FreeSQL adapter
- `src/Sandbox/Sandbox.UI.WasmHost/` — Blazor WebAssembly demo proving Abstractions has zero EF leakage in WASM bundle
- `HiFly.Table.slnx` — standalone solution

Build: `dotnet build HiFly.Table.slnx`
Pack:  `dotnet pack HiFly.Table.slnx -c Release -o out/`

## License

[Apache-2.0](LICENSE.txt)
