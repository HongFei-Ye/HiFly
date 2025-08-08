// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Openiddict.Identity.Data;

public class Role : IdentityRole, IRole
{
    [Key]
    [DisplayName("识别码")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("角色名称")]
    public override string? Name { get; set; }

    [DisplayName("标准角色名称")]
    public override string? NormalizedName { get; set; }

    [DisplayName("显示名称")]
    public string? ShowName { get; set; }

    [DisplayName("权限等级")]
    public int Hierarchy { get; set; }

    [DisplayName("上级角色ID")]
    public string? SuperiorRoleId { get; set; }

    [DisplayName("并发标识")]
    public override string? ConcurrencyStamp { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;

}
