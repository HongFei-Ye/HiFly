// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HiFly.Openiddict.Middleware;

/// <summary>
/// 会话同步中间件，确保多个应用之间的会话状态同步
/// </summary>
public class SessionSyncMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionSyncMiddleware> _logger;
    private readonly SessionSyncOptions _options;

    public SessionSyncMiddleware(
        RequestDelegate next,
        ILogger<SessionSyncMiddleware> logger,
        IOptions<SessionSyncOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, ISsoService ssoService)
    {
        try
        {
            // 跳过不需要会话同步的请求
            if (ShouldSkipSessionSync(context))
            {
                await _next(context);
                return;
            }

            // 检查用户是否已认证
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // 验证会话有效性
                var isValidSession = await ssoService.ValidateSessionAsync(context);
                
                if (!isValidSession)
                {
                    _logger.LogWarning("会话验证失败，清除本地会话");
                    
                    // 清除本地会话
                    await context.SignOutAsync();
                    
                    // 根据配置决定是否重定向到登录页面
                    if (_options.RedirectToLoginOnSessionInvalid)
                    {
                        var loginUrl = _options.LoginPath ?? "/signin";
                        var returnUrl = context.Request.Path + context.Request.QueryString;
                        context.Response.Redirect($"{loginUrl}?returnUrl={Uri.EscapeDataString(returnUrl)}");
                        return;
                    }
                }
                else
                {
                    // 更新会话活动时间
                    context.Items["LastSessionCheck"] = DateTimeOffset.UtcNow;
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会话同步中间件发生错误");
            await _next(context);
        }
    }

    private bool ShouldSkipSessionSync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (path == null) return false;

        // 跳过静态文件和API端点
        return _options.ExcludePaths.Any(excludePath => 
            path.StartsWith(excludePath.ToLowerInvariant()));
    }
}

/// <summary>
/// 会话同步配置选项
/// </summary>
public class SessionSyncOptions
{
    /// <summary>
    /// 会话验证间隔（秒）
    /// </summary>
    public int ValidationIntervalSeconds { get; set; } = 300; // 5分钟

    /// <summary>
    /// 会话无效时是否重定向到登录页面
    /// </summary>
    public bool RedirectToLoginOnSessionInvalid { get; set; } = true;

    /// <summary>
    /// 登录页面路径
    /// </summary>
    public string LoginPath { get; set; } = "/signin";

    /// <summary>
    /// 排除的路径列表
    /// </summary>
    public List<string> ExcludePaths { get; set; } = new()
    {
        "/api/",
        "/css/",
        "/js/",
        "/images/",
        "/lib/",
        "/signin",
        "/signout"
    };
}

/// <summary>
/// 安全头中间件，添加必要的安全HTTP头
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // 添加安全头
            if (!context.Response.HasStarted)
            {
                AddSecurityHeaders(context);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安全头中间件发生错误");
            await _next(context);
        }
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;

        // X-Content-Type-Options
        if (_options.EnableXContentTypeOptions)
        {
            response.Headers.Append("X-Content-Type-Options", "nosniff");
        }

        // X-Frame-Options
        if (_options.EnableXFrameOptions)
        {
            response.Headers.Append("X-Frame-Options", _options.XFrameOptionsValue);
        }

        // X-XSS-Protection
        if (_options.EnableXXssProtection)
        {
            response.Headers.Append("X-XSS-Protection", "1; mode=block");
        }

        // Referrer-Policy
        if (_options.EnableReferrerPolicy)
        {
            response.Headers.Append("Referrer-Policy", _options.ReferrerPolicyValue);
        }

        // Content-Security-Policy
        if (_options.EnableContentSecurityPolicy && !string.IsNullOrEmpty(_options.ContentSecurityPolicyValue))
        {
            response.Headers.Append("Content-Security-Policy", _options.ContentSecurityPolicyValue);
        }

        // Strict-Transport-Security
        if (_options.EnableStrictTransportSecurity && context.Request.IsHttps)
        {
            response.Headers.Append("Strict-Transport-Security", 
                $"max-age={_options.StrictTransportSecurityMaxAge}; includeSubDomains");
        }

        // Permission-Policy
        if (_options.EnablePermissionsPolicy && !string.IsNullOrEmpty(_options.PermissionsPolicyValue))
        {
            response.Headers.Append("Permissions-Policy", _options.PermissionsPolicyValue);
        }

        // 移除服务器信息头
        if (_options.RemoveServerHeader)
        {
            response.Headers.Remove("Server");
        }
    }
}

/// <summary>
/// 安全头配置选项
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// 启用X-Content-Type-Options头
    /// </summary>
    public bool EnableXContentTypeOptions { get; set; } = true;

    /// <summary>
    /// 启用X-Frame-Options头
    /// </summary>
    public bool EnableXFrameOptions { get; set; } = true;

    /// <summary>
    /// X-Frame-Options值
    /// </summary>
    public string XFrameOptionsValue { get; set; } = "DENY";

    /// <summary>
    /// 启用X-XSS-Protection头
    /// </summary>
    public bool EnableXXssProtection { get; set; } = true;

    /// <summary>
    /// 启用Referrer-Policy头
    /// </summary>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>
    /// Referrer-Policy值
    /// </summary>
    public string ReferrerPolicyValue { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>
    /// 启用Content-Security-Policy头
    /// </summary>
    public bool EnableContentSecurityPolicy { get; set; } = false;

    /// <summary>
    /// Content-Security-Policy值
    /// </summary>
    public string? ContentSecurityPolicyValue { get; set; }

    /// <summary>
    /// 启用Strict-Transport-Security头
    /// </summary>
    public bool EnableStrictTransportSecurity { get; set; } = true;

    /// <summary>
    /// Strict-Transport-Security最大年龄（秒）
    /// </summary>
    public int StrictTransportSecurityMaxAge { get; set; } = 31536000; // 1年

    /// <summary>
    /// 启用Permissions-Policy头
    /// </summary>
    public bool EnablePermissionsPolicy { get; set; } = false;

    /// <summary>
    /// Permissions-Policy值
    /// </summary>
    public string? PermissionsPolicyValue { get; set; }

    /// <summary>
    /// 移除Server头
    /// </summary>
    public bool RemoveServerHeader { get; set; } = true;
}
