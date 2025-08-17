// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

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
