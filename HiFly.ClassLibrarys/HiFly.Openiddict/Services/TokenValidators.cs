// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

// 使用别名解决命名冲突
using HiFlyTokenValidationResult = HiFly.Openiddict.Interfaces.TokenValidationResult;
using MSTokenValidationResult = Microsoft.IdentityModel.Tokens.TokenValidationResult;

namespace HiFly.Openiddict.Services;

/// <summary>
/// JWT Token验证器实现
/// </summary>
public class JwtTokenValidator : ITokenValidator
{
    private readonly ILogger<JwtTokenValidator> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenValidator(
        ILogger<JwtTokenValidator> logger,
        TokenValidationParameters validationParameters)
    {
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
        _validationParameters = validationParameters;
    }

    /// <inheritdoc/>
    public async Task<HiFlyTokenValidationResult> ValidateAsync(string token)
    {
        var result = new HiFlyTokenValidationResult();

        try
        {
            if (string.IsNullOrEmpty(token))
            {
                result.IsValid = false;
                result.ErrorMessage = "Token不能为空";
                return result;
            }

            // 检查Token格式
            if (!_tokenHandler.CanReadToken(token))
            {
                result.IsValid = false;
                result.ErrorMessage = "Token格式无效";
                return result;
            }

            // 验证Token
            var validationResult = await _tokenHandler.ValidateTokenAsync(token, _validationParameters);
            
            if (validationResult.IsValid)
            {
                result.IsValid = true;
                
                // 提取声明
                if (validationResult.ClaimsIdentity != null)
                {
                    foreach (var claim in validationResult.ClaimsIdentity.Claims)
                    {
                        result.Claims[claim.Type] = claim.Value;
                    }
                }

                // 获取过期时间
                var jwtToken = _tokenHandler.ReadJwtToken(token);
                result.ExpiresAt = jwtToken.ValidTo;
            }
            else
            {
                result.IsValid = false;
                result.ErrorMessage = validationResult.Exception?.Message ?? "Token验证失败";
            }
        }
        catch (SecurityTokenExpiredException ex)
        {
            result.IsValid = false;
            result.ErrorMessage = "Token已过期";
            _logger.LogWarning(ex, "Token验证失败: Token已过期");
        }
        catch (SecurityTokenException ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Token安全验证失败: {ex.Message}";
            _logger.LogWarning(ex, "Token验证失败: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Token验证过程中发生错误: {ex.Message}";
            _logger.LogError(ex, "Token验证过程中发生未知错误");
        }

        return result;
    }
}

/// <summary>
/// 自定义Token验证器，支持多种验证策略
/// </summary>
public class CustomTokenValidator : ITokenValidator
{
    private readonly ILogger<CustomTokenValidator> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _introspectionEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public CustomTokenValidator(
        ILogger<CustomTokenValidator> logger,
        IHttpClientFactory httpClientFactory,
        string introspectionEndpoint,
        string clientId,
        string clientSecret)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _introspectionEndpoint = introspectionEndpoint;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /// <inheritdoc/>
    public async Task<HiFlyTokenValidationResult> ValidateAsync(string token)
    {
        var result = new HiFlyTokenValidationResult();

        try
        {
            if (string.IsNullOrEmpty(token))
            {
                result.IsValid = false;
                result.ErrorMessage = "Token不能为空";
                return result;
            }

            // 使用内省端点验证Token
            var isValid = await IntrospectTokenAsync(token);
            
            if (isValid)
            {
                result.IsValid = true;
                
                // 如果是JWT格式，提取声明
                if (IsJwtFormat(token))
                {
                    ExtractClaimsFromJwt(token, result);
                }
            }
            else
            {
                result.IsValid = false;
                result.ErrorMessage = "Token验证失败或已失效";
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Token验证过程中发生错误: {ex.Message}";
            _logger.LogError(ex, "自定义Token验证失败");
        }

        return result;
    }

    private async Task<bool> IntrospectTokenAsync(string token)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            var requestData = new List<KeyValuePair<string, string>>
            {
                new("token", token),
                new("client_id", _clientId),
                new("client_secret", _clientSecret)
            };

            var response = await httpClient.PostAsync(_introspectionEndpoint, 
                new FormUrlEncodedContent(requestData));

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var introspectionResult = JsonSerializer.Deserialize<JsonElement>(content);
                
                return introspectionResult.TryGetProperty("active", out var activeProperty) 
                    && activeProperty.GetBoolean();
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token内省验证失败");
            return false;
        }
    }

    private bool IsJwtFormat(string token)
    {
        return token.Split('.').Length == 3;
    }

    private void ExtractClaimsFromJwt(string token, HiFlyTokenValidationResult result)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            foreach (var claim in jwtToken.Claims)
            {
                result.Claims[claim.Type] = claim.Value;
            }
            
            result.ExpiresAt = jwtToken.ValidTo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "提取JWT声明时发生错误");
        }
    }
}

/// <summary>
/// 缓存Token验证器，提高验证性能
/// </summary>
public class CachedTokenValidator : ITokenValidator
{
    private readonly ITokenValidator _innerValidator;
    private readonly ITokenCacheService _cacheService;
    private readonly ILogger<CachedTokenValidator> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public CachedTokenValidator(
        ITokenValidator innerValidator,
        ITokenCacheService cacheService,
        ILogger<CachedTokenValidator> logger)
    {
        _innerValidator = innerValidator;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<HiFlyTokenValidationResult> ValidateAsync(string token)
    {
        try
        {
            var cacheKey = $"token_validation:{ComputeTokenHash(token)}";
            
            // 尝试从缓存获取验证结果
            var cachedResult = await GetCachedValidationResultAsync(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("从缓存获取Token验证结果");
                return cachedResult;
            }

            // 执行实际验证
            var result = await _innerValidator.ValidateAsync(token);
            
            // 只缓存有效的验证结果
            if (result.IsValid)
            {
                await CacheValidationResultAsync(cacheKey, result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存Token验证失败");
            
            // 缓存失败时直接使用内部验证器
            return await _innerValidator.ValidateAsync(token);
        }
    }

    private string ComputeTokenHash(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes)[..16]; // 使用前16个字符作为缓存键
    }

    private async Task<HiFlyTokenValidationResult?> GetCachedValidationResultAsync(string cacheKey)
    {
        try
        {
            var cachedData = await _cacheService.GetTokenAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<HiFlyTokenValidationResult>(cachedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取缓存验证结果失败");
        }
        
        return null;
    }

    private async Task CacheValidationResultAsync(string cacheKey, HiFlyTokenValidationResult result)
    {
        try
        {
            var serializedResult = JsonSerializer.Serialize(result);
            await _cacheService.SetTokenAsync(cacheKey, serializedResult, _cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "缓存验证结果失败");
        }
    }
}
