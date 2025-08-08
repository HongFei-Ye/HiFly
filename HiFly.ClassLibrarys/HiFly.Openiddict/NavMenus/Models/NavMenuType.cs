// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com


using System.ComponentModel;

namespace HiFly.Openiddict.NavMenus.Models;

public enum NavMenuType
{
    [Description("前台")]
    Reception,

    [Description("后台")]
    Backstage,


    [Description("角色前台")]
    RoleReception,

    [Description("角色后台")]
    RoleBackstage,

    [Description("用户前台")]
    UserReception,

    [Description("用户后台")]
    UserBackstage,


}
