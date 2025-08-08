// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Identity.Services;
using HiFly.Openiddict.Identity.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace HiFly.Openiddict;

/// <summary>
/// OpenID Connect客户端配置
/// </summary>
public static class ClientConfiguration
{
    /// <summary>
    /// 添加OpenID Connect客户端认证
    /// </summary>
    public static IServiceCollection AddHiFlyOpenIdClient(
        this IServiceCollection services,
        Action<HiFlyOpenIdClientOptions> configureOptions)
    {
        var options = new HiFlyOpenIdClientOptions();
        configureOptions(options);

        services.AddAuthentication(authOptions =>
        {
            authOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            authOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(cookieOptions =>
        {
            cookieOptions.Cookie.Name = options.CookieName;
            cookieOptions.ExpireTimeSpan = options.CookieExpiration;
        })
        .AddOpenIdConnect(oidcOptions =>
        {
            oidcOptions.Authority = options.Authority;
            oidcOptions.ClientId = options.ClientId;
            oidcOptions.ClientSecret = options.ClientSecret;

            oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
            oidcOptions.ResponseMode = options.ResponseMode;

            // 配置作用域
            oidcOptions.Scope.Clear();
            foreach (var scope in options.Scopes)
            {
                oidcOptions.Scope.Add(scope);
            }

            // 配置令牌验证参数
            oidcOptions.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = options.NameClaimType,
                RoleClaimType = options.RoleClaimType,
                ValidateLifetime = true
            };

            // 确保从UserInfo端点获取声明
            oidcOptions.GetClaimsFromUserInfoEndpoint = true;

            // 保存令牌和声明
            oidcOptions.SaveTokens = true;

            // 事件处理程序
            oidcOptions.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProviderForSignOut = context =>
                {
                    // 确保状态参数被正确生成
                    if (string.IsNullOrEmpty(context.ProtocolMessage.State))
                    {
                        context.ProtocolMessage.State = Guid.NewGuid().ToString();
                    }

                    return Task.CompletedTask;
                },

                OnSignedOutCallbackRedirect = async context =>
                {
                    // 执行会话清理
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // 重定向到首页
                    context.HandleResponse();
                    context.Response.Redirect("/");
                },

                // 远程会话结束事件处理
                OnRemoteSignOut = async context =>
                {
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    // 重定向到首页
                    context.HandleResponse();
                    context.Response.Redirect("/");
                },

                //OnTokenValidated = context =>
                //{
                //    // 调试输出 - 查看哪些声明可用
                //    var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");

                //    return Task.CompletedTask;
                //}

            };

            // 允许自定义配置
            options.ConfigureOpenIdConnectOptions?.Invoke(oidcOptions);





            //// 配置是否要求 HTTPS
            //oidcOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;

            //// 如果禁用 SSL 证书验证，应添加以下代码
            //if (options.DisableSslCertificateValidation)
            //{
            //    oidcOptions.BackchannelHttpHandler = new HttpClientHandler
            //    {
            //        ServerCertificateCustomValidationCallback =
            //            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            //    };
            //}
            //else if (options.BackchannelHttpHandler != null)
            //{
            //    oidcOptions.BackchannelHttpHandler = options.BackchannelHttpHandler;
            //}

        });

        // 添加认证状态服务
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        // 注册验证状态服务
        services.AddScoped<IAuthStateService, AuthStateService>();

        // 添加控制器服务
        services.AddControllers();

        return services;
    }

    /// <summary>
    /// 映射所有客户端应用需要的OpenID Connect相关端点
    /// </summary>
    public static void MapHiFlyOpenIdClientEndpoints(
        this WebApplication app,
        Action<ClientEndpointOptions>? configureOptions = null)
    {
        var options = new ClientEndpointOptions();
        configureOptions?.Invoke(options);

        // 添加登录端点
        app.MapGet(options.SigninPath, (HttpContext context, [FromQuery] string returnUrl = "/") =>
        {
            // 创建身份验证属性，设置登录成功后的重定向URL
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl,
                IsPersistent = true // 创建持久Cookie
            };

            // 使用OpenID Connect协议发起挑战，将用户重定向到认证服务器
            return Results.Challenge(properties, new[] { OpenIdConnectDefaults.AuthenticationScheme });
        });

        // 添加登出端点
        app.MapMethods(options.SignoutPath, new[] { "GET", "POST" }, async (HttpContext context) =>
        {
            // 首先执行本地登出
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 获取要返回的URL
            string returnUrl = options.DefaultReturnUrl;

            // 使用OpenID Connect协议执行登出，通知身份提供者
            return Results.SignOut(
                properties: new AuthenticationProperties
                {
                    RedirectUri = returnUrl,
                    Items =
                    {
                        ["prompt"] = "logout",
                        ["id_token_hint"] = await context.GetTokenAsync("id_token") // 添加ID令牌提示
                    }
                },
                authenticationSchemes: new[]
                {
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme
                });
        });

    }

}

/// <summary>
/// OpenID Connect客户端选项
/// </summary>
public class HiFlyOpenIdClientOptions
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CookieName { get; set; } = "HiFlyClient";

    public TimeSpan CookieExpiration { get; set; } = TimeSpan.FromHours(8);

    public string ResponseMode { get; set; } = "query";

    public string NameClaimType { get; set; } = Claims.Name;

    public string RoleClaimType { get; set; } = Claims.Role;

    public List<string> Scopes { get; set; } =
    [
        "openid", "profile", "email", "offline_access"
    ];

    public Action<OpenIdConnectOptions>? ConfigureOpenIdConnectOptions { get; set; }


    /// <summary>
    /// 是否要求元数据地址使用 HTTPS
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// 是否禁用 SSL 证书验证（警告：在生产环境中禁用存在安全风险）
    /// </summary>
    public bool DisableSslCertificateValidation { get; set; } = false;

    /// <summary>
    /// 自定义的 HTTP 消息处理器，可用于替代默认配置
    /// </summary>
    public HttpMessageHandler? BackchannelHttpHandler { get; set; }
}

/// <summary>
/// 客户端端点选项
/// </summary>
public class ClientEndpointOptions
{
    public string SigninPath { get; set; } = "/signin";

    public string SignoutPath { get; set; } = "/signout";

    public string DefaultReturnUrl { get; set; } = "/";

}




