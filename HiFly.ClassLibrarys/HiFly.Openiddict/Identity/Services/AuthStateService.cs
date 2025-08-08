// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Identity.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace HiFly.Openiddict.Identity.Services;

/// <summary>
/// 用户认证状态服务，提供对当前用户身份信息的访问功能
/// </summary>
/// <param name="authenticationStateProvider">身份验证状态提供程序</param>
public class AuthStateService(AuthenticationStateProvider authenticationStateProvider) : IAuthStateService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider = authenticationStateProvider;

    /// <summary>
    /// 获取当前用户的 ClaimsPrincipal 对象
    /// </summary>
    /// <returns>表示当前用户的 ClaimsPrincipal 对象</returns>
    private async Task<ClaimsPrincipal> GetUserAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User;
    }

    /// <summary>
    /// 检查当前用户是否已通过身份验证
    /// </summary>
    /// <returns>如果用户已登录则返回 true，否则返回 false</returns>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var user = await GetUserAsync();
        return user.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// 获取当前已验证用户的用户名
    /// </summary>
    /// <returns>当前用户的用户名，如果用户未登录则返回空字符串</returns>
    public async Task<string> GetUserNameAsync()
    {
        var user = await GetUserAsync();
        return user.Identity?.Name ?? "";
    }

    /// <summary>
    /// 获取当前已验证用户的唯一标识符
    /// </summary>
    /// <returns>当前用户的ID，如果用户未登录或未找到ID则返回空字符串</returns>
    public async Task<string> GetUserIdAsync()
    {
        var user = await GetUserAsync();
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    /// <summary>
    /// 获取当前用户所属的所有角色
    /// </summary>
    /// <returns>用户角色的集合，如果用户未登录或没有角色则返回空集合</returns>
    public async Task<IEnumerable<string>> GetUserRolesAsync()
    {
        var user = await GetUserAsync();
        return user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value);
    }

    /// <summary>
    /// 检查当前用户是否属于指定角色
    /// </summary>
    /// <param name="role">要检查的角色名称</param>
    /// <returns>如果用户属于指定角色则返回 true，否则返回 false</returns>
    public async Task<bool> IsInRoleAsync(string role)
    {
        var user = await GetUserAsync();
        return user.IsInRole(role);
    }

    /// <summary>
    /// 检查当前用户是否拥有指定类型和值的声明
    /// </summary>
    /// <param name="claimType">声明类型</param>
    /// <param name="claimValue">声明值</param>
    /// <returns>如果用户拥有指定声明则返回 true，否则返回 false</returns>
    public async Task<bool> HasClaimAsync(string claimType, string claimValue)
    {
        var user = await GetUserAsync();
        return user.HasClaim(claimType, claimValue);
    }

    /// <summary>
    /// 获取当前用户指定类型声明的值
    /// </summary>
    /// <param name="claimType">要获取的声明类型</param>
    /// <returns>指定类型声明的值，如果不存在则返回空字符串</returns>
    public async Task<string> GetClaimValueAsync(string claimType)
    {
        var user = await GetUserAsync();
        return user.FindFirst(claimType)?.Value ?? "";
    }

    /// <summary>
    /// 获取当前用户的所有声明
    /// </summary>
    /// <returns>用户声明的集合，如果用户未登录则可能返回空集合</returns>
    public async Task<IEnumerable<Claim>> GetUserClaimsAsync()
    {
        var user = await GetUserAsync();
        return user.Claims;
    }

    /// <summary>
    /// 检查当前用户是否拥有特定权限
    /// </summary>
    /// <param name="permission">要检查的权限名称</param>
    /// <returns>如果用户拥有指定权限则返回 true，否则返回 false</returns>
    public async Task<bool> HasPermissionAsync(string permission)
    {
        var user = await GetUserAsync();
        return user.HasClaim("Permission", permission);
    }

    /// <summary>
    /// 获取当前用户的自定义声明值
    /// </summary>
    /// <param name="claimType">自定义声明类型</param>
    /// <returns>指定类型声明的值，如果不存在则返回空字符串</returns>
    public async Task<string> GetCustomClaimAsync(string claimType)
    {
        var user = await GetUserAsync();
        return user.FindFirst(claimType)?.Value ?? "";
    }



}

