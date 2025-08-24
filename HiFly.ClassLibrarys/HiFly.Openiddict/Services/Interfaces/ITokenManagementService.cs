// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace HiFly.Openiddict.Services.Interfaces;

/// <summary>
/// Token管理服务接口，提供统一的Token操作功能
/// </summary>
public interface ITokenManagementService
{
    /// <summary>
    /// 获取当前用户的访问令牌
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>访问令牌，如果不存在则返回null</returns>
    Task<string?> GetAccessTokenAsync(HttpContext context);

    /// <summary>
    /// 获取当前用户的刷新令牌
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>刷新令牌，如果不存在则返回null</returns>
    Task<string?> GetRefreshTokenAsync(HttpContext context);

    /// <summary>
    /// 获取当前用户的ID令牌
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>ID令牌，如果不存在则返回null</returns>
    Task<string?> GetIdTokenAsync(HttpContext context);

    /// <summary>
    /// 检查访问令牌是否即将过期
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="threshold">提前刷新阈值（分钟），默认为5分钟</param>
    /// <returns>如果令牌即将过期返回true，否则返回false</returns>
    Task<bool> IsAccessTokenExpiringSoonAsync(HttpContext context, int threshold = 5);

    /// <summary>
    /// 自动刷新访问令牌
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>刷新成功返回true，否则返回false</returns>
    Task<bool> RefreshAccessTokenAsync(HttpContext context);

    /// <summary>
    /// 检查令牌有效性
    /// </summary>
    /// <param name="token">要验证的令牌</param>
    /// <returns>令牌有效返回true，否则返回false</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// 获取令牌的剩余有效时间
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>剩余有效时间，如果令牌无效则返回null</returns>
    Task<TimeSpan?> GetTokenRemainingLifetimeAsync(HttpContext context);

    /// <summary>
    /// 清除用户的所有令牌
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>操作完成的Task</returns>
    Task ClearTokensAsync(HttpContext context);

    /// <summary>
    /// 获取令牌到期时间
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>令牌到期时间，如果无法获取则返回null</returns>
    Task<DateTimeOffset?> GetTokenExpirationAsync(HttpContext context);

    /// <summary>
    /// 保存令牌信息到认证属性
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="properties">认证属性</param>
    /// <returns>操作完成的Task</returns>
    Task SaveTokensAsync(HttpContext context, AuthenticationProperties properties);
}
