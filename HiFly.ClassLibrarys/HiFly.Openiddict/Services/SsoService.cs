// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace HiFly.Openiddict.Services;

/// <summary>
/// SSO服务实现，提供跨应用的统一身份验证功能
/// </summary>
public class SsoService : ISsoService
{
    private readonly ILogger<SsoService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITokenManagementService _tokenManagementService;
    private readonly HiFlyOpenIdClientOptions _clientOptions;

    public SsoService(
        ILogger<SsoService> logger,
        IHttpClientFactory httpClientFactory,
        ITokenManagementService tokenManagementService,
        IOptions<HiFlyOpenIdClientOptions> clientOptions)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _tokenManagementService = tokenManagementService;
        _clientOptions = clientOptions.Value;
    }

    /// <inheritdoc/>
    public async Task<bool> CheckSsoSessionAsync(HttpContext context)
    {
        try
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
                return false;

            // 检查本地会话有效性
            var accessToken = await _tokenManagementService.GetAccessTokenAsync(context);
            if (string.IsNullOrEmpty(accessToken))
                return false;

            // 检查令牌是否有效
            var isTokenValid = await _tokenManagementService.ValidateTokenAsync(accessToken);
            if (!isTokenValid)
            {
                // 尝试刷新令牌
                var refreshed = await _tokenManagementService.RefreshAccessTokenAsync(context);
                if (!refreshed)
                    return false;
            }

            // 远程会话检查（可选）
            var sessionValid = await CheckRemoteSessionAsync(context);
            return sessionValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查SSO会话时发生错误");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string> InitiateSsoLoginAsync(HttpContext context, string returnUrl = "/", string? clientId = null)
    {
        try
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = returnUrl,
                IsPersistent = true
            };

            // 添加PKCE支持
            properties.SetParameter("code_challenge_method", "S256");
            
            // 保存状态信息
            properties.Items["returnUrl"] = returnUrl;
            properties.Items["clientId"] = clientId ?? _clientOptions.ClientId;

            // 构建授权URL
            var authUrl = $"{_clientOptions.Authority.TrimEnd('/')}/connect/authorize?" +
                         $"client_id={Uri.EscapeDataString(_clientOptions.ClientId)}&" +
                         $"redirect_uri={Uri.EscapeDataString(GetRedirectUri(context))}&" +
                         $"response_type=code&" +
                         $"scope={Uri.EscapeDataString(string.Join(" ", _clientOptions.Scopes))}&" +
                         $"state={Uri.EscapeDataString(Guid.NewGuid().ToString())}&" +
                         $"nonce={Uri.EscapeDataString(Guid.NewGuid().ToString())}";

            _logger.LogInformation("启动SSO登录流程，重定向到: {AuthUrl}", authUrl);
            return authUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动SSO登录时发生错误");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HandleSsoCallbackAsync(HttpContext context)
    {
        try
        {
            var result = await context.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                _logger.LogWarning("SSO回调认证失败");
                return false;
            }

            // 验证状态参数
            var state = context.Request.Query["state"].FirstOrDefault();
            if (string.IsNullOrEmpty(state))
            {
                _logger.LogWarning("SSO回调缺少状态参数");
                return false;
            }

            // 处理认证结果
            var sessionInfo = await CreateSessionInfoFromAuthResult(result);
            if (sessionInfo == null)
            {
                _logger.LogWarning("无法创建会话信息");
                return false;
            }

            _logger.LogInformation("SSO登录回调处理成功，用户: {UserId}", sessionInfo.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理SSO回调时发生错误");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string> InitiateSsoLogoutAsync(HttpContext context, string? postLogoutRedirectUri = null)
    {
        try
        {
            var idToken = await _tokenManagementService.GetIdTokenAsync(context);
            
            var logoutUrl = $"{_clientOptions.Authority.TrimEnd('/')}/connect/logout?" +
                           $"id_token_hint={Uri.EscapeDataString(idToken ?? "")}&" +
                           $"post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri ?? GetDefaultLogoutRedirectUri(context))}";

            _logger.LogInformation("启动SSO登出流程，重定向到: {LogoutUrl}", logoutUrl);
            return logoutUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动SSO登出时发生错误");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HandleSsoLogoutCallbackAsync(HttpContext context)
    {
        try
        {
            // 清除本地令牌
            await _tokenManagementService.ClearTokensAsync(context);
            
            _logger.LogInformation("SSO登出回调处理成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理SSO登出回调时发生错误");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<SsoSessionInfo?> GetSsoSessionInfoAsync(HttpContext context)
    {
        try
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
                return null;

            var sessionInfo = new SsoSessionInfo
            {
                SessionId = context.User.FindFirst("sid")?.Value ?? "",
                UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "",
                UserName = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "",
                AuthTime = GetAuthTime(context),
                ExpiresAt = await _tokenManagementService.GetTokenExpirationAsync(context),
                AuthenticationMethod = context.User.FindFirst("amr")?.Value ?? "",
                IdentityProvider = context.User.FindFirst("idp")?.Value ?? "",
                IsActive = true
            };

            return sessionInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取SSO会话信息时发生错误");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RequiresReauthenticationAsync(HttpContext context, int? maxAge = null)
    {
        try
        {
            if (maxAge == null)
                return false;

            var authTime = GetAuthTime(context);
            var timeSinceAuth = DateTimeOffset.UtcNow - authTime;
            
            return timeSinceAuth.TotalSeconds > maxAge.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查重新认证需求时发生错误");
            return true; // 出错时假设需要重新认证
        }
    }

    /// <inheritdoc/>
    public async Task<SsoMetadata?> GetSsoMetadataAsync(string authority)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var metadataUrl = $"{authority.TrimEnd('/')}/.well-known/openid_configuration";
            
            var response = await httpClient.GetAsync(metadataUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("无法获取SSO元数据，状态码: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var metadata = JsonSerializer.Deserialize<JsonElement>(content);

            return new SsoMetadata
            {
                Issuer = metadata.GetProperty("issuer").GetString() ?? "",
                AuthorizationEndpoint = metadata.GetProperty("authorization_endpoint").GetString() ?? "",
                TokenEndpoint = metadata.GetProperty("token_endpoint").GetString() ?? "",
                UserInfoEndpoint = metadata.GetProperty("userinfo_endpoint").GetString() ?? "",
                EndSessionEndpoint = metadata.TryGetProperty("end_session_endpoint", out var endSession) ? endSession.GetString() ?? "" : "",
                CheckSessionEndpoint = metadata.TryGetProperty("check_session_iframe", out var checkSession) ? checkSession.GetString() ?? "" : ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取SSO元数据时发生错误");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateSessionAsync(HttpContext context)
    {
        try
        {
            return await CheckSsoSessionAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证会话时发生错误");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshSsoSessionAsync(HttpContext context)
    {
        try
        {
            return await _tokenManagementService.RefreshAccessTokenAsync(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新SSO会话时发生错误");
            return false;
        }
    }

    #region 私有辅助方法

    private async Task<bool> CheckRemoteSessionAsync(HttpContext context)
    {
        try
        {
            var metadata = await GetSsoMetadataAsync(_clientOptions.Authority);
            if (metadata == null || string.IsNullOrEmpty(metadata.CheckSessionEndpoint))
                return true; // 如果没有检查端点，假设会话有效

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(metadata.CheckSessionEndpoint);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查远程会话时发生错误");
            return true; // 网络错误时假设会话有效
        }
    }

    private async Task<SsoSessionInfo?> CreateSessionInfoFromAuthResult(AuthenticateResult result)
    {
        try
        {
            if (result.Principal == null)
                return null;

            return new SsoSessionInfo
            {
                SessionId = result.Principal.FindFirst("sid")?.Value ?? Guid.NewGuid().ToString(),
                UserId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "",
                UserName = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? "",
                AuthTime = DateTimeOffset.UtcNow,
                AuthenticationMethod = "oidc",
                IdentityProvider = _clientOptions.Authority,
                IsActive = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从认证结果创建会话信息时发生错误");
            return null;
        }
    }

    private DateTimeOffset GetAuthTime(HttpContext context)
    {
        var authTimeClaim = context.User.FindFirst("auth_time")?.Value;
        if (long.TryParse(authTimeClaim, out var unixTime))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }
        return DateTimeOffset.UtcNow;
    }

    private string GetRedirectUri(HttpContext context)
    {
        return $"{context.Request.Scheme}://{context.Request.Host}/signin-oidc";
    }

    private string GetDefaultLogoutRedirectUri(HttpContext context)
    {
        return $"{context.Request.Scheme}://{context.Request.Host}/";
    }

    #endregion
}
