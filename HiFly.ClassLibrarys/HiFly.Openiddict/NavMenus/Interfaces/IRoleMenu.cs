// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.NavMenus.Models;

namespace HiFly.Openiddict.NavMenus.Interfaces;
public interface IRoleMenu
{
    /// <summary>
    /// 识别码
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    NavMenuType NavMenuType { get; set; }

    /// <summary>
    /// 所属角色
    /// </summary>
    string? BelongRoleId { get; set; }

    /// <summary>
    /// 导航页面ID
    /// </summary>
    string MenuPageId { get; set; }

    /// <summary>
    /// 层级
    /// </summary>
    int Hierarchy { get; set; }

    /// <summary>
    /// 顺序
    /// </summary>
    int Sequence { get; set; }

    /// <summary>
    /// 父菜单名称
    /// </summary>
    string ParentId { get; set; }

}
