// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using System.Security.Claims;

namespace HiFly.Openiddict.Identity.Services.Interfaces;

/// <summary>
/// 提供用户认证状态的访问服务，用于快速获取当前用户的身份信息
/// </summary>
public interface IAuthStateService
{
    /// <summary>
    /// 异步检查当前用户是否已通过身份验证
    /// </summary>
    /// <returns>
    /// 如果用户已通过身份验证则返回 <c>true</c>，否则返回 <c>false</c>
    /// </returns>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// 异步获取当前已验证用户的用户名
    /// </summary>
    /// <returns>当前用户的用户名，如果用户未登录则可能返回 <c>null</c></returns>
    Task<string> GetUserNameAsync();

    /// <summary>
    /// 异步获取当前已验证用户的唯一标识符
    /// </summary>
    /// <returns>当前用户的ID，如果用户未登录则可能返回 <c>null</c></returns>
    Task<string> GetUserIdAsync();

    /// <summary>
    /// 异步获取当前用户所属的所有角色
    /// </summary>
    /// <returns>用户角色的集合，如果用户未登录或没有角色则返回空集合</returns>
    Task<IEnumerable<string>> GetUserRolesAsync();

    /// <summary>
    /// 异步检查当前用户是否属于指定角色
    /// </summary>
    /// <param name="role">要检查的角色名称</param>
    /// <returns>如果用户属于指定角色则返回 <c>true</c>，否则返回 <c>false</c></returns>
    Task<bool> IsInRoleAsync(string role);

    /// <summary>
    /// 异步检查当前用户是否拥有指定类型和值的声明
    /// </summary>
    /// <param name="claimType">声明类型</param>
    /// <param name="claimValue">声明值</param>
    /// <returns>如果用户拥有指定声明则返回 <c>true</c>，否则返回 <c>false</c></returns>
    Task<bool> HasClaimAsync(string claimType, string claimValue);

    /// <summary>
    /// 异步获取当前用户指定类型声明的值
    /// </summary>
    /// <param name="claimType">要获取的声明类型</param>
    /// <returns>指定类型声明的值，如果不存在则返回 <c>null</c></returns>
    Task<string> GetClaimValueAsync(string claimType);

    /// <summary>
    /// 异步获取当前用户的所有声明
    /// </summary>
    /// <returns>用户声明的集合，如果用户未登录则返回空集合</returns>
    Task<IEnumerable<Claim>> GetUserClaimsAsync();

    /// <summary>
    /// 检查当前用户是否拥有特定权限
    /// </summary>
    /// <param name="permission">要检查的权限名称</param>
    /// <returns>如果用户拥有指定权限则返回 true，否则返回 false</returns>
    Task<bool> HasPermissionAsync(string permission);

    /// <summary>
    /// 获取当前用户的自定义声明值
    /// </summary>
    /// <param name="claimType">自定义声明类型</param>
    /// <returns>指定类型声明的值，如果不存在则返回空字符串</returns>
    Task<string> GetCustomClaimAsync(string claimType);


}
