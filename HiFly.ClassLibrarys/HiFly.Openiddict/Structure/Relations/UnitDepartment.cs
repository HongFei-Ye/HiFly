// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Structure.Relations.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Openiddict.Structure.Relations;

/// <summary>
/// 单位部门关系
/// </summary>
public class UnitDepartment : IUnitDepartment
{
    [Key]
    [DisplayName("识别码")]
    public virtual string Id { get; set; } = Guid.NewGuid().ToString();

    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("单位ID")]
    [Required(ErrorMessage = "请输入{0}")]
    public string UnitId { get; set; } = default!;

    [DisplayName("部门ID")]
    [Required(ErrorMessage = "请输入{0}")]
    public string DepartmentId { get; set; } = default!;

    [DisplayName("描述")]
    public string? Description { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;
}
