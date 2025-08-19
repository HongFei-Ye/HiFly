// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.BbTables;
using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace HiFly.OpeniddictBbUI.IdentityManages;

[CascadingTypeParameter(nameof(TUser))]
public partial class UserManage<TContext, TUser> : ComponentBase
        where TContext : DbContext
        where TUser : class, IUser, new()
{
    [Parameter]
    public PropertyFilterParameters? PropertyFilterParameters { get; set; }

    /// <summary>
    /// OnInitialized 方法
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

    }

    public void Dispose()
    {

    }






}


