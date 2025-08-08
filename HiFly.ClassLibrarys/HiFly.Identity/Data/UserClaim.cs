// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Identity.Data;

public class UserClaim : IdentityUserClaim<string>, IUserClaim
{
    [Key]
    [DisplayName("识别码")]
    public override int Id { get; set; } = default!;

    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("用户ID")]
    public override string UserId { get; set; } = default!;

    [DisplayName("申明类型")]
    public override string? ClaimType { get; set; }

    [DisplayName("申明值")]
    public override string? ClaimValue { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;
}
