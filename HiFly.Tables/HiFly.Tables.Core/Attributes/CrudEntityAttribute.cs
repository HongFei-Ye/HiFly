// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Tables.Core.Attributes;

/// <summary>
/// 标记需要 CRUD 服务的实体特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class CrudEntityAttribute : Attribute
{
    /// <summary>
    /// 是否启用树形模式
    /// </summary>
    public bool EnableTreeMode { get; set; } = false;

    /// <summary>
    /// 实体描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 默认排序字段
    /// </summary>
    public string? DefaultSortField { get; set; }

    /// <summary>
    /// 是否启用软删除
    /// </summary>
    public bool EnableSoftDelete { get; set; } = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CrudEntityAttribute()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="description">实体描述</param>
    public CrudEntityAttribute(string description)
    {
        Description = description;
    }
}
