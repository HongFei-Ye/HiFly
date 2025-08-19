// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

namespace HiFly.Tables.Core.Models;

/// <summary>
/// 数据操作验证
/// </summary>
public class DataOperationVerification
{
    /// <summary>
    /// 是否可以添加
    /// </summary>
    public bool IsCanAdd { get; set; } = false;

    /// <summary>
    /// 是否可以编辑
    /// </summary>
    public bool IsCanEdit { get; set; } = false;

    /// <summary>
    /// 是否可以删除
    /// </summary>
    public bool IsCanDelete { get; set; } = false;

    /// <summary>
    /// 是否可以查询
    /// </summary>
    public bool IsCanQuery { get; set; } = false;
}
