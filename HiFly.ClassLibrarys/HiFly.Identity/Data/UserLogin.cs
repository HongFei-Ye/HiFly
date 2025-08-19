// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;

namespace HiFly.Identity.Data;

public class UserLogin : IdentityUserLogin<Guid>, IUserLogin
{
    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("用户ID")]
    public override Guid UserId { get; set; } = default!;

    [DisplayName("登录提供程序")]
    public override string LoginProvider { get; set; } = default!;

    [DisplayName("提供程序用户Key")]
    public override string ProviderKey { get; set; } = default!;


    [DisplayName("提供程序名称")]
    public override string? ProviderDisplayName { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;

}
