// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.BbTables;
using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.OpeniddictBbUI.IdentityManages;

[CascadingTypeParameter(nameof(TItem))]
public partial class UserRoleManage<TContext, TItem, TRole, TUser>
        where TContext : DbContext
        where TItem : class, IUserRole, new()
        where TRole : class, IRole, new()
        where TUser : class, IUser, new()
{

    [NotNull]
    private TItemTable<TContext, TItem>? TableRef { get; set; }

    private List<TItem> SelectedRows { get; set; } = [];

    [Parameter]
    public bool IsDialog { get; set; } = false;

    [Parameter]
    public PropertyFilterParameters? PropertyFilterParameters { get; set; }


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // var Test1 = SelectedRows.Select(ur => ur.UserId);



    }

}

