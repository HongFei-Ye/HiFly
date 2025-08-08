// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.Identity.Data.Interfaces;

public interface IUserRole
{
    string UserId { get; set; }

    string RoleId { get; set; }

    string? SuperiorUserId { get; set; }

    bool Enable { get; set; }

}
