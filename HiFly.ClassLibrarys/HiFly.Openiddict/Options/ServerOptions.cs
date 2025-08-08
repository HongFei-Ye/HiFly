// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.Options;

/// <summary>
/// OpenIddict服务器配置选项，用于配置认证服务器的各种端点和授权流程
/// </summary>
public class ServerOptions
{
    /// <summary>
    /// OAuth2.0授权端点路径，用于处理授权请求
    /// 默认值: "/connect/authorize"
    /// </summary>
    /// <remarks>
    /// 客户端应用会重定向到此端点，让用户进行身份验证并授权访问
    /// </remarks>
    public string AuthorizationEndpoint { get; set; } = "/connect/authorize";

    /// <summary>
    /// OAuth2.0令牌端点路径，用于颁发访问令牌
    /// 默认值: "/connect/token"
    /// </summary>
    /// <remarks>
    /// 客户端应用通过此端点获取访问令牌、ID令牌或刷新令牌
    /// </remarks>
    public string TokenEndpoint { get; set; } = "/connect/token";

    /// <summary>
    /// OpenID Connect用户信息端点路径，用于获取已认证用户的信息
    /// 默认值: "/connect/userinfo"
    /// </summary>
    /// <remarks>
    /// 客户端应用使用访问令牌请求此端点以获取用户详细信息
    /// </remarks>
    public string UserInfoEndpoint { get; set; } = "/connect/userinfo";

    /// <summary>
    /// OAuth2.0令牌内省端点路径，用于验证令牌有效性
    /// 默认值: "/connect/introspect"
    /// </summary>
    /// <remarks>
    /// 资源服务器可通过此端点验证访问令牌的有效性和关联的声明
    /// </remarks>
    public string IntrospectionEndpoint { get; set; } = "/connect/introspect";

    /// <summary>
    /// OpenID Connect登出端点路径，用于结束用户会话
    /// 默认值: "/connect/logout"
    /// </summary>
    /// <remarks>
    /// 客户端应用通过此端点注销用户并终止其会话
    /// </remarks>
    public string LogoutEndpoint { get; set; } = "/connect/logout";

    /// <summary>
    /// 是否启用授权码流程
    /// 默认值: true
    /// </summary>
    /// <remarks>
    /// 授权码流程是最安全的OAuth2.0流程，适合Web应用，包含前端通道和后端通道交换
    /// </remarks>
    public bool AllowAuthorizationCodeFlow { get; set; } = true;

    /// <summary>
    /// 是否启用密码授权流程
    /// 默认值: false
    /// </summary>
    /// <remarks>
    /// 密码流程允许客户端直接使用用户的用户名和密码获取令牌
    /// 仅推荐用于高度受信任的第一方应用，存在安全风险
    /// </remarks>
    public bool AllowPasswordFlow { get; set; } = false;

    /// <summary>
    /// 是否启用客户端凭据流程
    /// 默认值: false
    /// </summary>
    /// <remarks>
    /// 客户端凭据流程适用于服务器到服务器的通信，不涉及用户
    /// 客户端使用自己的凭据获取访问令牌
    /// </remarks>
    public bool AllowClientCredentialsFlow { get; set; } = false;

    /// <summary>
    /// 是否启用隐式流程
    /// 默认值: false
    /// </summary>
    /// <remarks>
    /// 隐式流程是一种旧的流程，直接在浏览器中返回令牌
    /// 由于安全问题，不推荐在新应用中使用
    /// </remarks>
    public bool AllowImplicitFlow { get; set; } = false;

    /// <summary>
    /// 是否支持刷新令牌
    /// 默认值: true
    /// </summary>
    /// <remarks>
    /// 启用此选项允许颁发刷新令牌，客户端可使用刷新令牌获取新的访问令牌
    /// 提高用户体验，无需频繁登录
    /// </remarks>
    public bool AllowRefreshTokenFlow { get; set; } = true;

    /// <summary>
    /// 是否要求使用PKCE(Proof Key for Code Exchange)
    /// 默认值: true
    /// </summary>
    /// <remarks>
    /// PKCE是授权码流程的安全增强机制，可防止授权码被拦截攻击
    /// 强烈建议在公共客户端应用中启用
    /// </remarks>
    public bool RequireProofKeyForCodeExchange { get; set; } = true;

    /// <summary>
    /// 是否禁用访问令牌加密
    /// 默认值: false
    /// </summary>
    /// <remarks>
    /// 仅在开发环境中使用，生产环境应始终加密访问令牌
    /// 禁用加密可简化调试但降低安全性
    /// </remarks>
    public bool DisableAccessTokenEncryption { get; set; } = false;

    /// <summary>
    /// 自定义OAuth2.0作用域列表，用于定义客户端可请求的权限范围
    /// </summary>
    /// <remarks>
    /// 除标准作用域(openid, profile, email等)外，可添加自定义API作用域
    /// 例如: "hrm_api", "ems_api"用于控制对特定API的访问权限
    /// </remarks>
    public List<string> CustomScopes { get; set; } = [];


}
