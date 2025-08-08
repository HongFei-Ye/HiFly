// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Identity.Data.Interfaces;
using HiFly.Openiddict.Identity.Services;
using HiFly.Openiddict.Identity.Services.Interfaces;
using HiFly.Openiddict.NavMenus.Interfaces;
using HiFly.Openiddict.NavMenus.Services;
using HiFly.Openiddict.NavMenus.Services.Interfaces;
using HiFly.Openiddict.Options;
using HiFly.Openiddict.Structure.Data.Interfaces;
using HiFly.Openiddict.Structure.Data.Services;
using HiFly.Openiddict.Structure.Data.Services.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace HiFly.Openiddict;

/// <summary>
/// OpenIddict认证服务配置
/// </summary>
public static class ServerConfiguration
{
    /// <summary>
    /// 注册Identity服务配置
    /// </summary>
    public static IServiceCollection AddHiFlyIdentity<TContext, TUser, TRole>(
        this IServiceCollection services,
        Action<ServerUserOptions>? configureOptions = null)
        where TContext : DbContext
        where TUser : class, IUser, new()
        where TRole : class, IRole, new()
    {
        var options = new ServerUserOptions();
        configureOptions?.Invoke(options);


        // 添加 Identity 服务
        services.AddIdentity<TUser, TRole>(idOptions =>
        {
            // 账户确认和密码恢复配置
            idOptions.SignIn.RequireConfirmedAccount = options.RequireConfirmedAccount;
            idOptions.SignIn.RequireConfirmedEmail = options.RequireConfirmedEmail;
            idOptions.SignIn.RequireConfirmedPhoneNumber = options.RequireConfirmedPhoneNumber;

            // 密码复杂度配置
            idOptions.Password.RequireDigit = options.PasswordRequireDigit;
            idOptions.Password.RequireLowercase = options.PasswordRequireLowercase;
            idOptions.Password.RequireUppercase = options.PasswordRequireUppercase;
            idOptions.Password.RequireNonAlphanumeric = options.PasswordRequireNonAlphanumeric;
            idOptions.Password.RequiredLength = options.PasswordRequiredLength;
            idOptions.Password.RequiredUniqueChars = options.PasswordRequiredUniqueChars;

            // 锁定策略配置
            idOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(options.DefaultLockoutMinutes);
            idOptions.Lockout.MaxFailedAccessAttempts = options.MaxFailedAccessAttempts;
            idOptions.Lockout.AllowedForNewUsers = options.LockoutAllowedForNewUsers;

            // 用户配置
            idOptions.User.RequireUniqueEmail = options.RequireUniqueEmail;
            idOptions.User.AllowedUserNameCharacters = options.AllowedUserNameCharacters;

            // 存储配置
            idOptions.Stores.MaxLengthForKeys = options.MaxLengthForKeys;
            idOptions.Stores.ProtectPersonalData = options.ProtectPersonalData;
        })
        .AddEntityFrameworkStores<TContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        // 根据配置添加额外的令牌提供程序
        //if (options.AddEmailTokenProvider)
        //{
        //    services.AddAuthentication()
        //        .AddGoogle(googleOptions => {
        //            googleOptions.ClientId = options.GoogleClientId;
        //            googleOptions.ClientSecret = options.GoogleClientSecret;
        //        });
        //}

        // 配置Identity Cookie
        services.ConfigureApplicationCookie(cookieOptions =>
        {
            cookieOptions.Cookie.HttpOnly = options.CookieHttpOnly;
            cookieOptions.Cookie.SecurePolicy = options.CookieSecurePolicy;
            cookieOptions.ExpireTimeSpan = TimeSpan.FromMinutes(options.CookieExpirationMinutes);
            cookieOptions.SlidingExpiration = options.CookieSlidingExpiration;
            cookieOptions.LoginPath = options.LoginPath;
            cookieOptions.LogoutPath = options.LogoutPath;
            cookieOptions.AccessDeniedPath = options.AccessDeniedPath;
        });

        return services;
    }

    /// <summary>
    /// 注册OpenIddict服务配置
    /// </summary>
    public static IServiceCollection AddHiFlyOpenIdServer<TContext>(
        this IServiceCollection services,
        Action<ServerOptions>? configureOptions = null)
        where TContext : DbContext
    {
        var options = new ServerOptions();
        configureOptions?.Invoke(options);

        // 从服务集合中解析 IWebHostEnvironment
        var serviceProvider = services.BuildServiceProvider();
        var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

        // 添加 OpenIddict 配置
        services.AddOpenIddict()
            // 注册 OpenIddict 核心组件
            .AddCore(coreOptions =>
            {
                coreOptions.UseEntityFrameworkCore()
                          .UseDbContext<TContext>();
            })
            // 注册 OpenIddict 服务器组件
            .AddServer(serverOptions =>
            {
                // 配置端点
                serverOptions.SetAuthorizationEndpointUris(options.AuthorizationEndpoint)
                            .SetTokenEndpointUris(options.TokenEndpoint)
                            .SetUserInfoEndpointUris(options.UserInfoEndpoint)
                            .SetIntrospectionEndpointUris(options.IntrospectionEndpoint)
                            .SetEndSessionEndpointUris(options.LogoutEndpoint);

                // 配置授权流程
                if (options.AllowAuthorizationCodeFlow)
                    serverOptions.AllowAuthorizationCodeFlow();
                if (options.AllowPasswordFlow)
                    serverOptions.AllowPasswordFlow();
                if (options.AllowClientCredentialsFlow)
                    serverOptions.AllowClientCredentialsFlow();
                if (options.AllowImplicitFlow)
                    serverOptions.AllowImplicitFlow();
                if (options.RequireProofKeyForCodeExchange)
                    serverOptions.RequireProofKeyForCodeExchange();
                if (options.AllowRefreshTokenFlow)
                    serverOptions.AllowRefreshTokenFlow();

                // 注册标准作用域
                var scopeOptions = new List<string>
                {
                    Scopes.OpenId,
                    Scopes.Email,
                    Scopes.Profile,
                    Scopes.Roles,
                    Scopes.OfflineAccess
                };

                // 添加自定义作用域
                scopeOptions.AddRange(options.CustomScopes);

                // 注册所有作用域
                serverOptions.RegisterScopes([.. scopeOptions]);

                // ASP.NET Core 集成
                serverOptions.UseAspNetCore()
                            .EnableTokenEndpointPassthrough()
                            .EnableAuthorizationEndpointPassthrough()
                            .EnableUserInfoEndpointPassthrough()
                            .EnableEndSessionEndpointPassthrough()
                            .EnableStatusCodePagesIntegration();

                // 在开发环境使用开发证书
                if (environment.IsDevelopment())
                {
                    // 使用开发证书(仅用于测试，不要在实际生产中使用)
                    serverOptions.AddDevelopmentEncryptionCertificate()
                                 .AddDevelopmentSigningCertificate();

                    // 在开发环境中禁用访问令牌加密
                    if (options.DisableAccessTokenEncryption)
                        serverOptions.DisableAccessTokenEncryption();
                }
                else
                {
                    // 在生产环境中添加临时加密密钥和签名密钥
                    // 选项1：使用临时密钥（每次应用启动都会生成新密钥，不适合多实例部署）
                    serverOptions.AddEphemeralEncryptionKey()
                                 .AddEphemeralSigningKey();  // 添加临时签名密钥

                    // 选项2：使用实际证书（推荐用于真正的生产环境）
                    // serverOptions.AddEncryptionCertificate("thumbprint-of-your-certificate")
                    //              .AddSigningCertificate("thumbprint-of-your-certificate");
                }

            })
            // 注册 OpenIddict 验证组件
            .AddValidation(validationOptions =>
            {
                validationOptions.UseLocalServer();
                validationOptions.UseAspNetCore();
            });

        // 添加控制器服务
        services.AddControllers();

        return services;
    }

    /// <summary>
    /// 注册核心应用服务
    /// </summary>
    public static IServiceCollection AddHiFlyAppServices<TContext, TUser, TRole, TMenuPage, TRoleMenu>(
        this IServiceCollection services)
        where TContext : DbContext
        where TUser : class, IUser, new()
        where TRole : class, IRole, new()
        where TMenuPage : class, IMenuPage, new()
        where TRoleMenu : class, IRoleMenu, new()
    {
        // 注册验证状态服务
        services.AddScoped<IAuthStateService, AuthStateService>();

        // 注册用户服务
        services.AddScoped<IUserService, UserService<TContext, TUser>>();

        // 注册用户服务
        services.AddScoped<IRoleService, RoleService<TContext, TRole>>();

        // 注册导航菜单服务
        services.AddScoped<IMenuPageService, MenuPageService<TContext, TMenuPage>>();

        // 注册角色菜单服务
        services.AddScoped<IRoleMenuService, RoleMenuService<TContext, TRoleMenu>>();

        return services;
    }

    public static IServiceCollection AddHiFlyStructureServices<TContext, TOrganization, TUnit, TInstitution, TDepartment, TTeam>
        (this IServiceCollection services)
        where TContext : DbContext
        where TOrganization : class, IOrganization, new()
        where TUnit : class, IUnit, new()
        where TInstitution : class, IInstitution, new()
        where TDepartment : class, IDepartment, new()
        where TTeam : class, ITeam, new()
    {
        // 注册组织服务
        services.AddScoped<IOrganizationService, OrganizationService<TContext, TOrganization>>();

        // 注册单位服务
        services.AddScoped<IUnitService, UnitService<TContext, TUnit>>();

        // 注册机构服务
        services.AddScoped<IInstitutionService, InstitutionService<TContext, TInstitution>>();

        // 注册部门服务
        services.AddScoped<IDepartmentService, DepartmentService<TContext, TDepartment>>();

        // 注册小组服务
        services.AddScoped<ITeamService, TeamService<TContext, TTeam>>();

        return services;
    }


    /// <summary>
    /// 映射OpenIddict认证服务器端点
    /// </summary>
    public static WebApplication MapHiFlyOpenIdServerEndpoints<TUser>(
        this WebApplication app,
        Action<ServerEndpointOptions>? configureOptions = null)
        where TUser : class, IUser
    {
        var options = new ServerEndpointOptions();
        configureOptions?.Invoke(options);

        // 授权端点 - 处理OAuth 2.0授权请求
        app.MapMethods(options.AuthorizationEndpoint, new[] { "GET", "POST" }, async (
            HttpContext httpContext,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IOpenIddictScopeManager scopeManager,
            SignInManager<TUser> signInManager,
            UserManager<TUser> userManager) =>
        {
            var request = httpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("OpenID Connect请求不能为空");

            // 用户未登录时，重定向到登录页面
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                // 将当前完整的请求URL作为returnUrl参数保存
                var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;

                var properties = new AuthenticationProperties
                {
                    RedirectUri = options.LoginPath + $"?ReturnUrl={Uri.EscapeDataString(returnUrl)}"
                };

                return Results.Challenge(properties, new[] { IdentityConstants.ApplicationScheme });
            }

            // 查找请求的客户端应用
            var application = await applicationManager.FindByClientIdAsync(request.ClientId ?? "");
            if (application == null)
            {
                return Results.Problem("未知客户端", statusCode: StatusCodes.Status400BadRequest);
            }

            // 获取当前用户
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user == null)
            {
                return Results.Challenge(
                    properties: new AuthenticationProperties(),
                    authenticationSchemes: new[] { IdentityConstants.ApplicationScheme });
            }

            // 创建ClaimsIdentity
            var identity = new ClaimsIdentity(
                authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                nameType: Claims.Name,
                roleType: Claims.Role);

            // 添加标准声明
            identity.AddClaim(Claims.Subject, await userManager.GetUserIdAsync(user));
            identity.AddClaim(Claims.Email, await userManager.GetEmailAsync(user) ?? "");
            identity.AddClaim(Claims.Name, await userManager.GetUserNameAsync(user) ?? "");

            // 添加会话ID声明用于前端通道登出
            identity.AddClaim("sid", Guid.NewGuid().ToString());

            // 添加角色声明
            foreach (var role in await userManager.GetRolesAsync(user))
            {
                identity.AddClaim(Claims.Role, role);
            }

            // 创建主体
            var principal = new ClaimsPrincipal(identity);

            // 为所有Claims设置目标，确保包含在ID令牌和访问令牌中
            foreach (var claim in principal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, principal));
            }

            // 签名并返回挑战
            return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        });

        // 令牌端点 - 处理Token请求
        app.MapPost(options.TokenEndpoint, async (
            HttpContext httpContext,
            IOpenIddictApplicationManager applicationManager,
            UserManager<TUser> userManager,
            SignInManager<TUser> signInManager) =>
        {
            var request = httpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("OpenID Connect请求不能为空");

            if (request.IsPasswordGrantType())
            {
                var user = await userManager.FindByNameAsync(request.Username ?? "");
                if (user == null)
                {
                    return Results.Forbid(
                        authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "用户名或密码无效"
                        }));
                }

                // 验证密码
                var result = await signInManager.CheckPasswordSignInAsync(user, request.Password ?? "", lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    return Results.Forbid(
                        authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "用户名或密码无效"
                        }));
                }

                // 创建ClaimsIdentity
                var identity = new ClaimsIdentity(
                    authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                // 添加标准声明
                identity.AddClaim(Claims.Subject, await userManager.GetUserIdAsync(user));
                identity.AddClaim(Claims.Email, await userManager.GetEmailAsync(user) ?? "");
                identity.AddClaim(Claims.Name, await userManager.GetUserNameAsync(user) ?? "");

                // 添加角色声明
                foreach (var role in await userManager.GetRolesAsync(user))
                {
                    identity.AddClaim(Claims.Role, role);
                }

                // 设置作用域
                identity.SetScopes(request.GetScopes());

                // 创建主体
                var principal = new ClaimsPrincipal(identity);

                // 为所有Claims设置目标，确保包含在ID令牌和访问令牌中
                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsRefreshTokenGrantType())
            {
                // 处理刷新令牌...
                var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                if (!result.Succeeded)
                {
                    return Results.Forbid(
                        authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "刷新令牌无效"
                        }));
                }

                // 创建新的身份
                var identity = new ClaimsIdentity(result.Principal.Claims,
                    authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                // 设置作用域
                identity.SetScopes(request.GetScopes());

                // 创建主体
                var principal = new ClaimsPrincipal(identity);

                // 为所有Claims设置目标，确保包含在ID令牌和访问令牌中
                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            else if (request.IsAuthorizationCodeGrantType())
            {
                // 授权码流程应该通过认证结果
                var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                if (!result.Succeeded)
                {
                    return Results.Forbid(
                        authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                        properties: new AuthenticationProperties(new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "授权码无效或已过期"
                        }));
                }

                // 创建新的身份
                var identity = new ClaimsIdentity(result.Principal.Claims,
                    authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                var scopes = result.Principal.GetScopes();
                identity.SetScopes(scopes);

                var resources = new HashSet<string>(StringComparer.Ordinal);
                foreach (var resource in result.Principal.GetResources())
                {
                    resources.Add(resource);
                }
                identity.SetResources(resources);

                // 创建主体
                var principal = new ClaimsPrincipal(identity);

                // 为所有Claims设置目标，确保包含在ID令牌和访问令牌中
                foreach (var claim in principal.Claims)
                {
                    claim.SetDestinations(GetDestinations(claim, principal));
                }

                return Results.SignIn(principal, properties: null, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return Results.Forbid(
                authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "不支持的授权类型"
                }));
        });

        // 用户信息端点
        app.MapGet(options.UserInfoEndpoint, async (HttpContext httpContext) =>
        {
            var result = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Results.Challenge(
                    authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
            }

            var claims = result.Principal.Claims.Where(c =>
                c.Type == Claims.Subject ||
                c.Type == Claims.Name ||
                c.Type == Claims.Email ||
                c.Type == Claims.Role)
                .ToDictionary(c => c.Type, c => c.Value);

            return Results.Ok(claims);
        });

        // 登出端点
        app.MapMethods(options.LogoutEndpoint, new[] { "GET", "POST" }, async (
            HttpContext httpContext,
            SignInManager<TUser> signInManager,
            IOpenIddictApplicationManager applicationManager) =>
        {
            // 在清除Cookie前，先获取sid声明
            var sid = httpContext.User.FindFirst("sid")?.Value ?? Guid.NewGuid().ToString();

            // 获取OpenID Connect请求和PostLogoutRedirectUri
            var request = httpContext.GetOpenIddictServerRequest();
            var postLogoutRedirectUri = request?.PostLogoutRedirectUri;

            // 验证PostLogoutRedirectUri是否合法
            bool validRedirectUri = false;
            if (!string.IsNullOrEmpty(postLogoutRedirectUri))
            {
                // 直接遍历应用程序，检查其PostLogoutRedirectUris属性
                await foreach (var app in applicationManager.ListAsync())
                {
                    // 获取应用程序的所有登出后重定向URI
                    var uris = await applicationManager.GetPostLogoutRedirectUrisAsync(app);

                    // 检查是否有匹配的URI
                    foreach (var uri in uris)
                    {
                        if (string.Equals(uri.ToString(), postLogoutRedirectUri, StringComparison.OrdinalIgnoreCase))
                        {
                            validRedirectUri = true;
                            break;
                        }
                    }

                    if (validRedirectUri) break;
                }
            }

            // 清除本地认证Cookie
            await signInManager.SignOutAsync();

            // 获取前端通道登出的客户端列表
            var clients = new List<(string ClientId, string FrontChannelLogoutUri)>();

            // 从数据库获取客户端应用
            await foreach (var application in applicationManager.ListAsync())
            {
                string? clientId = await applicationManager.GetClientIdAsync(application);

                // 获取前端通道登出URI
                var properties = await applicationManager.GetPropertiesAsync(application);
                if (properties.TryGetValue("urn:openid:connect:frontchannel_logout_uri", out var uriElement))
                {
                    var uri = uriElement.GetString();
                    if (!string.IsNullOrEmpty(uri))
                    {
                        clients.Add((clientId!, uri));
                    }
                }
            }

            // 处理前端通道登出
            if (clients.Count > 0)
            {
                // 生成包含iframe的HTML
                var iframes = string.Join("\n", clients.Select(client =>
                    $"<iframe src=\"{HtmlEncode(client.FrontChannelLogoutUri)}?sid={UrlEncode(sid)}&iss={UrlEncode(options.IssuerUri)}\" style=\"display:none;\"></iframe>"));

                // 安全地构建重定向脚本
                string redirectScript = validRedirectUri
                    ? $"window.location.href = '{HtmlEncode(postLogoutRedirectUri)}';"
                    : "window.location.href = '/';";

                var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>登出中...</title>
            <meta charset=""utf-8"">
        </head>
        <body>
            <h1>正在完成登出...</h1>
            {iframes}
            <script>
                window.onload = function() {{
                    // 完成前端通道登出后重定向
                    setTimeout(function() {{
                        {redirectScript}
                    }}, 1500);
                }};
            </script>
        </body>
        </html>";

                return Results.Content(html, "text/html", System.Text.Encoding.UTF8);
            }

            // 处理后端通道登出（没有前端通道登出客户端）
            if (validRedirectUri)
            {
                // 如果有有效的重定向URI，使用SignOut响应
                return Results.SignOut(
                    authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                    properties: new AuthenticationProperties
                    {
                        RedirectUri = postLogoutRedirectUri
                    });
            }
            else
            {
                // 否则重定向到默认页面
                return Results.Redirect("/", false, true);
            }
        });


        // 内省端点
        app.MapPost(options.IntrospectionEndpoint, async (
            HttpContext httpContext,
            IOpenIddictApplicationManager applicationManager) =>
        {
            var request = httpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("OpenID Connect请求不能为空");

            // 内省请求必须包含token参数
            if (string.IsNullOrEmpty(request.Token))
            {
                return Results.Forbid(
                    authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme },
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidRequest,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "令牌参数缺失"
                    }));
            }

            // 内省处理直接传递
            return Results.Challenge(
                properties: new AuthenticationProperties(),
                authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        });

        // 会话检查端点
        app.MapGet(options.SessionCheckEndpoint, (HttpContext httpContext) =>
        {
            // 检查用户是否已认证
            var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;

            // 允许跨域请求
            httpContext.Response.Headers.Append("Access-Control-Allow-Origin", options.AllowedOrigins);
            httpContext.Response.Headers.Append("Access-Control-Allow-Credentials", "true");

            return Results.Ok(new { active = isAuthenticated });
        });

        // CORS预检请求支持
        app.MapMethods(options.SessionCheckEndpoint, new[] { "OPTIONS" }, (HttpContext httpContext) =>
        {
            httpContext.Response.Headers.Append("Access-Control-Allow-Origin", options.AllowedOrigins);
            httpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, OPTIONS");
            httpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
            httpContext.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
            httpContext.Response.Headers.Append("Access-Control-Max-Age", "86400");

            return Results.Ok();
        });

        return app;
    }


    /// <summary>
    /// 确定声明应包含在哪些令牌中
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        // 根据声明类型确定目标位置
        switch (claim.Type)
        {
            case Claims.Name:
            case Claims.Subject:
            case Claims.Email:
                // 这些声明应该包含在访问令牌中
                yield return Destinations.AccessToken;

                // 如果请求包含OpenID范围，也包含在身份令牌中
                if (principal.HasScope(Scopes.OpenId))
                    yield return Destinations.IdentityToken;

                break;

            case Claims.Role:
                // 角色声明包含在访问令牌中
                yield return Destinations.AccessToken;

                // 如果请求包含OpenID和角色范围，也包含在身份令牌中
                if (principal.HasScope(Scopes.OpenId) && principal.HasScope(Scopes.Roles))
                    yield return Destinations.IdentityToken;

                break;

            case "sid":
                // 会话ID应该包含在身份令牌中用于前端通道登出
                yield return Destinations.IdentityToken;
                break;

            // 对于其他声明类型，仅包含在访问令牌中
            default:
                yield return Destinations.AccessToken;
                break;
        }
    }


    private static string HtmlEncode(string? text)
    {
        return string.IsNullOrEmpty(text) ? "" : System.Web.HttpUtility.HtmlEncode(text);
    }

    private static string UrlEncode(string? text)
    {
        return string.IsNullOrEmpty(text) ? "" : Uri.EscapeDataString(text);
    }



    /// <summary>
    /// 初始化OpenIddict客户端，不存在则创建，存在则更新
    /// </summary>
    /// <remarks>
    /// 该方法会检查每个客户端是否已存在。对于已存在的客户端，会更新其配置；
    /// 对于不存在的客户端，会创建新的客户端记录。
    /// </remarks>
    public static async Task InitializeClientsAsync(
        this IServiceProvider services,
        IEnumerable<ServerClientOptions> clientOptions)
    {
        using var scope = services.CreateScope();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in clientOptions)
        {
            if (string.IsNullOrEmpty(client.ClientId))
                continue;

            // 尝试查找现有客户端
            var existingApplication = await applicationManager.FindByClientIdAsync(client.ClientId);

            // 创建或更新应用程序描述符
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = client.ClientId,
                ClientSecret = client.ClientSecret,
                DisplayName = client.DisplayName,
                ClientType = client.ClientType,
                ConsentType = client.ConsentType
            };

            // 添加重定向URI
            foreach (var uri in client.RedirectUris)
            {
                descriptor.RedirectUris.Add(new Uri(uri));
            }

            // 添加登出后重定向URI
            foreach (var uri in client.PostLogoutRedirectUris)
            {
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
            }

            // 添加权限
            foreach (var permission in client.Permissions)
            {
                descriptor.Permissions.Add(permission);
            }

            // 设置前端通道登出URI（如果提供）
            if (!string.IsNullOrEmpty(client.FrontChannelLogoutUri))
            {
                descriptor.Properties.Add(
                    "urn:openid:connect:frontchannel_logout_uri",
                    JsonDocument.Parse($"\"{client.FrontChannelLogoutUri}\"").RootElement);

                descriptor.Properties.Add(
                    "urn:openid:connect:frontchannel_logout_session_required",
                    JsonDocument.Parse($"\"{client.FrontChannelLogoutSessionRequired}\"").RootElement);
            }

            // 根据客户端是否存在执行创建或更新操作
            if (existingApplication is null)
            {
                // 创建新客户端
                await applicationManager.CreateAsync(descriptor);
            }
            else
            {
                // 更新现有客户端
                await applicationManager.UpdateAsync(existingApplication, descriptor);
            }
        }
    }

    /// <summary>
    /// 初始化默认用户和角色
    /// </summary>
    public static async Task InitializeDefaultUserAsync<TContext, TUser, TRole>(
        this IServiceProvider services,
        Action<ServerUserOptions>? configureOptions = null)
        where TContext : DbContext
        where TUser : class, IUser, new()
        where TRole : class, IRole, new()
    {
        var options = new ServerUserOptions();
        configureOptions?.Invoke(options);

        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<TRole>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        // 创建角色和权限
        foreach (var role in options.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Key))
            {
                var identityRole = new TRole
                {
                    Name = role.Key,
                    ShowName = role.Value.ShowName
                };
                var result = await roleManager.CreateAsync(identityRole);

                if (!result.Succeeded)
                {
                    throw new Exception($"创建角色 {role.Key} 失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // 为角色添加声明（权限）
                foreach (var permission in role.Value.Permissions)
                {
                    await roleManager.AddClaimAsync(identityRole, new Claim("permission", permission));
                }
            }
        }

        // 创建默认系统管理员用户（如果不存在）
        if (!(await userManager.GetUsersInRoleAsync(options.SystemAdminUserName)).Any())
        {
            var adminUser = new TUser
            {
                UserName = options.SystemAdminUserName,
                Email = options.SystemAdminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, options.SystemAdminPassword);
            if (!result.Succeeded)
            {
                throw new Exception($"创建管理员用户失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // 添加到管理员角色
            result = await userManager.AddToRoleAsync(adminUser, options.SystemAdminUserName);
            if (!result.Succeeded)
            {
                throw new Exception($"将用户添加到管理员角色失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

    }

}

/// <summary>
/// 服务器端点选项
/// </summary>
public class ServerEndpointOptions
{
    // 基本端点路径
    public string AuthorizationEndpoint { get; set; } = "/connect/authorize";
    public string TokenEndpoint { get; set; } = "/connect/token";
    public string UserInfoEndpoint { get; set; } = "/connect/userinfo";
    public string IntrospectionEndpoint { get; set; } = "/connect/introspect";
    public string LogoutEndpoint { get; set; } = "/connect/logout";
    public string SessionCheckEndpoint { get; set; } = "/api/session/check";

    // 登录页面路径
    public string LoginPath { get; set; } = "/Account/Login";

    // 颁发者URI（用于前端通道登出）
    public string IssuerUri { get; set; } = "https://localhost:6100";

    // CORS允许的源
    public string AllowedOrigins { get; set; } = "*";
}

