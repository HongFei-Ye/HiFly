// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Identity.Data;

public class UserRole : IdentityUserRole<Guid>, IUserRole
{
    [Key]
    [DisplayName("识别码")]
    public virtual Guid Id { get; set; } = Guid.NewGuid();

    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("用户ID")]
    public override Guid UserId { get; set; } = default!;

    [DisplayName("角色ID")]
    public override Guid RoleId { get; set; } = default!;

    [DisplayName("上级用户ID")]
    public Guid? SuperiorUserId { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;

}
