// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Openiddict.Structure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiFly.OpeniddictBbUI.StructureManages.TItemIdFilters;

public partial class UnitIdFilter<TContext, TUnit>
    where TContext : DbContext
    where TUnit : class, IUnit, new()
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
        SearchValue = "";

        StateHasChanged();
    }

    private string SearchValue { get; set; } = "";

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
