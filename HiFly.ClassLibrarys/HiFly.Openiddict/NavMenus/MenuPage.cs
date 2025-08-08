// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.NavMenus.Interfaces;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Openiddict.NavMenus;

public class MenuPage : IMenuPage
{
    [Key]
    [DisplayName("识别码")]
    public string Id { get; set; } = Guid.NewGuid().ToString();


    [DisplayName("标题")]
    public string Text { get; set; } = "";

    [DisplayName("图标")]
    public string Icon { get; set; } = "";

    [DisplayName("连接")]
    public string Url { get; set; } = "";


    [DisplayName("访问用户集合")]
    public List<string> RouterUserIds { get; set; } = [];

    [DisplayName("访问角色集合")]
    public List<string> RouterRoleIds { get; set; } = [];

    [DisplayName("访问角色等级")]
    public int RouterRoleHierarchy { get; set; }


    [DisplayName("增加用户集合")]
    public List<string> AddDataUserIds { get; set; } = [];

    [DisplayName("增加角色集合")]
    public List<string> AddDataRoleIds { get; set; } = [];

    [DisplayName("增加角色等级")]
    public int AddDataRoleHierarchy { get; set; }


    [DisplayName("编辑用户集合")]
    public List<string> EditDataUserIds { get; set; } = [];

    [DisplayName("编辑角色集合")]
    public List<string> EditDataRoleIds { get; set; } = [];

    [DisplayName("编辑角色等级")]
    public int EditDataRoleHierarchy { get; set; }


    [DisplayName("删除用户集合")]
    public List<string> DeleteDataUserIds { get; set; } = [];

    [DisplayName("删除角色集合")]
    public List<string> DeleteDataRoleIds { get; set; } = [];

    [DisplayName("删除角色等级")]
    public int DeleteDataRoleHierarchy { get; set; }


    [DisplayName("查询用户集合")]
    public List<string> QueryDataUserIds { get; set; } = [];

    [DisplayName("查询角色集合")]
    public List<string> QueryDataRoleIds { get; set; } = [];

    [DisplayName("查询角色等级")]
    public int QueryDataRoleaHierarchy { get; set; }


    [DisplayName("其他验证Json")]
    public string? OtherAuthorizeJson { get; set; }









}
