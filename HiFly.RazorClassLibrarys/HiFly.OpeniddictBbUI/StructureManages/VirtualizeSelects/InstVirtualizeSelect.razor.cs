// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Openiddict.Structure.Data.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.OpeniddictBbUI.StructureManages.VirtualizeSelects;

public partial class InstVirtualizeSelect<TContext, TInstitution>
    where TContext : DbContext
    where TInstitution : class, IInstitution, new()
{
    [NotNull]
    private TContext? Context { get; set; }

    [Parameter]
    public string Value { get; set; } = "";

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    private SelectedItem? VirtualItem { get; set; }

    private string DefaultVirtualizeItemText { get; set; } = "";

    [Parameter]
    public string? DisplayText { get; set; }

    [Parameter]
    public Color Color { get; set; } = Color.Secondary;

    [Parameter]
    public bool IsCanNull { get; set; }


    protected override void OnInitialized()
    {
        Context ??= DbFactory.CreateDbContext();
        base.OnInitialized();

        var inst = Context.Set<TInstitution>().FirstOrDefault(i => i.Id == Value);
        DefaultVirtualizeItemText = inst?.ShortName ?? inst?.FullName ?? "";
        if (string.IsNullOrEmpty(DisplayText))
        {
            DisplayText = "机构名称";
        }
    }

    public void Dispose()
    {
        Context.Dispose();
    }


    private async Task<QueryData<SelectedItem>> OnQueryAsync(VirtualizeQueryOption option)
    {
        using var _context = DbFactory.CreateDbContext();
        IQueryable<TInstitution>? items = _context.Set<TInstitution>();

        // 获取总数量（需要在分页之前计算）
        var totalCount = await items.CountAsync();

        if (!string.IsNullOrEmpty(option.SearchText))
        {
            items = items.Where(i => i.ShortName != null && i.ShortName.Contains(option.SearchText) || i.FullName != null && i.FullName.Contains(option.SearchText));
        }

        var selectedItems = await items
            .OrderBy(u => u.CreateTime)
            .Skip(option.StartIndex).Take(option.Count)
            .Select(u => new SelectedItem(u.Id, u.ShortName ?? u.FullName ?? ""))
            .ToListAsync();

        selectedItems?.Insert(0, new SelectedItem("", "请选择"));

        return new QueryData<SelectedItem>
        {
            Items = selectedItems,
            TotalCount = totalCount + 1
        };
    }

    private async Task OnSelectedItemChanged(SelectedItem value)
    {
        if (IsCanNull == true && value.Value == "")
        {
            await ValueChanged.InvokeAsync(null);
        }
        else
        {
            await ValueChanged.InvokeAsync(value.Value);
        }
    }


}

