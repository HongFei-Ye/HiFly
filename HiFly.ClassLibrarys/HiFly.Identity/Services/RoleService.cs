// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using HiFly.Identity.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiFly.Identity.Services;

public class RoleService<TContext, TItem>(IDbContextFactory<TContext> factory) : IRoleService
    where TContext : DbContext
    where TItem : class, IRole, new()
{
    private readonly IDbContextFactory<TContext> _dbFactory = factory;


    /// <summary>
    /// 根据角色名称获取角色ID
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public Guid? GetRoleIdByName(string roleName)
    {
        using var context = _dbFactory.CreateDbContext();

        var role = context.Set<TItem>().FirstOrDefault(r => r.Name == roleName);

        return role?.Id;
    }

    /// <summary>
    /// 获取角色显示名称
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    public string? GetRoleShowNameById(Guid roleId)
    {
        using var context = _dbFactory.CreateDbContext();

        var role = context.Set<TItem>().FirstOrDefault(r => r.Id == roleId);

        return role?.ShowName;
    }

    /// <summary>
    /// 获取角色显示名称
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    public string? GetRoleShowNameByName(string roleName)
    {
        using var context = _dbFactory.CreateDbContext();

        var role = context.Set<TItem>().FirstOrDefault(r => r.Name == roleName);

        return role?.ShowName;
    }

    /// <summary>
    /// 获取角色等级
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    public int GetRoleHierarchy(Guid roleId)
    {
        using var context = _dbFactory.CreateDbContext();

        var role = context.Set<TItem>().FirstOrDefault(r => r.Id == roleId);

        return role?.Hierarchy ?? 0;
    }



}
