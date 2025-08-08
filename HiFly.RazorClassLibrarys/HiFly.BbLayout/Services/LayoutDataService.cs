// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.BbLayout.Services.Interfaces;

namespace HiFly.BbLayout.Services;

public class LayoutDataService : ILayoutDataService
{
    /// <summary>
    /// 头像路径
    /// </summary>
    public string AvatarUrl { get; set; } = "./images/avatars/noportrait.png";

    /// <summary>
    /// 当前用户名称
    /// </summary>
    public string LoginUserName { get; set; } = "当前用户名称";

    /// <summary>
    /// 当前角色名称
    /// </summary>
    public string NowRoleName { get; set; } = "当前角色名称";

    /// <summary>
    /// 退出组件导航
    /// </summary>
    public List<MenuItem> LogoutMenus { get; set; } =
    [
        new(){ Text="个人资料", Url="/Account/Manage", Icon="fas fa-address-card" },
        new(){ Text="安全中心", Url="/Backstage/UserInfo", Icon="fa-solid fa-shield" },
        new(){ Text="注销登录", Url="/Account/UserLogout", Icon="fas fa-power-off" },
    ];

    /// <summary>
    /// 侧面Logo路径
    /// </summary>
    public string SideLogoSrc { get; set; } = "./favicon.png";

    /// <summary>
    /// 侧面标题
    /// </summary>
    public string SideTitle { get; set; } = "帮联数智方案";

    /// <summary>
    /// 页脚链接
    /// </summary>
    public string FooterHref { get; set; } = "/";

    /// <summary>
    /// 页脚标题
    /// </summary>
    public string FooterText { get; set; } = "Copyright © 2025-帮联数智工厂解决方案";





}
