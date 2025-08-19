// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;

namespace HiFly.Tables.Core.Models;

/// <summary>
/// 查询请求模型
/// </summary>
public class QueryRequest
{
    /// <summary>
    /// 查询选项
    /// </summary>
    public QueryPageOptions Options { get; set; } = new();

    /// <summary>
    /// 是否为树形表格
    /// </summary>
    public bool IsTree { get; set; } = false;

    /// <summary>
    /// 属性过滤参数
    /// </summary>
    public PropertyFilterParameters? FilterParameters { get; set; }

}
