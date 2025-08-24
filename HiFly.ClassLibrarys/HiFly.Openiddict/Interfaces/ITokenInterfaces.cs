// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.Extensions.Caching.Distributed;

namespace HiFly.Openiddict.Interfaces;

/// <summary>
/// Token验证器接口
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// 验证Token
    /// </summary>
    /// <param name="token">要验证的Token</param>
    /// <returns>验证结果</returns>
    Task<TokenValidationResult> ValidateAsync(string token);
}

/// <summary>
/// Token验证结果
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// 验证是否成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 用户声明
    /// </summary>
    public Dictionary<string, object> Claims { get; set; } = new();
}

/// <summary>
/// Token缓存选项
/// </summary>
public class TokenCacheOptions
{
    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "hifly:token:";

    /// <summary>
    /// 默认过期时间（分钟）
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Redis连接字符串
    /// </summary>
    public string? RedisConnectionString { get; set; }
}

/// <summary>
/// Token缓存服务接口
/// </summary>
public interface ITokenCacheService
{
    /// <summary>
    /// 获取缓存的Token
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>Token值</returns>
    Task<string?> GetTokenAsync(string key);

    /// <summary>
    /// 设置Token缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="token">Token值</param>
    /// <param name="expiration">过期时间</param>
    /// <returns>操作结果</returns>
    Task SetTokenAsync(string key, string token, TimeSpan expiration);

    /// <summary>
    /// 移除Token缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>操作结果</returns>
    Task RemoveTokenAsync(string key);
}

/// <summary>
/// 分布式Token缓存服务实现
/// </summary>
public class DistributedTokenCacheService : ITokenCacheService
{
    private readonly IDistributedCache _cache;
    private readonly TokenCacheOptions _options;

    public DistributedTokenCacheService(
        IDistributedCache cache,
        Microsoft.Extensions.Options.IOptions<TokenCacheOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<string?> GetTokenAsync(string key)
    {
        return await _cache.GetStringAsync(_options.KeyPrefix + key);
    }

    public async Task SetTokenAsync(string key, string token, TimeSpan expiration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        };

        await _cache.SetStringAsync(_options.KeyPrefix + key, token, options);
    }

    public async Task RemoveTokenAsync(string key)
    {
        await _cache.RemoveAsync(_options.KeyPrefix + key);
    }
}

/// <summary>
/// SSO中间件选项
/// </summary>
public class SsoMiddlewareOptions
{
    /// <summary>
    /// 启用自动Token刷新
    /// </summary>
    public bool EnableAutoTokenRefresh { get; set; } = true;

    /// <summary>
    /// 启用会话同步
    /// </summary>
    public bool EnableSessionSync { get; set; } = true;

    /// <summary>
    /// 启用安全头
    /// </summary>
    public bool EnableSecurityHeaders { get; set; } = true;

    /// <summary>
    /// 启用审计日志
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;
}
