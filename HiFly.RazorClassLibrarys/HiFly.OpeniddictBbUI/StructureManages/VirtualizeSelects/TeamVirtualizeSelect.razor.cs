// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Openiddict.Structure.Data.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.OpeniddictBbUI.StructureManages.VirtualizeSelects;

public partial class TeamVirtualizeSelect<TContext, TTeam>
    where TContext : DbContext
    where TTeam : class, ITeam, new()
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

        var team = Context.Set<TTeam>().FirstOrDefault(t => t.Id == Value);
        DefaultVirtualizeItemText = team?.ShortName ?? team?.FullName ?? "";
        if (string.IsNullOrEmpty(DisplayText))
        {
            DisplayText = "小组名称";
        }
    }

    public void Dispose()
    {
        Context.Dispose();
    }


    private async Task<QueryData<SelectedItem>> OnQueryAsync(VirtualizeQueryOption option)
    {
        using var _context = DbFactory.CreateDbContext();
        IQueryable<TTeam>? items = _context.Set<TTeam>();

        // 获取总数量（需要在分页之前计算）
        var totalCount = await items.CountAsync();

        if (!string.IsNullOrEmpty(option.SearchText))
        {
            items = items.Where(t => t.ShortName != null && t.ShortName.Contains(option.SearchText) || t.FullName != null && t.FullName.Contains(option.SearchText));
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

