// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Identity.Data;

public class User : IdentityUser<Guid>, IUser
{
    [Key]
    [DisplayName("识别码")]
    [PersonalData]
    public override Guid Id { get; set; } = Guid.NewGuid();

    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("用户名称")]
    [ProtectedPersonalData]
    public override string? UserName
    {
        get => base.UserName;
        set
        {
            base.UserName = value;
            NormalizedUserName = value?.ToUpperInvariant();
        }
    }

    [DisplayName("标准用户名称")]
    public override string? NormalizedUserName { get; set; }

    [DisplayName("密码哈希")]
    public override string? PasswordHash { get; set; }

    [DisplayName("邮箱地址")]
    [ProtectedPersonalData]
    public override string? Email
    {
        get => base.Email;
        set
        {
            base.Email = value;
            NormalizedEmail = value?.ToUpperInvariant();
        }
    }

    [DisplayName("标准邮箱地址")]
    public override string? NormalizedEmail { get; set; }

    [DisplayName("邮箱地址绑定")]
    [PersonalData]
    public override bool EmailConfirmed { get; set; }

    [DisplayName("手机号码")]
    [ProtectedPersonalData]
    public override string? PhoneNumber { get; set; }

    [DisplayName("手机号码绑定")]
    [PersonalData]
    public override bool PhoneNumberConfirmed { get; set; }


    [DisplayName("登录首选角色")]
    public string? LoginedRole { get; set; }


    [DisplayName("安全标识")]
    public override string? SecurityStamp { get; set; }

    [DisplayName("并发标识")]
    public override string? ConcurrencyStamp { get; set; }

    [DisplayName("双因素身份验证")]
    [PersonalData]
    public override bool TwoFactorEnabled { get; set; }

    [DisplayName("锁定时间")]
    public override DateTimeOffset? LockoutEnd { get; set; }

    [DisplayName("锁定启用")]
    public override bool LockoutEnabled { get; set; }

    [DisplayName("访问失败次数")]
    public override int AccessFailedCount { get; set; }


    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;


}
