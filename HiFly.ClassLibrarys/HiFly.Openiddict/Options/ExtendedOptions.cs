// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.Options;

/// <summary>
/// SSO (单点登录) 配置选项
/// </summary>
public class SsoOptions
{
    /// <summary>
    /// 是否启用SSO功能
    /// 默认值：true
    /// </summary>
    public bool EnableSso { get; set; } = true;

    /// <summary>
    /// SSO会话检查间隔（秒）
    /// 默认值：60秒
    /// </summary>
    public int SessionCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 是否启用会话同步
    /// 默认值：true
    /// </summary>
    public bool EnableSessionSync { get; set; } = true;

    /// <summary>
    /// 会话超时时间（分钟）
    /// 默认值：480分钟（8小时）
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 480;

    /// <summary>
    /// 是否启用跨域SSO
    /// 默认值：true
    /// </summary>
    public bool EnableCrossDomainSso { get; set; } = true;

    /// <summary>
    /// 允许的跨域来源列表
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new();

    /// <summary>
    /// SSO登出时是否执行前端通道登出
    /// 默认值：true
    /// </summary>
    public bool EnableFrontChannelLogout { get; set; } = true;

    /// <summary>
    /// SSO登出时是否执行后端通道登出
    /// 默认值：false
    /// </summary>
    public bool EnableBackChannelLogout { get; set; } = false;

    /// <summary>
    /// 后端通道登出端点URL
    /// </summary>
    public string? BackChannelLogoutUri { get; set; }

    /// <summary>
    /// 前端通道登出端点URL
    /// </summary>
    public string? FrontChannelLogoutUri { get; set; }

    /// <summary>
    /// 是否在前端通道登出时要求会话ID
    /// 默认值：true
    /// </summary>
    public bool FrontChannelLogoutSessionRequired { get; set; } = true;

    /// <summary>
    /// SSO域名（用于跨域Cookie）
    /// </summary>
    public string? SsoDomain { get; set; }

    /// <summary>
    /// 是否启用记住我功能
    /// 默认值：true
    /// </summary>
    public bool EnableRememberMe { get; set; } = true;

    /// <summary>
    /// 记住我的有效期（天）
    /// 默认值：30天
    /// </summary>
    public int RememberMeDays { get; set; } = 30;
}

/// <summary>
/// Token管理配置选项
/// </summary>
public class TokenManagementOptions
{
    /// <summary>
    /// 是否启用Token管理功能
    /// 默认值：true
    /// </summary>
    public bool EnableTokenManagement { get; set; } = true;

    /// <summary>
    /// 访问令牌有效期（分钟）
    /// 默认值：60分钟
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// 刷新令牌有效期（天）
    /// 默认值：30天
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 30;

    /// <summary>
    /// ID令牌有效期（分钟）
    /// 默认值：15分钟
    /// </summary>
    public int IdTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>
    /// Token刷新阈值（分钟）
    /// 当令牌剩余有效时间小于此值时触发自动刷新
    /// 默认值：5分钟
    /// </summary>
    public int RefreshThresholdMinutes { get; set; } = 5;

    /// <summary>
    /// 是否启用Token缓存
    /// 默认值：true
    /// </summary>
    public bool EnableTokenCache { get; set; } = true;

    /// <summary>
    /// Token缓存有效期（分钟）
    /// 默认值：5分钟
    /// </summary>
    public int TokenCacheLifetimeMinutes { get; set; } = 5;

    /// <summary>
    /// 是否启用Token加密存储
    /// 默认值：true
    /// </summary>
    public bool EnableTokenEncryption { get; set; } = true;

    /// <summary>
    /// 是否允许不安全的HTTP传输（仅用于开发环境）
    /// 默认值：false
    /// </summary>
    public bool AllowInsecureHttp { get; set; } = false;

    /// <summary>
    /// Token存储提供程序类型
    /// </summary>
    public TokenStorageProvider StorageProvider { get; set; } = TokenStorageProvider.Cookie;

    /// <summary>
    /// 自定义Token验证器类型
    /// </summary>
    public Type? CustomTokenValidator { get; set; }
}

/// <summary>
/// Token存储提供程序枚举
/// </summary>
public enum TokenStorageProvider
{
    /// <summary>
    /// Cookie存储
    /// </summary>
    Cookie,

    /// <summary>
    /// 内存缓存存储
    /// </summary>
    MemoryCache,

    /// <summary>
    /// 分布式缓存存储
    /// </summary>
    DistributedCache,

    /// <summary>
    /// 数据库存储
    /// </summary>
    Database,

    /// <summary>
    /// 自定义存储
    /// </summary>
    Custom
}

/// <summary>
/// 安全配置选项
/// </summary>
public class SecurityOptions
{
    /// <summary>
    /// 是否启用PKCE（授权码交换的校验密钥）
    /// 默认值：true
    /// </summary>
    public bool EnablePkce { get; set; } = true;

    /// <summary>
    /// PKCE挑战方法
    /// 默认值：S256
    /// </summary>
    public string PkceChallengeMethod { get; set; } = "S256";

    /// <summary>
    /// 是否启用状态参数验证
    /// 默认值：true
    /// </summary>
    public bool EnableStateValidation { get; set; } = true;

    /// <summary>
    /// 是否启用随机数验证
    /// 默认值：true
    /// </summary>
    public bool EnableNonceValidation { get; set; } = true;

    /// <summary>
    /// 是否要求HTTPS
    /// 默认值：true
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// 允许的重定向URI模式列表
    /// </summary>
    public List<string> AllowedRedirectUriPatterns { get; set; } = new();

    /// <summary>
    /// 是否启用CORS支持
    /// 默认值：true
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// CORS策略名称
    /// </summary>
    public string CorsPolicyName { get; set; } = "HiFlyOpenIdCorsPolicy";

    /// <summary>
    /// 是否启用CSP（内容安全策略）
    /// 默认值：false
    /// </summary>
    public bool EnableContentSecurityPolicy { get; set; } = false;

    /// <summary>
    /// CSP策略配置
    /// </summary>
    public string? ContentSecurityPolicy { get; set; }

    /// <summary>
    /// 最大认证年龄（秒）
    /// 超过此时间需要重新认证
    /// </summary>
    public int? MaxAuthenticationAge { get; set; }

    /// <summary>
    /// 登录提示类型
    /// </summary>
    public string LoginPrompt { get; set; } = "select_account";
}

/// <summary>
/// 审计日志配置选项
/// </summary>
public class AuditOptions
{
    /// <summary>
    /// 是否启用审计日志
    /// 默认值：true
    /// </summary>
    public bool EnableAudit { get; set; } = true;

    /// <summary>
    /// 是否记录成功的认证事件
    /// 默认值：true
    /// </summary>
    public bool LogSuccessfulAuthentication { get; set; } = true;

    /// <summary>
    /// 是否记录失败的认证事件
    /// 默认值：true
    /// </summary>
    public bool LogFailedAuthentication { get; set; } = true;

    /// <summary>
    /// 是否记录Token操作事件
    /// 默认值：true
    /// </summary>
    public bool LogTokenOperations { get; set; } = true;

    /// <summary>
    /// 是否记录用户操作事件
    /// 默认值：false
    /// </summary>
    public bool LogUserOperations { get; set; } = false;

    /// <summary>
    /// 审计日志保留天数
    /// 默认值：90天
    /// </summary>
    public int LogRetentionDays { get; set; } = 90;

    /// <summary>
    /// 审计日志存储提供程序
    /// </summary>
    public AuditStorageProvider StorageProvider { get; set; } = AuditStorageProvider.Database;

    /// <summary>
    /// 是否启用敏感数据日志记录
    /// 默认值：false（生产环境不建议启用）
    /// </summary>
    public bool LogSensitiveData { get; set; } = false;
}

/// <summary>
/// 审计日志存储提供程序枚举
/// </summary>
public enum AuditStorageProvider
{
    /// <summary>
    /// 数据库存储
    /// </summary>
    Database,

    /// <summary>
    /// 文件存储
    /// </summary>
    File,

    /// <summary>
    /// Serilog存储
    /// </summary>
    Serilog,

    /// <summary>
    /// 自定义存储
    /// </summary>
    Custom
}
