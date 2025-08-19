// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Identity.Data;

public class RoleClaim : IdentityRoleClaim<Guid>, IRoleClaim
{
    [Key]
    [DisplayName("识别码")]
    public override int Id { get; set; } = default!;

    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("角色ID")]
    public override Guid RoleId { get; set; } = default!;

    [DisplayName("申明类型")]
    public override string? ClaimType { get; set; }

    [DisplayName("申明值")]
    public override string? ClaimValue { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;

}
