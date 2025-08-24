// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HiFly.Openiddict.Middleware;

/// <summary>
/// 自动Token刷新中间件，在每个请求中检查并自动刷新即将过期的访问令牌
/// </summary>
public class AutoTokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AutoTokenRefreshMiddleware> _logger;
    private readonly AutoTokenRefreshOptions _options;

    public AutoTokenRefreshMiddleware(
        RequestDelegate next,
        ILogger<AutoTokenRefreshMiddleware> logger,
        IOptions<AutoTokenRefreshOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, ITokenManagementService tokenManagementService)
    {
        try
        {
            // 检查是否需要跳过令牌刷新
            if (ShouldSkipTokenRefresh(context))
            {
                await _next(context);
                return;
            }

            // 检查用户是否已认证
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // 检查令牌是否即将过期
                var isExpiringSoon = await tokenManagementService.IsAccessTokenExpiringSoonAsync(
                    context, _options.RefreshThresholdMinutes);

                if (isExpiringSoon)
                {
                    _logger.LogInformation("访问令牌即将过期，开始自动刷新");

                    var refreshSuccessful = await tokenManagementService.RefreshAccessTokenAsync(context);

                    if (refreshSuccessful)
                    {
                        _logger.LogInformation("访问令牌自动刷新成功");
                        
                        // 添加刷新成功的响应头（可选）
                        if (_options.AddRefreshHeader)
                        {
                            context.Response.Headers.Append("X-Token-Refreshed", "true");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("访问令牌自动刷新失败");

                        // 根据配置决定是否重定向到登录页面
                        if (_options.RedirectToLoginOnRefreshFailure)
                        {
                            await HandleRefreshFailure(context);
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自动令牌刷新中间件发生错误");
            
            // 发生异常时继续执行下一个中间件
            await _next(context);
        }
    }

    /// <summary>
    /// 判断是否应该跳过令牌刷新
    /// </summary>
    private bool ShouldSkipTokenRefresh(HttpContext context)
    {
        // 跳过静态文件请求
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (path != null)
        {
            foreach (var excludePath in _options.ExcludePaths)
            {
                if (path.StartsWith(excludePath.ToLowerInvariant()))
                    return true;
            }

            foreach (var excludeExtension in _options.ExcludeExtensions)
            {
                if (path.EndsWith(excludeExtension.ToLowerInvariant()))
                    return true;
            }
        }

        // 跳过AJAX请求（可选）
        if (_options.SkipAjaxRequests)
        {
            var requestedWith = context.Request.Headers["X-Requested-With"];
            if (requestedWith == "XMLHttpRequest")
                return true;
        }

        // 跳过API请求（可选）
        if (_options.SkipApiRequests && path != null && path.StartsWith("/api/"))
            return true;

        return false;
    }

    /// <summary>
    /// 处理刷新令牌失败的情况
    /// </summary>
    private async Task HandleRefreshFailure(HttpContext context)
    {
        try
        {
            // 清除无效的令牌
            var tokenManagementService = context.RequestServices.GetRequiredService<ITokenManagementService>();
            await tokenManagementService.ClearTokensAsync(context);

            // 重定向到登录页面
            var loginUrl = _options.LoginPath ?? "/signin";
            var returnUrl = context.Request.Path + context.Request.QueryString;
            var redirectUrl = $"{loginUrl}?returnUrl={Uri.EscapeDataString(returnUrl)}";

            context.Response.Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理令牌刷新失败时发生错误");
        }
    }
}

/// <summary>
/// 自动Token刷新中间件配置选项
/// </summary>
public class AutoTokenRefreshOptions
{
    /// <summary>
    /// 刷新阈值（分钟），当令牌剩余有效时间小于此值时触发刷新
    /// 默认值：5分钟
    /// </summary>
    public int RefreshThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// 刷新失败时是否重定向到登录页面
    /// 默认值：true
    /// </summary>
    public bool RedirectToLoginOnRefreshFailure { get; set; } = true;

    /// <summary>
    /// 登录页面路径
    /// 默认值："/signin"
    /// </summary>
    public string LoginPath { get; set; } = "/signin";

    /// <summary>
    /// 是否在响应头中添加令牌刷新标识
    /// 默认值：false
    /// </summary>
    public bool AddRefreshHeader { get; set; } = false;

    /// <summary>
    /// 是否跳过AJAX请求的令牌刷新
    /// 默认值：false
    /// </summary>
    public bool SkipAjaxRequests { get; set; } = false;

    /// <summary>
    /// 是否跳过API请求的令牌刷新
    /// 默认值：false
    /// </summary>
    public bool SkipApiRequests { get; set; } = false;

    /// <summary>
    /// 排除的路径列表，这些路径不会触发令牌刷新
    /// </summary>
    public List<string> ExcludePaths { get; set; } = new()
    {
        "/css/",
        "/js/",
        "/images/",
        "/lib/",
        "/favicon.ico",
        "/signin",
        "/signout",
        "/connect/"
    };

    /// <summary>
    /// 排除的文件扩展名列表，这些文件不会触发令牌刷新
    /// </summary>
    public List<string> ExcludeExtensions { get; set; } = new()
    {
        ".css",
        ".js",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".ico",
        ".svg",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot"
    };
}
