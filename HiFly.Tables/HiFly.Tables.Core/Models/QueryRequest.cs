// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
