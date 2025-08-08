// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Openiddict.Structure.Data.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.OpeniddictBbUI.StructureManages.VirtualizeSelects;

public partial class UnitVirtualizeSelect<TContext, TUnit>
    where TContext : DbContext
    where TUnit : class, IUnit, new()
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

        var unit = Context.Set<TUnit>().FirstOrDefault(u => u.Id == Value);
        DefaultVirtualizeItemText = unit?.ShortName ?? unit?.FullName ?? "";
        if (string.IsNullOrEmpty(DisplayText))
        {
            DisplayText = "单位名称";
        }
    }

    public void Dispose()
    {
        Context.Dispose();
    }


    private async Task<QueryData<SelectedItem>> OnQueryAsync(VirtualizeQueryOption option)
    {
        using var _context = DbFactory.CreateDbContext();
        IQueryable<TUnit>? items = _context.Set<TUnit>();

        // 获取总数量（需要在分页之前计算）
        var totalCount = await items.CountAsync();

        if (!string.IsNullOrEmpty(option.SearchText))
        {
            items = items.Where(u => u.ShortName != null && u.ShortName.Contains(option.SearchText) || u.FullName != null && u.FullName.Contains(option.SearchText));
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

