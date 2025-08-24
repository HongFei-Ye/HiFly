# HiFly.Openiddict 企业级 SSO 解决方案

<div align="center">

![HiFly Logo](https://img.shields.io/badge/HiFly-Openiddict-blue?style=for-the-badge)
![.NET 9](https://img.shields.io/badge/.NET-9.0-purple?style=for-the-badge&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-Server-orange?style=for-the-badge&logo=blazor)
![OpenIddict](https://img.shields.io/badge/OpenIddict-5.8.0-green?style=for-the-badge)
![License](https://img.shields.io/badge/License-弘飞帮联科技-red?style=for-the-badge)

**企业级单点登录解决方案，基于 OpenIddict 和 Blazor 构建**

[快速开始](#快速开始) • [文档](#详细文档) • [示例](#示例项目) • [API 参考](#api-参考) • [支持](#技术支持)

</div>

---

## 🎯 项目概述

HiFly.Openiddict 是一个功能完整的企业级 SSO（单点登录）解决方案，基于 OpenIddict 框架构建，专为 .NET 9 和 Blazor 应用程序设计。提供开箱即用的身份验证、授权、Token 管理和跨应用会话同步功能。

### ✨ 核心特性

| 特性 | 描述 | 状态 |
|------|------|------|
| 🔐 **统一身份认证** | 基于 OpenID Connect & OAuth 2.0 标准 | ✅ 已完成 |
| 🔄 **Token 自动管理** | 智能刷新访问令牌，无缝用户体验 | ✅ 已完成 |
| 🌐 **跨应用 SSO** | 支持多应用单点登录和登出 | ✅ 已完成 |
| 🛡️ **安全增强** | PKCE、状态验证、会话保护等安全机制 | ✅ 已完成 |
| 🎨 **Blazor 集成** | 开箱即用的 UI 组件和服务 | ✅ 已完成 |
| ⚡ **高性能** | 分布式缓存、连接池、异步处理 | ✅ 已完成 |
| 📊 **监控审计** | 完整的日志记录和审计追踪 | ✅ 已完成 |
| 🔧 **易于扩展** | 插件化架构，支持自定义扩展 | ✅ 已完成 |

---

## 🚀 快速开始

### 📋 系统要求

- **.NET 9.0** 或更高版本
- **SQL Server** 2019+ 或 **PostgreSQL** 12+
- **Redis** 6.0+（可选，用于分布式缓存）
- **Visual Studio 2022** 17.8+ 或 **JetBrains Rider** 2023.3+

### 📦 安装

#### 1. NuGet 包管理器
```powershell
Install-Package HiFly.Openiddict
```

#### 2. .NET CLI
```bash
dotnet add package HiFly.Openiddict
```

#### 3. PackageReference
```xml
<PackageReference Include="HiFly.Openiddict" Version="1.0.0" />
```

### ⚙️ 配置步骤

#### 步骤 1: 认证服务器配置

创建一个新的 Blazor Server 项目作为认证服务器：

```csharp
// Program.cs - 认证服务器
using HiFly.Openiddict;
using HiFly.Identity.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 📚 添加数据库上下文
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict(); // 启用 OpenIddict 支持
});

// 👤 配置 Identity 系统
builder.Services.AddHiFlyIdentity<ApplicationDbContext, ApplicationUser, ApplicationRole>(options =>
{
    options.RequireConfirmedEmail = false;
    options.PasswordRequiredLength = 8;
    options.RequireUniqueEmail = true;
    
    // 密码策略
    options.PasswordRequireDigit = true;
    options.PasswordRequireLowercase = true;
    options.PasswordRequireUppercase = true;
    options.PasswordRequireNonAlphanumeric = false;
    
    // 锁定策略
    options.MaxFailedAccessAttempts = 5;
    options.DefaultLockoutMinutes = 15;
});

// 🔐 配置 OpenIddict 服务器
builder.Services.AddHiFlyOpenIdServer<ApplicationDbContext>(options =>
{
    options.AllowAuthorizationCodeFlow = true;
    options.AllowRefreshTokenFlow = true;
    options.RequireProofKeyForCodeExchange = true;
    options.AllowPasswordFlow = false; // 生产环境建议禁用
    options.DisableAccessTokenEncryption = false; // 生产环境必须加密
    
    // 自定义作用域
    options.CustomScopes.AddRange(new[] { "api1", "api2", "hr", "finance", "admin" });
    
    // 配置令牌生命周期
    options.AccessTokenLifetime = TimeSpan.FromHours(1);
    options.RefreshTokenLifetime = TimeSpan.FromDays(30);
    options.AuthorizationCodeLifetime = TimeSpan.FromMinutes(10);
});

// 🏢 添加应用服务
builder.Services.AddHiFlyAppServices<ApplicationDbContext, ApplicationUser, ApplicationRole, MenuPage, RoleMenu>();

// 🎨 添加 Blazor 服务
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// 🛠️ 配置中间件管道
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 🗺️ 映射 OpenIddict 端点
app.MapHiFlyOpenIdServerEndpoints<ApplicationUser>(options =>
{
    options.LoginPath = "/Account/Login";
    options.IssuerUri = "https://auth.yourdomain.com";
    options.AllowedOrigins = "https://app1.yourdomain.com,https://app2.yourdomain.com";
});

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// 🔧 初始化数据
await InitializeDataAsync(app.Services);

app.Run();

// 📊 数据初始化方法
static async Task InitializeDataAsync(IServiceProvider services)
{
    await services.InitializeClientsAsync(new[]
    {
        new ServerClientOptions
        {
            ClientId = "hifly-blazor-app",
            ClientSecret = "your-secure-secret-key-minimum-32-chars",
            DisplayName = "HiFly Blazor Application",
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            RedirectUris = { 
                "https://app.yourdomain.com/signin-oidc",
                "https://localhost:7001/signin-oidc" 
            },
            PostLogoutRedirectUris = { 
                "https://app.yourdomain.com/",
                "https://localhost:7001/" 
            },
            FrontChannelLogoutUri = "https://app.yourdomain.com/frontchannel-logout",
            Permissions = {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Logout,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
                Permissions.Scopes.OpenId,
                Permissions.Prefixes.Scope + "api1",
                Permissions.Prefixes.Scope + "hr"
            }
        }
    });
    
    await services.InitializeDefaultUserAsync<ApplicationDbContext, ApplicationUser, ApplicationRole>();
}
```

#### 步骤 2: 客户端应用配置

配置您的 Blazor 客户端应用：

```csharp
// Program.cs - Blazor 客户端
using HiFly.Openiddict;
using HiFly.Openiddict.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 🎨 添加 Blazor 服务
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 🔐 配置完整的 SSO 客户端
builder.Services.AddHiFlySsoClient(
    // 基础客户端配置
    configureClient: options =>
    {
        options.Authority = "https://auth.yourdomain.com";
        options.ClientId = "hifly-blazor-app";
        options.ClientSecret = "your-secure-secret-key-minimum-32-chars";
        options.Scopes = ["openid", "profile", "email", "roles", "offline_access", "api1", "hr"];
        
        // 🔄 启用自动 Token 刷新
        options.EnableAutoTokenRefresh = true;
        options.AutoRefreshOptions = new AutoTokenRefreshOptions
        {
            RefreshThresholdMinutes = 5,
            RedirectToLoginOnRefreshFailure = true,
            AddRefreshHeader = true,
            SkipApiRequests = false,
            ExcludePaths = ["/api/health", "/api/metrics"]
        };
        
        // 🛡️ 安全配置
        options.RequireHttpsMetadata = true;
        options.DisableSslCertificateValidation = false;
        
        // ⚙️ 自定义 OIDC 配置
        options.ConfigureOpenIdConnectOptions = oidcOptions =>
        {
            oidcOptions.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
            oidcOptions.MaxAge = TimeSpan.FromHours(8);
            oidcOptions.Events.OnTokenValidated = async context =>
            {
                // 自定义 Token 验证逻辑
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("用户 {UserId} 已成功通过认证", 
                    context.Principal?.FindFirst("sub")?.Value);
            };
        };
    },
    
    // 🌐 SSO 配置
    configureSso: options =>
    {
        options.EnableSso = true;
        options.SessionCheckIntervalSeconds = 30;
        options.EnableCrossDomainSso = true;
        options.AllowedOrigins = ["https://app1.yourdomain.com", "https://app2.yourdomain.com"];
        options.EnableFrontChannelLogout = true;
        options.FrontChannelLogoutUri = "/frontchannel-logout";
        options.SessionTimeoutMinutes = 480; // 8小时
    },
    
    // 🎫 Token 管理配置
    configureTokenManagement: options =>
    {
        options.EnableTokenManagement = true;
        options.AccessTokenLifetimeMinutes = 60;
        options.RefreshTokenLifetimeDays = 30;
        options.EnableTokenCache = true;
        options.EnableTokenEncryption = true;
        options.TokenCacheLifetimeMinutes = 5;
    },
    
    // 🔒 安全配置
    configureSecurity: options =>
    {
        options.EnablePkce = true;
        options.RequireHttps = true;
        options.EnableStateValidation = true;
        options.EnableNonceValidation = true;
        options.EnableCors = true;
        options.EnableSecurityHeaders = true;
    },
    
    // 📋 审计配置
    configureAudit: options =>
    {
        options.EnableAudit = true;
        options.LogSuccessfulAuthentication = true;
        options.LogFailedAuthentication = true;
        options.LogTokenOperations = true;
        options.LogRetentionDays = 90;
        options.LogSensitiveData = false; // 生产环境必须为 false
    }
);

// 💾 添加分布式缓存（推荐用于生产环境）
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "HiFlySSO";
});

builder.Services.AddDistributedTokenCache(options =>
{
    options.RedisConnectionString = builder.Configuration.GetConnectionString("Redis");
    options.DefaultExpirationMinutes = 60;
});

var app = builder.Build();

// 🛠️ 配置中间件管道
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 🌐 使用 HiFly SSO 中间件
app.UseHiFlySso(options =>
{
    options.EnableAutoTokenRefresh = true;
    options.EnableSessionSync = true;
    options.EnableSecurityHeaders = true;
    options.EnableAuditLogging = true;
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 🗺️ 映射客户端端点
app.MapHiFlyOpenIdClientEndpoints(options =>
{
    options.SigninPath = "/signin";
    options.SignoutPath = "/signout";
    options.SsoSessionCheckPath = "/api/sso/session";
    options.TokenStatusPath = "/api/token/status";
    options.RefreshTokenPath = "/api/token/refresh";
});

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

#### 步骤 3: 在 Blazor 页面中使用

```razor
@page "/"
@using HiFly.Openiddict.Components
@using HiFly.Openiddict.Services.Interfaces
@inject NavigationManager Navigation
@inject ISsoService SsoService
@inject ITokenManagementService TokenService

<PageTitle>HiFly 企业级 SSO 演示</PageTitle>

<div class="container-fluid">
    <div class="row mb-4">
        <div class="col-12">
            <div class="jumbotron bg-primary text-white p-4 rounded">
                <h1 class="display-4">🚀 欢迎使用 HiFly SSO</h1>
                <p class="lead">企业级单点登录解决方案演示</p>
            </div>
        </div>
    </div>

    <div class="row">
        <!-- SSO 状态管理 -->
        <div class="col-lg-4 mb-4">
            <div class="card h-100">
                <div class="card-header bg-info text-white">
                    <h5><i class="fas fa-users"></i> SSO 状态管理</h5>
                </div>
                <div class="card-body">
                    <SsoManager 
                        ShowControls="true" 
                        ShowStatusBar="true" 
                        ShowSessionInfo="true"
                        AutoCheckSession="true"
                        CheckIntervalSeconds="60"
                        OnSessionChanged="HandleSessionChanged"
                        OnSignInCompleted="HandleSignInCompleted"
                        OnSignOutCompleted="HandleSignOutCompleted" />
                </div>
            </div>
        </div>
        
        <!-- Token 状态监控 -->
        <div class="col-lg-4 mb-4">
            <div class="card h-100">
                <div class="card-header bg-success text-white">
                    <h5><i class="fas fa-key"></i> Token 状态监控</h5>
                </div>
                <div class="card-body">
                    <TokenStatusIndicator 
                        ShowIndicator="true" 
                        ShowDetails="true" 
                        AutoRefresh="true"
                        RefreshIntervalSeconds="30"
                        OnTokenStatusChanged="HandleTokenStatusChanged"
                        OnTokenRefreshed="HandleTokenRefreshed" />
                </div>
            </div>
        </div>
        
        <!-- 用户信息展示 -->
        <div class="col-lg-4 mb-4">
            <div class="card h-100">
                <div class="card-header bg-warning text-dark">
                    <h5><i class="fas fa-user"></i> 用户信息</h5>
                </div>
                <div class="card-body">
                    <AuthorizeView>
                        <Authorized>
                            <UserProfile User="@context.User" />
                        </Authorized>
                        <NotAuthorized>
                            <div class="text-center">
                                <i class="fas fa-sign-in-alt fa-3x text-muted mb-3"></i>
                                <h6>欢迎使用 HiFly 企业应用</h6>
                                <p class="text-muted">请登录以访问您的个人资料和应用功能。</p>
                                <button class="btn btn-primary" @onclick="NavigateToLogin">
                                    <i class="fas fa-sign-in-alt"></i> 立即登录
                                </button>
                            </div>
                        </NotAuthorized>
                    </AuthorizeView>
                </div>
            </div>
        </div>
    </div>
    
    <!-- 管理员功能 -->
    <AuthorizeView Roles="Admin,系统管理员">
        <div class="row">
            <div class="col-12">
                <div class="card border-danger">
                    <div class="card-header bg-danger text-white">
                        <h5><i class="fas fa-cogs"></i> 管理员控制面板</h5>
                    </div>
                    <div class="card-body">
                        <div class="row">
                            <div class="col-md-3">
                                <div class="card bg-light">
                                    <div class="card-body text-center">
                                        <i class="fas fa-users fa-2x text-primary mb-2"></i>
                                        <h6>用户管理</h6>
                                        <small class="text-muted">管理系统用户</small>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="card bg-light">
                                    <div class="card-body text-center">
                                        <i class="fas fa-shield-alt fa-2x text-success mb-2"></i>
                                        <h6>安全设置</h6>
                                        <small class="text-muted">配置安全策略</small>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="card bg-light">
                                    <div class="card-body text-center">
                                        <i class="fas fa-chart-line fa-2x text-warning mb-2"></i>
                                        <h6>监控报告</h6>
                                        <small class="text-muted">查看系统指标</small>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-3">
                                <div class="card bg-light">
                                    <div class="card-body text-center">
                                        <i class="fas fa-cog fa-2x text-info mb-2"></i>
                                        <h6>系统配置</h6>
                                        <small class="text-muted">修改系统设置</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </AuthorizeView>
</div>

@code {
    private void HandleSessionChanged(SsoManager.SsoSessionInfo sessionInfo)
    {
        Console.WriteLine($"🔄 会话状态变化: {sessionInfo.IsActive}, 用户: {sessionInfo.UserName}");
        StateHasChanged();
    }
    
    private void HandleSignInCompleted()
    {
        Console.WriteLine("✅ 登录完成");
        StateHasChanged();
    }
    
    private void HandleSignOutCompleted()
    {
        Console.WriteLine("👋 登出完成");
        Navigation.NavigateTo("/", true);
    }
    
    private void HandleTokenStatusChanged(TokenStatusIndicator.TokenStatusInfo tokenStatus)
    {
        if (tokenStatus.IsExpiringSoon)
        {
            Console.WriteLine("⚠️ 令牌即将过期，建议刷新");
        }
    }
    
    private void HandleTokenRefreshed()
    {
        Console.WriteLine("🔄 令牌已刷新");
    }
    
    private void NavigateToLogin()
    {
        Navigation.NavigateTo("/signin");
    }
}
```

---

## 📚 详细文档

### 🎨 Blazor 组件

#### SsoManager 组件
完整的 SSO 状态管理和控制组件：

```razor
<SsoManager 
    ShowControls="true"           @* 显示登录/登出按钮 *@
    ShowStatusBar="true"          @* 显示状态栏 *@
    ShowSessionInfo="true"        @* 显示会话信息 *@
    AutoCheckSession="true"       @* 自动检查会话 *@
    CheckIntervalSeconds="60"     @* 检查间隔（秒） *@
    OnSessionChanged="@OnSessionChanged"
    OnSignInCompleted="@OnSignInCompleted"
    OnSignOutCompleted="@OnSignOutCompleted" />
```

#### TokenStatusIndicator 组件
Token 状态监控和管理：

```razor
<TokenStatusIndicator 
    ShowIndicator="true"          @* 显示状态指示器 *@
    ShowDetails="true"            @* 显示详细信息 *@
    AutoRefresh="true"            @* 自动刷新 *@
    RefreshIntervalSeconds="30"   @* 刷新间隔（秒） *@
    OnTokenStatusChanged="@OnTokenStatusChanged"
    OnTokenRefreshed="@OnTokenRefreshed" />
```

### ⚙️ 高级配置

#### 多环境配置文件

**appsettings.Development.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HiFlyAuth_Dev;Trusted_Connection=true;",
    "Redis": "localhost:6379"
  },
  "HiFlySso": {
    "Authority": "https://localhost:6001",
    "RequireHttpsMetadata": false,
    "Security": {
      "EnablePkce": true,
      "RequireHttps": false
    },
    "Audit": {
      "EnableAudit": true,
      "LogSensitiveData": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "HiFly.Openiddict": "Debug"
    }
  }
}
```

**appsettings.Production.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql-server;Database=HiFlyAuth;User Id=sa;Password=YourPassword;TrustServerCertificate=true;",
    "Redis": "prod-redis-cluster:6379"
  },
  "HiFlySso": {
    "Authority": "https://auth.yourdomain.com",
    "RequireHttpsMetadata": true,
    "TokenLifetime": {
      "AccessTokenMinutes": 60,
      "RefreshTokenDays": 30,
      "IdTokenMinutes": 15
    },
    "Security": {
      "EnablePkce": true,
      "RequireHttps": true,
      "MaxAuthenticationAge": 28800,
      "EnableSecurityHeaders": true
    },
    "Audit": {
      "EnableAudit": true,
      "LogRetentionDays": 90,
      "LogSensitiveData": false
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "HiFly.Openiddict": "Information"
    }
  }
}
```

#### 自定义事件处理

```csharp
builder.Services.AddHiFlyOpenIdClient(options =>
{
    options.ConfigureOpenIdConnectOptions = oidcOptions =>
    {
        // Token 验证事件
        oidcOptions.Events.OnTokenValidated = async context =>
        {
            var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
            var userId = context.Principal?.FindFirst("sub")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await userService.GetUserByIdAsync(userId);
                if (user?.IsActive != true)
                {
                    context.Fail("用户账户已被禁用");
                    return;
                }
                
                // 更新最后登录时间
                await userService.UpdateLastLoginAsync(userId);
            }
        };
        
        // 重定向到身份提供者事件
        oidcOptions.Events.OnRedirectToIdentityProvider = context =>
        {
            // 添加自定义参数
            context.ProtocolMessage.SetParameter("tenant", "default");
            context.ProtocolMessage.SetParameter("ui_locales", "zh-CN");
            return Task.CompletedTask;
        };
        
        // 认证失败事件
        oidcOptions.Events.OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "认证失败: {Error}", context.Exception.Message);
            
            context.Response.Redirect($"/error?message={Uri.EscapeDataString(context.Exception.Message)}");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    };
});
```

### 🔒 安全最佳实践

#### 1. 证书配置
```csharp
// 生产环境证书配置
builder.Services.AddHiFlyOpenIdServer<ApplicationDbContext>(options =>
{
    // 使用证书进行签名和加密
    options.AddSigningCertificate(GetSigningCertificate());
    options.AddEncryptionCertificate(GetEncryptionCertificate());
    
    // 或者使用开发证书（仅开发环境）
    if (builder.Environment.IsDevelopment())
    {
        options.AddDevelopmentSigningCertificate();
        options.AddDevelopmentEncryptionCertificate();
    }
});

static X509Certificate2 GetSigningCertificate()
{
    // 从证书存储或文件加载证书
    return new X509Certificate2("path/to/signing-cert.pfx", "password");
}
```

#### 2. CORS 配置
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("HiFlyPolicy", builder =>
    {
        builder.WithOrigins(
            "https://app1.yourdomain.com",
            "https://app2.yourdomain.com",
            "https://mobile.yourdomain.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("X-Token-Expires-At");
    });
});

app.UseCors("HiFlyPolicy");
```

#### 3. 速率限制
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});

app.UseRateLimiter();

// 在认证端点上应用速率限制
app.MapPost("/connect/token", async (HttpContext context) =>
{
    // Token 端点逻辑
}).RequireRateLimiting("AuthPolicy");
```

---

## 🔗 API 参考

### REST API 端点

| 端点 | 方法 | 描述 | 认证要求 |
|------|------|------|----------|
| `/api/sso/session` | GET | 获取 SSO 会话信息 | 可选 |
| `/api/token/status` | GET | 获取 Token 状态 | 必需 |
| `/api/token/refresh` | POST | 手动刷新 Token | 必需 |
| `/signin` | GET | 启动登录流程 | 无 |
| `/signout` | GET/POST | 启动登出流程 | 可选 |
| `/frontchannel-logout` | GET | 前端通道登出 | 无 |

### 响应示例

#### Token 状态查询
```http
GET /api/token/status
Authorization: Bearer <access_token>
```

```json
{
  "authenticated": true,
  "expiresAt": "2024-01-20T10:30:00.000Z",
  "remainingSeconds": 3600,
  "isExpiringSoon": false,
  "scopes": ["openid", "profile", "email", "api1"],
  "roles": ["User", "Employee"]
}
```

#### SSO 会话信息
```http
GET /api/sso/session
```

```json
{
  "active": true,
  "session": {
    "isActive": true,
    "userName": "john.doe@company.com",
    "userId": "12345678-1234-1234-1234-123456789abc",
    "sessionId": "sess_abcdef123456",
    "loginTime": "2024-01-20T08:00:00.000Z",
    "lastActivity": "2024-01-20T09:45:00.000Z",
    "expiresAt": "2024-01-20T16:00:00.000Z",
    "applications": [
      {
        "clientId": "app1",
        "name": "HR 系统",
        "lastAccessed": "2024-01-20T09:30:00.000Z"
      },
      {
        "clientId": "app2",
        "name": "财务系统",
        "lastAccessed": "2024-01-20T09:45:00.000Z"
      }
    ]
  }
}
```

---

## 📊 示例项目

### 🏢 企业级多应用 SSO

一个完整的企业级示例，包含：
- 认证服务器
- HR 管理系统
- 财务管理系统
- 客户关系管理系统

```bash
git clone https://github.com/HiFly/OpeniddictExample.git
cd OpeniddictExample
dotnet run --project HiFly.Auth.Server
```

### 🛒 电商平台 SSO

电商平台示例，展示：
- 用户中心
- 商家后台
- 订单系统
- 支付网关

### 📚 在线教育平台

教育平台示例，包含：
- 学生端
- 教师端
- 管理端
- 直播系统

---

## 🚀 生产环境部署

### 🐳 Docker 部署

#### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["HiFly.Auth.Server/HiFly.Auth.Server.csproj", "HiFly.Auth.Server/"]
COPY ["HiFly.Openiddict/HiFly.Openiddict.csproj", "HiFly.Openiddict/"]
RUN dotnet restore "HiFly.Auth.Server/HiFly.Auth.Server.csproj"
COPY . .
WORKDIR "/src/HiFly.Auth.Server"
RUN dotnet build "HiFly.Auth.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HiFly.Auth.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HiFly.Auth.Server.dll"]
```

#### docker-compose.yml
```yaml
version: '3.8'

services:
  hifly-auth:
    build: 
      context: .
      dockerfile: HiFly.Auth.Server/Dockerfile
    ports:
      - "6001:80"
      - "6443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=HiFlyAuth;User Id=sa;Password=YourPassword123;
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - sql-server
      - redis
    volumes:
      - ./certs:/app/certs:ro

  hifly-app1:
    build:
      context: .
      dockerfile: HiFly.App1/Dockerfile
    ports:
      - "7001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - HiFlySso__Authority=https://hifly-auth:443
    depends_on:
      - hifly-auth

  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  sql_data:
  redis_data:
```

### ☸️ Kubernetes 部署

#### 部署文件示例
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hifly-auth-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: hifly-auth-server
  template:
    metadata:
      labels:
        app: hifly-auth-server
    spec:
      containers:
      - name: hifly-auth-server
        image: hifly/auth-server:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: hifly-secrets
              key: database-connection
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: hifly-auth-service
spec:
  selector:
    app: hifly-auth-server
  ports:
  - port: 80
    targetPort: 80
  type: ClusterIP
```

---

## 🔧 故障排除

### 常见问题解决方案

#### 1. Token 自动刷新失败

**问题现象：**
- 用户会话突然中断
- API 调用返回 401 未授权
- 控制台出现 Token 刷新错误

**解决方案：**
```csharp
// 检查刷新令牌配置
services.Configure<AutoTokenRefreshOptions>(options =>
{
    options.RefreshThresholdMinutes = 5; // 提前5分钟刷新
    options.RedirectToLoginOnRefreshFailure = true;
    options.MaxRetryAttempts = 3; // 最大重试次数
    options.RetryDelay = TimeSpan.FromSeconds(2);
});

// 启用详细日志
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddFilter("HiFly.Openiddict.TokenManagement", LogLevel.Trace);
});
```

#### 2. 跨域请求问题

**问题现象：**
- 浏览器控制台出现 CORS 错误
- 前端无法调用认证 API
- OPTIONS 请求失败

**解决方案：**
```csharp
// 服务器端 CORS 配置
app.UseCors(policy =>
{
    policy.WithOrigins(
        "https://app1.yourdomain.com",
        "https://app2.yourdomain.com"
    )
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithExposedHeaders("X-Token-Expires-At", "X-Refresh-Token");
});

// 客户端配置
services.Configure<SsoOptions>(options =>
{
    options.AllowedOrigins = [
        "https://auth.yourdomain.com",
        "https://api.yourdomain.com"
    ];
});
```

#### 3. 会话状态不同步

**问题现象：**
- 在一个应用登出后，其他应用仍显示已登录
- 会话检查失败
- 前端通道登出不工作

**解决方案：**
```csharp
// 配置会话同步
services.Configure<SessionSyncOptions>(options =>
{
    options.ValidationIntervalSeconds = 60; // 降低检查频率
    options.RedirectToLoginOnSessionInvalid = true;
    options.EnableHeartbeat = true; // 启用心跳检测
});

// 确保前端通道登出配置正确
await services.InitializeClientsAsync(new[]
{
    new ServerClientOptions
    {
        FrontChannelLogoutUri = "https://app.yourdomain.com/frontchannel-logout",
        FrontChannelLogoutSessionRequired = true,
        BackChannelLogoutUri = "https://app.yourdomain.com/api/logout", // 可选
    }
});
```

### 🐛 调试技巧

#### 启用详细日志
```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<AuditOptions>(options =>
    {
        options.LogSensitiveData = true;
        options.EnableDebugLogging = true;
    });
    
    builder.Logging.AddFilter("HiFly.Openiddict", LogLevel.Trace);
    builder.Logging.AddFilter("OpenIddict", LogLevel.Debug);
}
```

#### 健康检查端点
```csharp
builder.Services.AddHealthChecks()
    .AddDbContext<ApplicationDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis"))
    .AddCheck<TokenManagementHealthCheck>("token-management")
    .AddCheck<SsoSessionHealthCheck>("sso-session");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

---

## 📈 性能优化

### 🚀 缓存策略

#### 1. Token 缓存
```csharp
services.Configure<TokenManagementOptions>(options =>
{
    options.EnableTokenCache = true;
    options.TokenCacheLifetimeMinutes = 5;
    options.MaxCacheItems = 10000;
    options.EnableCacheCompression = true;
});
```

#### 2. 分布式缓存
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis-cluster:6379";
    options.InstanceName = "HiFlySSO";
    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
    {
        AbortOnConnectFail = false,
        ConnectRetry = 3,
        ConnectTimeout = 5000
    };
});
```

### 📊 监控指标

#### 应用程序洞察集成
```csharp
builder.Services.AddApplicationInsightsTelemetry();

// 自定义遥测初始化器
services.AddSingleton<ITelemetryInitializer, HiFlySsoTelemetryInitializer>();

public class HiFlySsoTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry requestTelemetry)
        {
            requestTelemetry.Properties["Application"] = "HiFly.SSO";
            requestTelemetry.Properties["Version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        }
    }
}
```

#### Prometheus 指标
```csharp
builder.Services.AddSingleton<IMetricsLogger, PrometheusMetricsLogger>();

app.UseRouting();
app.UseHttpMetrics(); // Prometheus 中间件
app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics(); // /metrics 端点
});
```

---

## 📞 技术支持

### 🏢 联系方式

- **官方网站**: [www.hongfei8.cn](https://www.hongfei8.cn)
- **技术支持**: [felix@hongfei8.com](mailto:felix@hongfei8.com)
- **备用邮箱**: [hongfei8@outlook.com](mailto:hongfei8@outlook.com)
- **在线文档**: [docs.hongfei8.cn/openiddict](https://docs.hongfei8.cn/openiddict)

### 📋 支持计划

| 类型 | 响应时间 | 支持渠道 | 价格 |
|------|----------|----------|------|
| 社区支持 | 7个工作日 | GitHub Issues | 免费 |
| 标准支持 | 2个工作日 | 邮件 + 电话 | ¥5,000/年 |
| 企业支持 | 4小时内 | 专属技术顾问 | ¥20,000/年 |
| 定制开发 | 即时响应 | 现场支持 | 按项目报价 |

### 🤝 社区参与

- **GitHub**: [HiFly/HiFly.Openiddict](https://github.com/HiFly/HiFly.Openiddict)
- **技术论坛**: [forum.hongfei8.cn](https://forum.hongfei8.cn)
- **微信群**: 扫描二维码加入技术交流群
- **QQ群**: 123456789

---

## 📄 许可证

```
Copyright (c) 2024 弘飞帮联科技有限公司
保留所有权利。

本软件受版权法和国际版权条约保护。未经版权所有者明确书面许可，
禁止以任何形式复制、分发、修改或创建衍生作品。

商业使用需要获得有效的商业许可证。
详情请联系: felix@hongfei8.com

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
```

---

## 🚀 更新日志

### v1.2.0 (2024-01-20)
- ✨ 新增前端通道登出支持
- 🔧 改进 Token 缓存机制
- 🛡️ 增强安全配置选项
- 📊 添加性能监控功能
- 🐛 修复会话同步问题

### v1.1.0 (2024-01-10)
- ✨ 新增分布式缓存支持
- 🔄 优化 Token 自动刷新逻辑
- 🎨 改进 Blazor 组件样式
- 📈 添加健康检查端点

### v1.0.0 (2024-01-01)
- 🎉 首个正式版本发布
- 🔐 完整的 SSO 功能
- 🎨 Blazor 组件库
- 📚 完整文档

---

<div align="center">

**⭐ 如果这个项目对您有帮助，请给我们一个 Star！**

[![Star History Chart](https://api.star-history.com/svg?repos=HiFly/HiFly.Openiddict&type=Date)](https://star-history.com/#HiFly/HiFly.Openiddict&Date)

---

**🔗 相关项目**

[HiFly.Identity](https://github.com/HiFly/HiFly.Identity) • [HiFly.Tables](https://github.com/HiFly/HiFly.Tables) • [HiFly.BbLayout](https://github.com/HiFly/HiFly.BbLayout)

---

*HiFly.Openiddict - 让企业级身份认证变得简单 🚀*

</div>
