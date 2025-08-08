// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.NavMenus.Models;

namespace HiFly.Openiddict.NavMenus.Services.Interfaces;

public interface IRoleMenuService
{

    /// <summary>
    /// 获取父ID
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    string GetParentIdById(string Id);

    /// <summary>
    /// 获取导航页面ID
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    string GetMenuPageIdById(string Id);

    /// <summary>
    /// 获取角色导航菜单页面ID
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="navMenuType"></param>
    /// <returns></returns>
    List<IEnumerable<string>> GetIdsForMenu(string roleId, NavMenuType navMenuType = NavMenuType.RoleBackstage);



}
