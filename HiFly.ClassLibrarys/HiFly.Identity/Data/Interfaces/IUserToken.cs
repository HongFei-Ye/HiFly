// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Identity.Data.Interfaces;

public interface IUserToken
{

    string UserId { get; set; }

    string LoginProvider { get; set; }

    string Name { get; set; }

    string? Value { get; set; }


    bool Enable { get; set; }
}
