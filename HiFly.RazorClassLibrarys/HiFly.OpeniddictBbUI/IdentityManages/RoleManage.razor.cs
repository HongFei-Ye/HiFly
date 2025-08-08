// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.BbTables;
using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace HiFly.OpeniddictBbUI.IdentityManages;

[CascadingTypeParameter(nameof(TRole))]
public partial class RoleManage<TContext, TRole> : ComponentBase
        where TContext : DbContext
        where TRole : class, IRole, new()
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
