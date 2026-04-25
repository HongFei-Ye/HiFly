// SPDX-License-Identifier: Apache-2.0

using BootstrapBlazor.Components;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace HiFly.Table.DataSources;

/// <summary>
/// v5.5 §7.6 P1 — <see cref="ITItemDataSource{TItem}"/> 的 HTTP 实现：把 TItemTable 的 Query/Save/Delete
/// 翻译成 <c>POST {bcBaseUri}/_ui-table/{TItem.Name}/(query|save|delete)</c> 请求。
/// 服务端实现见 <c>Platform.Infrastructure.UiTableEndpoints.UiTableEndpointExtensions.MapUiTableEndpoints&lt;TContext&gt;</c>。
/// <para>
/// 用法：
/// <code>
/// // {BC}.UI.Host Program.cs
/// services.AddHttpClient&lt;HttpTItemDataSource&lt;Tenant&gt;&gt;(c => c.BaseAddress = new Uri("http://gateway/api/v1/iam/"));
/// services.AddScoped&lt;ITItemDataSource&lt;Tenant&gt;&gt;(sp => sp.GetRequiredService&lt;HttpTItemDataSource&lt;Tenant&gt;&gt;());
/// </code>
/// </para>
/// <para>
/// **当前限制**（P1 最小可用，P4 polish）：BootstrapBlazor 的 PropertyFilterParameters / 多列搜索串 还
/// 没序列化到 wire；只把 <c>QueryPageOptions.Searches</c> 的纯文本拼成 <c>SearchText</c> 服务端做 OR Contains。
/// 排序 / 分页 / 总数 全保留。
/// </para>
/// </summary>
public sealed class HttpTItemDataSource<TItem> : ITItemDataSource<TItem>
    where TItem : class, new()
{
    private readonly HttpClient _httpClient;
    private readonly string _typeName;

    public HttpTItemDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _typeName = typeof(TItem).Name;
    }

    public async Task<QueryData<TItem>> QueryAsync(QueryPageOptions options, CancellationToken cancellationToken = default)
    {
        // v5.5 §7.6 P5 — options.ToFilter() 把 Searches/AdvanceSearches/CustomerSearches/Filters 全合
        // 进一棵 FilterKeyValueAction 树。我们把这棵树映射成 wire FilterClauseDto，让服务端反向构造
        // PropertyFilterParameters → AutoFilter，复杂多列过滤完整保真。
        var filterTree = SerializeFilter(options.ToFilter());

        var req = new
        {
            PageIndex = options.PageIndex,
            PageItems = options.PageItems,
            SortName = options.SortName,
            SortOrder = (int)options.SortOrder,
            SearchText = (string?)null,
            FilterTree = filterTree,
            IsTree = false, // HTTP 模式当前不开树（HttpTItemDataSource 由 TItemTable 在 IsTree=false 时优先走 HTTP；树模式仍走 EF）
        };

        var response = await _httpClient.PostAsJsonAsync($"_ui-table/{_typeName}/query", req, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<UiTableQueryResultDto<TItem>>(cancellationToken: cancellationToken)
            ?? new UiTableQueryResultDto<TItem>();

        return new QueryData<TItem>
        {
            Items = payload.Items,
            TotalCount = payload.TotalCount,
            IsSorted = options.SortOrder != SortOrder.Unset,
            IsFiltered = filterTree is not null,
            IsAdvanceSearch = options.AdvanceSearches.Count != 0,
            IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0,
        };
    }

    /// <summary>
    /// 把 BootstrapBlazor 的 <c>FilterKeyValueAction</c> 递归树序列化为 wire <c>FilterClauseDto</c>。
    /// 对齐 BootstrapBlazor 的 ToPropertyFilterParameters 路径 —— 服务端反向后调 AutoFilter 行为等价。
    /// </summary>
    private static FilterClauseDtoWire? SerializeFilter(FilterKeyValueAction? node)
    {
        if (node is null) return null;
        // 叶节点（FieldKey 非空）+ 复合节点（FieldKey 空，看 Filters）都映射；BootstrapBlazor 允许混合树。
        var hasField = !string.IsNullOrEmpty(node.FieldKey);
        var hasChildren = node.Filters is { Count: > 0 };
        if (!hasField && !hasChildren) return null;

        var dto = new FilterClauseDtoWire
        {
            FieldKey = node.FieldKey,
            FieldValue = node.FieldValue,
            FilterAction = (int)node.FilterAction,
            FilterLogic = (int)node.FilterLogic,
        };
        if (hasChildren)
        {
            foreach (var child in node.Filters!)
            {
                var childDto = SerializeFilter(child);
                if (childDto is not null) dto.Filters.Add(childDto);
            }
        }
        return dto;
    }

    /// <summary>本地 wire DTO；服务端 Platform.Infrastructure.UiTableEndpoints.FilterClauseDto 形状一致。</summary>
    private sealed class FilterClauseDtoWire
    {
        public string? FieldKey { get; set; }
        public object? FieldValue { get; set; }
        public int FilterAction { get; set; }
        public int FilterLogic { get; set; }
        public List<FilterClauseDtoWire> Filters { get; set; } = [];
    }

    public async Task<int> SaveAsync(TItem item, ItemChangedType changedType, CancellationToken cancellationToken = default)
    {
        var req = new { Item = item, ChangedType = (int)changedType };
        var response = await _httpClient.PostAsJsonAsync($"_ui-table/{_typeName}/save", req, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.TryGetProperty("affected", out var a) ? a.GetInt32() : 0;
    }

    public async Task<int> DeleteAsync(IEnumerable<TItem> items, CancellationToken cancellationToken = default)
    {
        // 反射拿主键属性序列 —— 与 BC.Host 端通过 EF Model 拿主键序列一致；这里依赖 entity 上有
        // [Key] 标注或者主键属性叫 "Id"（与 EF 默认约定一致）。
        var keys = items.Select(ExtractKeyValues).ToList();
        if (keys.Count == 0) return 0;

        var req = new { Keys = keys };
        var response = await _httpClient.PostAsJsonAsync($"_ui-table/{_typeName}/delete", req, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return result.TryGetProperty("affected", out var a) ? a.GetInt32() : 0;
    }

    private static string? ExtractSearchText(QueryPageOptions options)
    {
        // BootstrapBlazor 的搜索/过滤模型用 IFilterAction 树（GetFilterConditions()）；P1 暂不打通到 wire。
        // 当前 wire 端的 SearchText 字段空着，让前端的页面级 SearchText 后续 P4 走专用路径填上。
        // TODO(§7.6 P4): 把 options.SearchModel / GetFilterConditions() 序列化成 wire 友好结构。
        return null;
    }

    private static List<object?> ExtractKeyValues(TItem item)
    {
        // 优先找 [Key] 标注
        var keyProps = typeof(TItem).GetProperties()
            .Where(p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null)
            .ToList();

        // 没有 [Key] 时按 EF 约定：属性名 Id 或 {ClassName}Id
        if (keyProps.Count == 0)
        {
            var byName = typeof(TItem).GetProperty("Id")
                ?? typeof(TItem).GetProperty($"{typeof(TItem).Name}Id");
            if (byName is not null) keyProps.Add(byName);
        }

        return keyProps.Select(p => p.GetValue(item)).ToList();
    }

    /// <summary>
    /// 与 <c>Platform.Infrastructure.UiTableEndpoints.UiTableQueryResult&lt;TItem&gt;</c> 的 wire 形状一致；
    /// 复制定义到此处避免 Platform.UI.Shared → Platform.Infrastructure 的 ProjectReference（后者已引前者会循环）。
    /// </summary>
    private sealed class UiTableQueryResultDto<T>
    {
        public List<T> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }
}
