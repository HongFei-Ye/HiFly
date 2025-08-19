// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using System.ComponentModel;

namespace HiFly.Identity.Data.Interfaces;

public interface IUserLogin
{
    [DisplayName("用户ID")]
    Guid UserId { get; set; }

    [DisplayName("登录提供程序")]
    string LoginProvider { get; set; }

    [DisplayName("提供程序用户Key")]
    string ProviderKey { get; set; }

    [DisplayName("提供程序名称")]
    string? ProviderDisplayName { get; set; }

    [DisplayName("是否启用")]
    bool Enable { get; set; }
}
