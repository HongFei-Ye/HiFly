// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.Identity.Services.Interfaces;

public interface IRoleService
{
    /// <summary>
    /// 根据角色名称获取角色ID
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    string? GetRoleIdByName(string roleName);

    /// <summary>
    /// 获取角色显示名称
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    string? GetRoleShowNameById(string roleId);

    /// <summary>
    /// 获取角色显示名称
    /// </summary>
    /// <param name="roleName"></param>
    /// <returns></returns>
    string? GetRoleShowNameByName(string roleName);

    /// <summary>
    /// 获取角色等级
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    int GetRoleHierarchy(string roleId);


}
