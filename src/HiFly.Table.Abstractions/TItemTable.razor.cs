// SPDX-License-Identifier: Apache-2.0

using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using HiFly.Table.DataSources;
using HiFly.Table;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.Table;

/// <summary>
/// 平台 v5.5 §7.6 P6 + §7.7 P0/P1/P4 — <c>Table&lt;TItem&gt;</c> 直接继承的单泛型 TItemTable。
/// <para>
/// §7.7 P4 删掉了 TContext 类型参数，TItemTable 现在只需 <c>&lt;TItemTable TItem="X"&gt;</c>
/// 一个泛型形参。EF / FreeSQL co-located 模式通过 <see cref="EfTItemTableContextHint"/>
/// 由 BC.Host 注册的 DI hint 反推真实 DbContext。
/// </para>
/// <para>
/// 数据源优先级：
/// <list type="number">
///   <item>DI 注册的 <see cref="ITItemDataSource{TItem}"/>（独立 UI.Host: HttpTItemDataSource）</item>
///   <item>DI 注册的 <see cref="ITItemDataSourceFactory"/>（显式 BC.Host 启用某 ORM）</item>
///   <item><see cref="TItemDataSourceRegistry.Default"/>（ORM 包通过 ModuleInitializer 自动注册；
///     工厂从 <see cref="EfTItemTableContextHint"/> 拿 DbContext 类型）</item>
/// </list>
/// </para>
/// </summary>
[CascadingTypeParameter(nameof(TItem))]
public partial class TItemTable<TItem> : Table<TItem>
    where TItem : class, new()
{
    [Inject] public ICacheSyncService? CacheSyncService { get; set; }

    [Inject]
    [NotNull]
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>导航 Include 配置（仅 EF 模式生效；HTTP / FreeSQL 模式由服务端 endpoint 决定）。</summary>
    [Parameter]
    public IncludeNavigationConfig? IncludeNavigationConfig { get; set; }

    /// <summary>页查询后的额外数据加载（NotMapped 属性 / 多表 sub-query）；仅 EF 模式。</summary>
    [Parameter]
    public IExtraDataLoaderConfig<TItem>? ExtraDataLoaderConfig { get; set; }

    /// <summary>页级过滤参数（与 QueryPageOptions.Searches 合并）。</summary>
    [Parameter]
    public PropertyFilterParameters? PropertyFilterParameters { get; set; }

    /// <summary>
    /// 弹窗 / 详情页模式 —— 把 PageItemsSource 默认值改成 [10, 15, 20, 30, 40, 50, 100, 300, 500]（对话框空间小）。
    /// 消费侧若显式设了 PageItemsSource，本 flag 不再覆盖。
    /// </summary>
    [Parameter]
    public bool IsDialogOrDetail { get; set; }

    private ITItemDataSource<TItem>? _dataSource;

    protected override async Task OnInitializedAsync()
    {
        var diSource = ServiceProvider.GetService<ITItemDataSource<TItem>>();

        if (diSource is not null)
        {
            _dataSource = diSource;
        }
        else
        {
            var factory = ServiceProvider.GetService<ITItemDataSourceFactory>()
                          ?? TItemDataSourceRegistry.Default;

            if (factory is not null)
            {
                var configuration = new TItemTableConfiguration<TItem>
                {
                    IncludeNavigationConfig = IncludeNavigationConfig,
                    PropertyFilterParameters = PropertyFilterParameters,
                    IsTree = IsTree,
                    ExtraDataLoader = ExtraDataLoaderConfig,
                    CacheSyncService = CacheSyncService,
                };
                // §7.7 P4 — typeof(object) 作为 sentinel 让 factory 走 EfTItemTableContextHint DI 解析路径
                _dataSource = factory.TryCreate<TItem>(typeof(object), ServiceProvider, configuration);
            }

            if (_dataSource is null)
            {
                throw new InvalidOperationException(
                    $"TItemTable<{typeof(TItem).Name}> 找不到数据源："
                    + $"DI 既未注册 ITItemDataSource<{typeof(TItem).Name}>，"
                    + $"也未通过 ITItemDataSourceFactory + EfTItemTableContextHint 提供 EF/FreeSQL 路径。"
                    + "\n· 独立 UI.Host：注册 HttpTItemDataSource<TItem> as ITItemDataSource<TItem>"
                    + "\n· co-located EF：BC.Host 调用 services.AddPlatformUiTableEfContext<MyDbContext>()"
                    + "\n· FreeSQL BC.Host：引用 Platform.UI.Table.FreeSql 即自动启用");
            }
        }

        // 设默认回调（消费侧未传时启用兜底）。Table 的 InvokeInitAsync 会读这些回调发首次查询，
        // 必须在 base.OnInitializedAsync 之前。
        OnQueryAsync ??= DefaultOnQueryAsync;
        OnSaveAsync ??= DefaultOnSaveAsync;
        OnDeleteAsync ??= DefaultOnDeleteAsync;

        if (IsDialogOrDetail && (PageItemsSource is null || ReferenceEquals(PageItemsSource, Array.Empty<int>())))
        {
            PageItemsSource = [10, 15, 20, 30, 40, 50, 100, 300, 500];
        }

        await base.OnInitializedAsync();
    }

    private Task<QueryData<TItem>> DefaultOnQueryAsync(QueryPageOptions options)
        => _dataSource!.QueryAsync(options);

    private async Task<bool> DefaultOnSaveAsync(TItem item, ItemChangedType changedType)
    {
        try
        {
            var affected = await _dataSource!.SaveAsync(item, changedType);
            var ok = affected > 0;
            if (ok && CacheSyncService is not null)
            {
                await CacheSyncService.InvalidateCacheAsync(changedType, item);
            }
            return ok;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("数据已存在") || ex.Message.Contains("禁止再次加入"))
        {
            // EfTItemDataSource 对 Add 重复抛 InvalidOperationException —— 对齐原 UX
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> DefaultOnDeleteAsync(IEnumerable<TItem> items)
    {
        try
        {
            var affected = await _dataSource!.DeleteAsync(items);
            var ok = affected > 0;
            if (ok && CacheSyncService is not null)
            {
                await CacheSyncService.InvalidateEntityCacheAsync<TItem>();
            }
            return ok;
        }
        catch
        {
            return false;
        }
    }
}
