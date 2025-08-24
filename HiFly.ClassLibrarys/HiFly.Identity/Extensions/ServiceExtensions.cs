// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using HiFly.Identity.Options;
using HiFly.Identity.Services;
using HiFly.Identity.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace HiFly.Identity.Extensions;

/// <summary>
/// HiFly Identity 服务扩展方法
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// 注册HiFly Identity服务配置
    /// </summary>
    /// <typeparam name="TContext">数据库上下文类型</typeparam>
    /// <typeparam name="TUser">用户类型</typeparam>
    /// <typeparam name="TRole">角色类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
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

        services.PostConfigure<AuthenticationOptions>(opts =>
        {
            opts.DefaultScheme = IdentityConstants.ApplicationScheme;
            opts.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            opts.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        });

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

        // 注册核心 Identity 服务
        services.AddScoped<IAuthStateService, AuthStateService>();
        services.AddScoped<IUserService, UserService<TContext, TUser>>();
        services.AddScoped<IRoleService, RoleService<TContext, TRole>>();

        return services;
    }

    /// <summary>
    /// 初始化默认用户和角色
    /// </summary>
    /// <typeparam name="TContext">数据库上下文类型</typeparam>
    /// <typeparam name="TUser">用户类型</typeparam>
    /// <typeparam name="TRole">角色类型</typeparam>
    /// <param name="services">服务提供者</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>任务</returns>
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

