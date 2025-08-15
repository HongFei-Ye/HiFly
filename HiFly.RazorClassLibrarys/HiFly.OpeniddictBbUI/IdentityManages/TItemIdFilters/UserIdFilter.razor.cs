// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Identity.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiFly.OpeniddictBbUI.IdentityManages.TItemIdFilters;

public partial class UserIdFilter<TContext, TUser>
    where TContext : DbContext
    where TUser : class, IUser, new()
{
    /// <summary>
    /// OnInitialized 方法
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        //if (TableFilter != null)
        //{
        //    TableFilter.ShowMoreButton = false;
        //}
    }

    /// <summary>
    /// 重置过滤条件方法
    /// </summary>
    public override void Reset()
    {
        SearchValue = null;

        StateHasChanged();
    }

    private Guid? SearchValue { get; set; } 

    /// <summary>
    /// 生成过滤条件方法
    /// </summary>
    /// <returns></returns>
    public override FilterKeyValueAction GetFilterConditions()
    {
        var filter = new FilterKeyValueAction() { Filters = [] };
        filter.Filters.Add(new FilterKeyValueAction()
        {
            FieldKey = FieldKey,
            FieldValue = SearchValue,
            FilterAction = FilterAction.Contains,
        });
        return filter;
    }



}
