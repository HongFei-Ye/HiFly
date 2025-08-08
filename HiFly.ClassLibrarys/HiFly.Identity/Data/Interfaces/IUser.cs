// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Identity.Data.Interfaces;

public interface IUser
{
    string Id { get; set; }

    DateTime CreateTime { get; set; }

    string? UserName { get; set; }

    string? Email { get; set; }

    bool EmailConfirmed { get; set; }


    string? QQNumber { get; set; }

    string? WeChatNumber { get; set; }


    string? LoginedRole { get; set; }


    bool Enable { get; set; }

}
