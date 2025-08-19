// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Identity.Data;

public class Role : IdentityRole<Guid>, IRole
{
    [Key]
    [DisplayName("识别码")]
    public override Guid Id { get; set; } = Guid.NewGuid();

    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("角色名称")]
    public override string? Name
    {
        get => base.Name;
        set
        {
            base.Name = value;
            NormalizedName = value?.ToUpperInvariant();
        }
    }

    [DisplayName("标准角色名称")]
    public override string? NormalizedName { get; set; }

    [DisplayName("显示名称")]
    public string? ShowName { get; set; }

    [DisplayName("权限等级")]
    public int Hierarchy { get; set; }

    [DisplayName("并发标识")]
    public override string? ConcurrencyStamp { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;

}
