// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Core.Models;

namespace HiFly.Tables.Core.Interfaces;

/// <summary>
/// HiFly Tables 数据服务接口 - 不依赖任何特定ORM
/// </summary>
/// <typeparam name="TItem">实体类型</typeparam>
public interface IHiFlyDataService<TItem> : IDisposable where TItem : class, new()
{
    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="options">查询选项</param>
    /// <param name="propertyFilterParameters">属性过滤参数</param>
    /// <param name="isTree">是否为树形表格</param>
    /// <returns>查询数据</returns>
    Task<QueryData<TItem>> OnQueryAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters = null,
        bool isTree = false);

    /// <summary>
    /// 保存数据
    /// </summary>
    /// <param name="item">要保存的数据项</param>
    /// <param name="changedType">变更类型</param>
    /// <returns>保存是否成功</returns>
    Task<bool> OnSaveAsync(TItem item, ItemChangedType changedType);

    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="items">要删除的数据项集合</param>
    /// <returns>删除是否成功</returns>
    Task<bool> OnDeleteAsync(IEnumerable<TItem> items);
}
