// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.NavMenus.Interfaces;
using HiFly.Openiddict.NavMenus.Models;
using HiFly.Openiddict.NavMenus.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiFly.Openiddict.NavMenus.Services;

public class RoleMenuService<TContext, TRoleMenu>(IDbContextFactory<TContext> factory) : IRoleMenuService
    where TContext : DbContext
    where TRoleMenu : class, IRoleMenu, new()
{
    private readonly IDbContextFactory<TContext> _dbFactory = factory;


    /// <summary>
    /// 获取父ID
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    public string GetParentIdById(string Id)
    {
        var context = _dbFactory.CreateDbContext();

        var roleNavMenu = context.Set<TRoleMenu>().FirstOrDefault(rnm => rnm.Id == Id);

        return roleNavMenu?.ParentId ?? "";
    }

    /// <summary>
    /// 获取导航页面ID
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    public string GetMenuPageIdById(string Id)
    {
        var context = _dbFactory.CreateDbContext();

        var roleNavMenu = context.Set<TRoleMenu>().FirstOrDefault(rnm => rnm.Id == Id);

        return roleNavMenu?.MenuPageId ?? "";
    }

    /// <summary>
    /// 获取角色导航菜单页面ID
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="navMenuType"></param>
    /// <returns></returns>
    public List<IEnumerable<string>> GetIdsForMenu(string roleId, NavMenuType navMenuType = NavMenuType.RoleBackstage)
    {
        using var context = _dbFactory.CreateDbContext();

        // 获取 角色 所有的 导航菜单
        var roleNavMenus = context.Set<TRoleMenu>()
            .Where(rnm => rnm.BelongRoleId == roleId && rnm.NavMenuType == navMenuType);

        // 对每个分组按照 Hierarchy 进行排序
        var menuPageIds = roleNavMenus
            .GroupBy(rnm => rnm.Hierarchy) // 按照 Hierarchy 进行分组
            .OrderBy(group => group.Key) // 根据 Hierarchy 进行排序
            .Select(group => group.OrderBy(rnm => rnm.Sequence)
                                  .Select(rnm => rnm.Id)) // 根据 Sequence 进行排序
            .ToList(); // 外层转为List以立即执行查询

        return menuPageIds;
    }




}
