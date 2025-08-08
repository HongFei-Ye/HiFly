// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.NavMenus.Interfaces;
using HiFly.Openiddict.NavMenus.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Openiddict.NavMenus;

public class RoleMenu : IRoleMenu
{
    [Key]
    [DisplayName("识别码")]
    public string Id { get; set; } = Guid.NewGuid().ToString();


    [DisplayName("类型")]
    public NavMenuType NavMenuType { get; set; }

    [DisplayName("所属角色")]
    public string? BelongRoleId { get; set; }

    [DisplayName("导航页面")]
    public string MenuPageId { get; set; } = default!;

    [DisplayName("层级")]
    public int Hierarchy { get; set; }

    [DisplayName("顺序")]
    public int Sequence { get; set; }

    [DisplayName("父菜单名称")]
    public string ParentId { get; set; } = "顶级菜单";




}
