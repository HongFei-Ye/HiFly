# HiFly.Table.FreeSql

FreeSQL adapter for [HiFly.Table.Abstractions](../HiFly.Table.Abstractions). Mirrors the design of [HiFly.Table.EfCore](../HiFly.Table.EfCore) but consumes the FreeSQL `IFreeSql` singleton instead of EF Core's `IDbContextFactory<TContext>`.

```csharp
// Program.cs
services.AddSingleton<IFreeSql>(_ => new FreeSqlBuilder()
    .UseConnectionString(DataType.PostgreSQL, "Host=...;...")
    .Build());
// Done — FreeSqlModuleInitializer auto-registers FreeSqlTItemDataSourceFactory
```

```razor
@using HiFly.Table

<TItemTable TItem="MyEntity">
    <TableColumns>
        <TableColumn @bind-Field="@context.Name" Text="Name" />
    </TableColumns>
</TItemTable>
```

## Coverage

Currently implements: paged query (with text search across all string properties), Save (Add/Update), Delete.

Not yet covered (extend on demand): `PropertyFilterParameters` tree → FreeSQL `DynamicFilterInfo` translation, `IsTree`, complex includes, `IExtraDataLoaderConfig`. Pull requests welcome.

## License

Apache-2.0
