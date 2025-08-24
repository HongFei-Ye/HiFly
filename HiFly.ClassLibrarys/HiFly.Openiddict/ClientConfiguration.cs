// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Identity.Services;
using HiFly.Identity.Services.Interfaces;
using HiFly.Openiddict.Middleware;
using HiFly.Openiddict.Services;
using HiFly.Openiddict.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        // 注册配置选项
        services.Configure<HiFlyOpenIdClientOptions>(opt =>
        {
            opt.Authority = options.Authority;
            opt.ClientId = options.ClientId;
            opt.ClientSecret = options.ClientSecret;
            opt.CookieName = options.CookieName;
            opt.CookieExpiration = options.CookieExpiration;
            opt.ResponseMode = options.ResponseMode;
            opt.NameClaimType = options.NameClaimType;
            opt.RoleClaimType = options.RoleClaimType;
            opt.Scopes = options.Scopes;
            opt.ConfigureOpenIdConnectOptions = options.ConfigureOpenIdConnectOptions;
            opt.RequireHttpsMetadata = options.RequireHttpsMetadata;
            opt.DisableSslCertificateValidation = options.DisableSslCertificateValidation;
            opt.BackchannelHttpHandler = options.BackchannelHttpHandler;
            opt.EnableAutoTokenRefresh = options.EnableAutoTokenRefresh;
            opt.AutoRefreshOptions = options.AutoRefreshOptions;
        });

        services.AddAuthentication(authOptions =>
        {
            authOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            authOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(cookieOptions =>
        {
            cookieOptions.Cookie.Name = options.CookieName;
            cookieOptions.ExpireTimeSpan = options.CookieExpiration;
            cookieOptions.SlidingExpiration = true;
            
            // 配置Cookie安全选项
            cookieOptions.Cookie.HttpOnly = true;
            cookieOptions.Cookie.SameSite = SameSiteMode.Lax;
            cookieOptions.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            
            // 配置事件处理
            cookieOptions.Events.OnValidatePrincipal = async context =>
            {
                // 在Cookie验证时检查令牌状态
                if (options.EnableAutoTokenRefresh)
                {
                    try
                    {
                        var tokenService = context.HttpContext.RequestServices.GetService<ITokenManagementService>();
                        if (tokenService != null)
                        {
                            var isExpiring = await tokenService.IsAccessTokenExpiringSoonAsync(context.HttpContext);
                            if (isExpiring)
                            {
                                var refreshed = await tokenService.RefreshAccessTokenAsync(context.HttpContext);
                                if (!refreshed)
                                {
                                    // 刷新失败，可以选择让用户重新登录
                                    // context.RejectPrincipal();
                                    // await context.HttpContext.SignOutAsync();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但不阻止Cookie验证
                        var loggerFactory = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("HiFly.Openiddict.ClientConfiguration");
                        logger?.LogError(ex, "Cookie验证过程中的Token刷新失败");
                    }
                }
            };
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

            // 配置PKCE支持
            oidcOptions.UsePkce = true;

            // 配置令牌验证参数
            oidcOptions.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = options.NameClaimType,
                RoleClaimType = options.RoleClaimType,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // 允许5分钟的时钟偏差
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

                // 令牌验证事件
                OnTokenValidated = async context =>
                {
                    // 保存令牌信息
                    var tokenService = context.HttpContext.RequestServices.GetService<ITokenManagementService>();
                    if (tokenService != null && context.Properties != null)
                    {
                        await tokenService.SaveTokensAsync(context.HttpContext, context.Properties);
                    }
                },

                // 认证失败事件
                OnAuthenticationFailed = context =>
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
                    var logger = loggerFactory?.CreateLogger("HiFly.Openiddict.Authentication");
                    logger?.LogError(context.Exception, "OpenID Connect认证失败: {Error}", context.Exception.Message);
                    
                    context.HandleResponse();
                    context.Response.Redirect("/error?message=" + Uri.EscapeDataString(context.Exception.Message));
                    return Task.CompletedTask;
                }
            };

            // 允许自定义配置
            options.ConfigureOpenIdConnectOptions?.Invoke(oidcOptions);
        });

        // 添加HTTP客户端工厂
        services.AddHttpClient();

        // 添加认证状态服务
        services.AddCascadingAuthenticationState();
        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        // 注册核心服务
        services.AddScoped<IAuthStateService, AuthStateService>();
        services.AddScoped<ITokenManagementService, TokenManagementService>();
        services.AddScoped<ISsoService, SsoService>();

        // 配置自动Token刷新选项
        if (options.EnableAutoTokenRefresh)
        {
            services.Configure<AutoTokenRefreshOptions>(opt =>
            {
                if (options.AutoRefreshOptions != null)
                {
                    opt.RefreshThresholdMinutes = options.AutoRefreshOptions.RefreshThresholdMinutes;
                    opt.RedirectToLoginOnRefreshFailure = options.AutoRefreshOptions.RedirectToLoginOnRefreshFailure;
                    opt.LoginPath = options.AutoRefreshOptions.LoginPath;
                    opt.AddRefreshHeader = options.AutoRefreshOptions.AddRefreshHeader;
                    opt.SkipAjaxRequests = options.AutoRefreshOptions.SkipAjaxRequests;
                    opt.SkipApiRequests = options.AutoRefreshOptions.SkipApiRequests;
                    opt.ExcludePaths = options.AutoRefreshOptions.ExcludePaths;
                    opt.ExcludeExtensions = options.AutoRefreshOptions.ExcludeExtensions;
                }
            });
        }

        // 添加控制器服务
        services.AddControllers();

        return services;
    }

    /// <summary>
    /// 使用自动Token刷新中间件
    /// </summary>
    public static IApplicationBuilder UseAutoTokenRefresh(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AutoTokenRefreshMiddleware>();
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

        // 添加SSO会话检查端点
        app.MapGet(options.SsoSessionCheckPath, async (HttpContext context, ISsoService ssoService) =>
        {
            var sessionInfo = await ssoService.GetSsoSessionInfoAsync(context);
            var isActive = sessionInfo?.IsActive ?? false;
            
            return Results.Ok(new { active = isActive, session = sessionInfo });
        });

        // 添加Token状态端点
        app.MapGet(options.TokenStatusPath, async (HttpContext context, ITokenManagementService tokenService) =>
        {
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                return Results.Ok(new { authenticated = false });
            }

            var expiresAt = await tokenService.GetTokenExpirationAsync(context);
            var remainingTime = await tokenService.GetTokenRemainingLifetimeAsync(context);
            var isExpiringSoon = await tokenService.IsAccessTokenExpiringSoonAsync(context);

            return Results.Ok(new
            {
                authenticated = true,
                expiresAt = expiresAt?.ToString("O"),
                remainingSeconds = remainingTime?.TotalSeconds,
                isExpiringSoon = isExpiringSoon
            });
        });

        // 添加手动Token刷新端点
        app.MapPost(options.RefreshTokenPath, async (HttpContext context, ITokenManagementService tokenService) =>
        {
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                return Results.Unauthorized();
            }

            var refreshed = await tokenService.RefreshAccessTokenAsync(context);
            return Results.Ok(new { success = refreshed });
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

    /// <summary>
    /// 是否启用自动Token刷新功能
    /// </summary>
    public bool EnableAutoTokenRefresh { get; set; } = true;

    /// <summary>
    /// 自动Token刷新配置选项
    /// </summary>
    public AutoTokenRefreshOptions? AutoRefreshOptions { get; set; } = new();
}

/// <summary>
/// 客户端端点选项
/// </summary>
public class ClientEndpointOptions
{
    public string SigninPath { get; set; } = "/signin";

    public string SignoutPath { get; set; } = "/signout";

    public string DefaultReturnUrl { get; set; } = "/";

    /// <summary>
    /// SSO会话检查端点路径
    /// </summary>
    public string SsoSessionCheckPath { get; set; } = "/api/sso/session";

    /// <summary>
    /// Token状态端点路径
    /// </summary>
    public string TokenStatusPath { get; set; } = "/api/token/status";

    /// <summary>
    /// Token刷新端点路径
    /// </summary>
    public string RefreshTokenPath { get; set; } = "/api/token/refresh";
}




