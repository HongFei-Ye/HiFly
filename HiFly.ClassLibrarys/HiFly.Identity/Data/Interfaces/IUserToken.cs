// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using System.ComponentModel;

namespace HiFly.Identity.Data.Interfaces;

public interface IUserToken
{
    [DisplayName("用户ID")]
    Guid UserId { get; set; }

    [DisplayName("登录提供程序")]
    string LoginProvider { get; set; }

    [DisplayName("令牌名称")]
    string Name { get; set; }

    [DisplayName("令牌值")]
    string? Value { get; set; }

    [DisplayName("是否启用")]
    bool Enable { get; set; }
}
