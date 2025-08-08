// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.BbTables;
using HiFly.Openiddict.Identity.Data.Interfaces;
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


