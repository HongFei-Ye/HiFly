// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using System.ComponentModel;

namespace HiFly.Identity.Data.Interfaces;

public interface IUserClaim
{
    [DisplayName("识别码")]
    int Id { get; set; }

    [DisplayName("用户ID")]
    Guid UserId { get; set; }

    [DisplayName("申明类型")]
    string? ClaimType { get; set; }

    [DisplayName("申明值")]
    string? ClaimValue { get; set; }

    [DisplayName("是否启用")]
    bool Enable { get; set; }

}
