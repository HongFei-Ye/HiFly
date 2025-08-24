// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Interfaces;
using HiFly.Openiddict.Middleware;
using HiFly.Openiddict.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HiFly.Openiddict.Extensions;

/// <summary>
/// HiFly OpenIddict 高级扩展服务
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加完整的HiFly SSO客户端服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureClient">客户端配置</param>
    /// <param name="configureSso">SSO配置</param>
    /// <param name="configureTokenManagement">Token管理配置</param>
    /// <param name="configureSecurity">安全配置</param>
    /// <param name="configureAudit">审计配置</param>
    /// <param name="configureEndpoints">端点配置（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHiFlySsoClient(
        this IServiceCollection services,
        Action<HiFlyOpenIdClientOptions> configureClient,
        Action<SsoOptions>? configureSso = null,
        Action<TokenManagementOptions>? configureTokenManagement = null,
        Action<SecurityOptions>? configureSecurity = null,
        Action<AuditOptions>? configureAudit = null,
        Action<ClientEndpointOptions>? configureEndpoints = null)
    {
        // 添加基础OpenID Connect客户端 (从 ClientConfiguration)
        services.AddHiFlyOpenIdClient(configureClient);

        // 配置SSO选项
        if (configureSso != null)
        {
            services.Configure<SsoOptions>(configureSso);
        }

        // 配置Token管理选项
        if (configureTokenManagement != null)
        {
            services.Configure<TokenManagementOptions>(configureTokenManagement);
        }

        // 配置安全选项
        if (configureSecurity != null)
        {
            services.Configure<SecurityOptions>(configureSecurity);
        }

        // 配置审计选项
        if (configureAudit != null)
        {
            services.Configure<AuditOptions>(configureAudit);
        }

        // 配置端点选项（如果提供）
        if (configureEndpoints != null)
        {
            services.Configure<ClientEndpointOptions>(configureEndpoints);
        }

        return services;
    }

    /// <summary>
    /// 添加自定义Token验证器
    /// </summary>
    /// <typeparam name="TValidator">验证器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCustomTokenValidator<TValidator>(this IServiceCollection services)
        where TValidator : class, ITokenValidator
    {
        services.AddScoped<ITokenValidator, TValidator>();
        return services;
    }

    /// <summary>
    /// 添加分布式Token缓存
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureCache">缓存配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDistributedTokenCache(
        this IServiceCollection services,
        Action<TokenCacheOptions>? configureCache = null)
    {
        services.AddMemoryCache();
        
        // 注意：需要在应用程序中单独配置Redis
        // services.AddStackExchangeRedisCache(options => { ... });

        if (configureCache != null)
        {
            services.Configure<TokenCacheOptions>(configureCache);
        }

        services.AddScoped<ITokenCacheService, DistributedTokenCacheService>();
        return services;
    }
}

/// <summary>
/// HiFly OpenIddict 应用程序构建器扩展方法
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用完整的HiFly SSO中间件管道
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="configureOptions">中间件配置</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseHiFlySso(
        this IApplicationBuilder app,
        Action<SsoMiddlewareOptions>? configureOptions = null)
    {
        var options = new SsoMiddlewareOptions();
        configureOptions?.Invoke(options);

        // 使用自动Token刷新中间件
        if (options.EnableAutoTokenRefresh)
        {
            app.UseAutoTokenRefresh();
        }

        // 使用会话同步中间件
        if (options.EnableSessionSync)
        {
            app.UseMiddleware<SessionSyncMiddleware>();
        }

        // 使用安全中间件
        if (options.EnableSecurityHeaders)
        {
            app.UseMiddleware<SecurityHeadersMiddleware>();
        }

        return app;
    }

    /// <summary>
    /// 映射HiFly SSO客户端端点（简化版本）
    /// </summary>
    /// <param name="app">Web应用程序</param>
    /// <param name="configureOptions">端点配置</param>
    /// <returns>Web应用程序</returns>
    public static WebApplication MapHiFlySsoEndpoints(
        this WebApplication app,
        Action<ClientEndpointOptions>? configureOptions = null)
    {
        // 直接调用ClientConfiguration中的方法
        app.MapHiFlyOpenIdClientEndpoints(configureOptions);
        return app;
    }

    /// <summary>
    /// 使用CORS策略（为SSO优化）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="allowedOrigins">允许的源</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseHiFlySsoCors(
        this IApplicationBuilder app,
        params string[] allowedOrigins)
    {
        app.UseCors(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });

        return app;
    }
}
