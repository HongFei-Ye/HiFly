// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Openiddict.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.OpeniddictBbUI.IdentityManages.VirtualizeSelects;

public partial class UserVirtualizeSelect<TContext, TUser>
    where TContext : DbContext
    where TUser : class, IUser, new()
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

        var user = Context.Set<TUser>().FirstOrDefault(u => u.Id == Value);
        DefaultVirtualizeItemText = user?.UserName ?? "";
        if (string.IsNullOrEmpty(DisplayText))
        {
            DisplayText = "用户名称";
        }
    }

    public void Dispose()
    {
        Context.Dispose();
    }


    private async Task<QueryData<SelectedItem>> OnQueryAsync(VirtualizeQueryOption option)
    {
        using var _context = DbFactory.CreateDbContext();
        IQueryable<TUser>? items = _context.Set<TUser>();

        // 获取总数量（需要在分页之前计算）
        var totalCount = await items.CountAsync();

        if (!string.IsNullOrEmpty(option.SearchText))
        {
            items = items.Where(u => (u.UserName != null && u.UserName.Contains(option.SearchText)) || (u.Email != null && u.Email.Contains(option.SearchText)));
        }

        var selectedItems = await items
            .OrderBy(u => u.CreateTime)
            .Skip(option.StartIndex).Take(option.Count)
            .Select(u => new SelectedItem(u.Id, u.UserName ?? u.Email ?? ""))
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

