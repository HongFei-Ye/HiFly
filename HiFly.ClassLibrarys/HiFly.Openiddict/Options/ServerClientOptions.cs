// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.Options;

/// <summary>
/// OpenIddict客户端应用程序配置选项类
/// </summary>
/// <remarks>
/// 用于配置授权服务器信任的OAuth 2.0/OpenID Connect客户端应用程序。
/// 每个客户端可以有不同的权限、重定向URI和登出设置。
/// </remarks>
public class ServerClientOptions
{
    /// <summary>
    /// 客户端应用程序的唯一标识符
    /// </summary>
    /// <remarks>
    /// 必须在授权服务器范围内唯一。用于客户端向授权服务器进行身份验证。
    /// </remarks>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 客户端应用程序的密钥
    /// </summary>
    /// <remarks>
    /// 用于客户端凭据授权流程和令牌端点的客户端身份验证。
    /// 在生产环境中应使用强密码并安全存储。
    /// </remarks>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 客户端应用程序的显示名称
    /// </summary>
    /// <remarks>
    /// 在用户授权页面上显示，让用户了解他们授权的应用程序。
    /// </remarks>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 客户端类型
    /// </summary>
    /// <remarks>
    /// 可选值:
    /// - "confidential": 能够安全保存凭证的客户端(如Web服务器应用)
    /// - "public": 无法安全保存凭证的客户端(如移动或SPA应用)
    /// </remarks>
    public string ClientType { get; set; } = "confidential";

    /// <summary>
    /// 客户端同意类型
    /// </summary>
    /// <remarks>
    /// 可选值:
    /// - explicit: 每次都要求用户同意授权
    /// - implicit: 假定用户已同意，不显示同意页面
    /// - external: 由外部流程处理同意
    /// </remarks>
    public string ConsentType { get; set; } = "explicit";

    /// <summary>
    /// 客户端允许的重定向URI列表
    /// </summary>
    /// <remarks>
    /// 授权成功后，授权服务器将用户重定向到这些URI之一。
    /// 出于安全考虑，必须预先配置并精确匹配。
    /// </remarks>
    public List<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// 客户端允许的登出后重定向URI列表
    /// </summary>
    /// <remarks>
    /// 用户从授权服务器登出后可重定向到的URI。
    /// 必须预先配置以防止开放重定向攻击。
    /// </remarks>
    public List<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// 前端通道登出URI
    /// </summary>
    /// <remarks>
    /// 当用户从授权服务器登出时，将通过前端通道(使用iframe)通知客户端应用程序。
    /// 用于实现单点登出功能，确保用户在所有关联应用中都被登出。
    /// </remarks>
    public string FrontChannelLogoutUri { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要会话ID进行前端通道登出
    /// </summary>
    /// <remarks>
    /// 当设置为true时，前端通道登出请求将包含会话ID(sid)参数，
    /// 以便客户端可以验证登出请求与用户的当前会话相关联。
    /// </remarks>
    public bool FrontChannelLogoutSessionRequired { get; set; } = true;

    /// <summary>
    /// 客户端应用程序的权限列表
    /// </summary>
    /// <remarks>
    /// 定义客户端可以请求的权限，包括：
    /// - 授权类型(例如：Permissions.GrantTypes.AuthorizationCode)
    /// - 响应类型(例如：Permissions.ResponseTypes.Code)
    /// - 可访问的端点(例如：Permissions.Endpoints.Token)
    /// - 可请求的作用域(例如：Permissions.Scopes.Email)
    /// - 自定义资源访问权限(例如：Permissions.Prefixes.Scope + "api")
    /// </remarks>
    public List<string> Permissions { get; set; } = [];
}
