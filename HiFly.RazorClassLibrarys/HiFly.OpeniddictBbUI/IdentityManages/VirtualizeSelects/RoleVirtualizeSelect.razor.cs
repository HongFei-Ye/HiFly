// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.OpeniddictBbUI.IdentityManages.VirtualizeSelects;

public partial class RoleVirtualizeSelect<TContext, TRole>
    where TContext : DbContext
    where TRole : class, IRole, new()
{
    [NotNull]
    private TContext? Context { get; set; }

    [Parameter]
    public Guid? Value { get; set; }

    [Parameter]
    public EventCallback<Guid?> ValueChanged { get; set; }

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

        var role = Context.Set<TRole>().FirstOrDefault(r => r.Id == Value);
        DefaultVirtualizeItemText = role?.ShowName ?? role?.Name ?? "";
        if (string.IsNullOrEmpty(DisplayText))
        {
            DisplayText = "角色名称";
        }
    }

    public void Dispose()
    {
        Context.Dispose();
    }


    private async Task<QueryData<SelectedItem>> OnQueryAsync(VirtualizeQueryOption option)
    {
        using var _context = DbFactory.CreateDbContext();
        IQueryable<TRole>? items = _context.Set<TRole>();

        // 获取总数量（需要在分页之前计算）
        var totalCount = await items.CountAsync();

        if (!string.IsNullOrEmpty(option.SearchText))
        {
            items = items.Where(r => (r.ShowName != null && r.ShowName.Contains(option.SearchText)) || (r.Name != null && r.Name.Contains(option.SearchText)));
        }

        var selectedItems = await items
            .OrderByDescending(r => r.Hierarchy)
            .Skip(option.StartIndex).Take(option.Count)
            .Select(r => new SelectedItem(r.Id.ToString(), r.ShowName ?? r.Name ?? ""))
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
        //if (IsCanNull == true && value.Value == "")
        //{
        //    await ValueChanged.InvokeAsync(null);
        //}
        //else
        //{
        //    await ValueChanged.InvokeAsync(value.Value);
        //}
    }


}

