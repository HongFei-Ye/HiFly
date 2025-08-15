// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;

namespace HiFly.Identity.Data;

public class UserToken : IdentityUserToken<Guid>, IUserToken
{
    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("用户ID")]
    public override Guid UserId { get; set; } = default!;

    [DisplayName("登录提供程序")]
    public override string LoginProvider { get; set; } = default!;

    [DisplayName("令牌名称")]
    public override string Name { get; set; } = default!;

    [DisplayName("令牌值")]
    public override string? Value { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;
}
