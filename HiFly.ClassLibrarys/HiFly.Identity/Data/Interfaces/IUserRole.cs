// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using System.ComponentModel;

namespace HiFly.Identity.Data.Interfaces;

public interface IUserRole
{
    [DisplayName("识别码")]
    Guid Id { get; set; }

    [DisplayName("用户ID")]
    Guid UserId { get; set; }

    [DisplayName("角色ID")]
    Guid RoleId { get; set; }

    [DisplayName("上级用户ID")]
    Guid? SuperiorUserId { get; set; }

    [DisplayName("是否启用")]
    bool Enable { get; set; }

}
