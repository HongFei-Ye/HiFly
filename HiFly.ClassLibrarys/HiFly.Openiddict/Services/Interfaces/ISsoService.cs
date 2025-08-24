// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Http;

namespace HiFly.Openiddict.Services.Interfaces;

/// <summary>
/// 单点登录(SSO)服务接口，提供跨应用的统一身份验证功能
/// </summary>
public interface ISsoService
{
    /// <summary>
    /// 检查用户在SSO系统中的会话状态
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>如果用户已在SSO系统中认证返回true，否则返回false</returns>
    Task<bool> CheckSsoSessionAsync(HttpContext context);

    /// <summary>
    /// 启动SSO登录流程
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="returnUrl">登录成功后的返回URL</param>
    /// <param name="clientId">客户端标识符</param>
    /// <returns>重定向到认证服务器的URL</returns>
    Task<string> InitiateSsoLoginAsync(HttpContext context, string returnUrl = "/", string? clientId = null);

    /// <summary>
    /// 处理SSO登录回调
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>处理成功返回true，否则返回false</returns>
    Task<bool> HandleSsoCallbackAsync(HttpContext context);

    /// <summary>
    /// 启动SSO登出流程
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="postLogoutRedirectUri">登出后的重定向URI</param>
    /// <returns>重定向到认证服务器登出端点的URL</returns>
    Task<string> InitiateSsoLogoutAsync(HttpContext context, string? postLogoutRedirectUri = null);

    /// <summary>
    /// 处理SSO登出回调
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>处理成功返回true，否则返回false</returns>
    Task<bool> HandleSsoLogoutCallbackAsync(HttpContext context);

    /// <summary>
    /// 获取SSO会话信息
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>会话信息，如果不存在则返回null</returns>
    Task<SsoSessionInfo?> GetSsoSessionInfoAsync(HttpContext context);

    /// <summary>
    /// 检查是否需要重新认证
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="maxAge">最大认证年龄（秒）</param>
    /// <returns>如果需要重新认证返回true，否则返回false</returns>
    Task<bool> RequiresReauthenticationAsync(HttpContext context, int? maxAge = null);

    /// <summary>
    /// 获取SSO认证服务器的元数据
    /// </summary>
    /// <param name="authority">认证服务器地址</param>
    /// <returns>元数据信息</returns>
    Task<SsoMetadata?> GetSsoMetadataAsync(string authority);

    /// <summary>
    /// 验证会话的有效性
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>会话有效返回true，否则返回false</returns>
    Task<bool> ValidateSessionAsync(HttpContext context);

    /// <summary>
    /// 刷新SSO会话
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>刷新成功返回true，否则返回false</returns>
    Task<bool> RefreshSsoSessionAsync(HttpContext context);
}

/// <summary>
/// SSO会话信息
/// </summary>
public class SsoSessionInfo
{
    /// <summary>
    /// 会话标识符
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 用户标识符
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 认证时间
    /// </summary>
    public DateTimeOffset AuthTime { get; set; }

    /// <summary>
    /// 会话过期时间
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 认证方法
    /// </summary>
    public string AuthenticationMethod { get; set; } = string.Empty;

    /// <summary>
    /// 身份提供者
    /// </summary>
    public string IdentityProvider { get; set; } = string.Empty;

    /// <summary>
    /// 客户端应用列表
    /// </summary>
    public List<string> ClientApplications { get; set; } = new();

    /// <summary>
    /// 是否为活跃会话
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// SSO元数据信息
/// </summary>
public class SsoMetadata
{
    /// <summary>
    /// 颁发者
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 授权端点
    /// </summary>
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 令牌端点
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 用户信息端点
    /// </summary>
    public string UserInfoEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 登出端点
    /// </summary>
    public string EndSessionEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 检查会话端点
    /// </summary>
    public string CheckSessionEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// 支持的作用域
    /// </summary>
    public List<string> ScopesSupported { get; set; } = new();

    /// <summary>
    /// 支持的响应类型
    /// </summary>
    public List<string> ResponseTypesSupported { get; set; } = new();

    /// <summary>
    /// 支持的认证方法
    /// </summary>
    public List<string> TokenEndpointAuthMethodsSupported { get; set; } = new();
}
