// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Tables.Core.Models;
using Microsoft.AspNetCore.Components;

namespace HiFly.Tables.Core.Interfaces;

/// <summary>
/// 表格组件接口
/// </summary>
/// <typeparam name="TItem">实体类型</typeparam>
public interface ITableComponent<TItem> where TItem : class, new()
{
    /// <summary>
    /// 显示/隐藏 Loading 遮罩
    /// </summary>
    /// <param name="visible">是否显示</param>
    /// <returns></returns>
    ValueTask ToggleLoading(bool visible);

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <returns></returns>
    Task QueryAsync();

    /// <summary>
    /// 获得/设置 被选中数据集合
    /// </summary>
    List<TItem> SelectedRows { get; set; }

    /// <summary>
    /// 清除被选中数据集合
    /// </summary>
    /// <returns>是否清除成功</returns>
    bool CleanSelectedRows();

    /// <summary>
    /// 修改被选中数据集合
    /// </summary>
    /// <param name="newSelectedRows">新的选中行集合</param>
    void SetSelectedRows(List<TItem>? newSelectedRows = null);

    /// <summary>
    /// 获得/设置 数据操作权限验证
    /// </summary>
    DataOperationVerification? DataOperationVerification { get; set; }

    /// <summary>
    /// 获得/设置 属性过滤参数
    /// </summary>
    PropertyFilterParameters? PropertyFilterParameters { get; set; }

    /// <summary>
    /// 获得/设置 表格列模板
    /// </summary>
    RenderFragment<TItem>? TableColumns { get; set; }
}
