// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

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
