// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace HiFly.Openiddict.Services;

/// <summary>
/// Token管理服务实现，提供统一的Token操作功能
/// </summary>
public class TokenManagementService : ITokenManagementService
{
    private readonly ILogger<TokenManagementService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public TokenManagementService(
        ILogger<TokenManagementService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync(HttpContext context)
    {
        try
        {
            return await context.GetTokenAsync("access_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取访问令牌时发生错误");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetRefreshTokenAsync(HttpContext context)
    {
        try
        {
            return await context.GetTokenAsync("refresh_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取刷新令牌时发生错误");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetIdTokenAsync(HttpContext context)
    {
        try
        {
            return await context.GetTokenAsync("id_token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取ID令牌时发生错误");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAccessTokenExpiringSoonAsync(HttpContext context, int threshold = 5)
    {
        try
        {
            // 首先检查用户是否已认证
            if (context?.User?.Identity?.IsAuthenticated != true)
            {
                return false; // 用户未认证，不需要刷新
            }

            var expiresAt = await GetTokenExpirationAsync(context);
            if (expiresAt == null)
            {
                // 如果无法获取过期时间，检查是否有访问令牌
                var accessToken = await GetAccessTokenAsync(context);
                if (string.IsNullOrEmpty(accessToken))
                {
                    return false; // 没有令牌，不需要刷新
                }
                
                // 有令牌但无过期时间，假设需要刷新
                _logger.LogWarning("无法获取令牌过期时间，但存在访问令牌，假设需要刷新");
                return true;
            }

            var timeUntilExpiry = expiresAt.Value - DateTimeOffset.UtcNow;
            var result = timeUntilExpiry.TotalMinutes <= threshold;
            
            _logger.LogDebug("Token过期检查: 剩余时间 {RemainingMinutes} 分钟，阈值 {Threshold} 分钟，需要刷新: {NeedsRefresh}", 
                timeUntilExpiry.TotalMinutes, threshold, result);
                
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查令牌过期时间时发生错误");
            return false; // 出错时不要刷新，避免循环
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshAccessTokenAsync(HttpContext context)
    {
        try
        {
            var refreshToken = await GetRefreshTokenAsync(context);
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("没有可用的刷新令牌");
                return false;
            }

            // 获取OpenID Connect配置
            var scheme = OpenIdConnectDefaults.AuthenticationScheme;
            var authResult = await context.AuthenticateAsync(scheme);
            if (authResult?.Properties == null)
            {
                _logger.LogWarning("无法获取认证属性");
                return false;
            }

            // 从认证属性中获取必要的配置信息
            string? authority = null, clientId = null, clientSecret = null;
            authResult.Properties.Items.TryGetValue(".Token.authority", out authority);
            authResult.Properties.Items.TryGetValue(".Token.client_id", out clientId);
            authResult.Properties.Items.TryGetValue(".Token.client_secret", out clientSecret);

            if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(clientId))
            {
                _logger.LogWarning("缺少必要的令牌刷新配置信息");
                return false;
            }

            // 发送令牌刷新请求
            var httpClient = _httpClientFactory.CreateClient();
            var tokenEndpoint = $"{authority.TrimEnd('/')}/connect/token";

            var requestData = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "refresh_token"),
                new("refresh_token", refreshToken),
                new("client_id", clientId)
            };

            if (!string.IsNullOrEmpty(clientSecret))
            {
                requestData.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            }

            var response = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("令牌刷新请求失败: {StatusCode}", response.StatusCode);
                return false;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (!tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
            {
                _logger.LogWarning("刷新响应中缺少访问令牌");
                return false;
            }

            // 更新令牌
            var newAccessToken = accessTokenElement.GetString();
            var newRefreshToken = tokenResponse.TryGetProperty("refresh_token", out var refreshTokenElement) 
                ? refreshTokenElement.GetString() 
                : refreshToken; // 如果没有返回新的刷新令牌，继续使用旧的

            // 计算新的过期时间
            var expiresIn = tokenResponse.TryGetProperty("expires_in", out var expiresInElement) 
                ? expiresInElement.GetInt32() 
                : 3600; // 默认1小时

            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

            // 更新认证属性中的令牌
            var newProperties = new AuthenticationProperties(authResult.Properties.Items)
            {
                ExpiresUtc = expiresAt
            };

            newProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = newAccessToken },
                new AuthenticationToken { Name = "refresh_token", Value = newRefreshToken },
                new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o") }
            });

            // 重新登录用户以更新令牌
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User, newProperties);

            _logger.LogInformation("成功刷新访问令牌");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新访问令牌时发生错误");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return false;

            // 简单的JWT格式验证
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return false;

            var jwtToken = handler.ReadJwtToken(token);
            
            // 检查令牌是否过期
            if (jwtToken.ValidTo < DateTime.UtcNow)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证令牌时发生错误");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<TimeSpan?> GetTokenRemainingLifetimeAsync(HttpContext context)
    {
        try
        {
            var expiresAt = await GetTokenExpirationAsync(context);
            if (expiresAt == null)
                return null;

            var remaining = expiresAt.Value - DateTimeOffset.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取令牌剩余生命周期时发生错误");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task ClearTokensAsync(HttpContext context)
    {
        try
        {
            // 清除Cookie认证
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("已清除用户令牌");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除令牌时发生错误");
        }
    }

    /// <inheritdoc/>
    public async Task<DateTimeOffset?> GetTokenExpirationAsync(HttpContext context)
    {
        try
        {
            // 确保上下文和用户有效
            if (context?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var expiresAtString = await context.GetTokenAsync("expires_at");
            if (string.IsNullOrEmpty(expiresAtString))
            {
                _logger.LogDebug("未找到 expires_at 令牌属性");
                return null;
            }

            if (DateTimeOffset.TryParse(expiresAtString, out var expiresAt))
            {
                _logger.LogDebug("令牌过期时间: {ExpiresAt}", expiresAt);
                return expiresAt;
            }

            _logger.LogWarning("无法解析令牌过期时间: {ExpiresAtString}", expiresAtString);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取令牌过期时间时发生错误");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SaveTokensAsync(HttpContext context, AuthenticationProperties properties)
    {
        try
        {
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User, properties);
            _logger.LogDebug("令牌已保存到认证属性");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存令牌时发生错误");
        }
    }
}
