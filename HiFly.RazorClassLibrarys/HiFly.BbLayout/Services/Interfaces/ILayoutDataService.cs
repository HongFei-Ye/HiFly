// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;

namespace HiFly.BbLayout.Services.Interfaces;

/// <summary>
/// 布局数据服务接口，用于提供布局组件所需的各种配置数据
/// </summary>
public interface ILayoutDataService
{
    /// <summary>
    /// 用户头像路径
    /// </summary>
    string AvatarUrl { get; set; }

    /// <summary>
    /// 当前用户名称
    /// </summary>
    string LoginUserName { get; set; }

    /// <summary>
    /// 当前角色名称
    /// </summary>
    string NowRoleName { get; set; }

    /// <summary>
    /// 退出组件导航
    /// </summary>
    List<MenuItem> LogoutMenus { get; set; }

    /// <summary>
    /// 侧面Logo路径
    /// </summary>
    string SideLogoSrc { get; set; }

    /// <summary>
    /// 侧面标题
    /// </summary>
    string SideTitle { get; set; }

    /// <summary>
    /// 页脚链接
    /// </summary>
    string FooterHref { get; set; }

    /// <summary>
    /// 页脚标题
    /// </summary>
    string FooterText { get; set; }
}
