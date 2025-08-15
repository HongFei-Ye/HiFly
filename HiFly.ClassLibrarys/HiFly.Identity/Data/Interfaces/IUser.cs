// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using System.ComponentModel;

namespace HiFly.Identity.Data.Interfaces;

public interface IUser
{
    [DisplayName("识别码")]
    Guid Id { get; set; }

    [DisplayName("创建时间(UTC)")]
    DateTime CreateTime { get; set; }

    [DisplayName("用户名称")]
    string? UserName { get; set; }

    [DisplayName("邮箱地址")]
    string? Email { get; set; }

    [DisplayName("邮箱地址绑定")]
    bool EmailConfirmed { get; set; }

    [DisplayName("登录首选角色")]
    string? LoginedRole { get; set; }

    [DisplayName("是否启用")]
    bool Enable { get; set; }

}
