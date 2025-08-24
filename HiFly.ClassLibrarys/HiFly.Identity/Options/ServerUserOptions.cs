// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiFly.Identity.Options;

/// <summary>
/// ASP.NET Core Identity用户和认证配置选项
/// </summary>
public class ServerUserOptions
{
    #region 系统管理员用户配置
    /// <summary>
    /// 系统管理员用户名
    /// </summary>
    public string SystemAdminUserName { get; set; } = "SystemAdmin";

    /// <summary>
    /// 系统管理员邮箱
    /// </summary>
    public string SystemAdminEmail { get; set; } = "SystemAdmin@hongfei8.com";

    /// <summary>
    /// 系统管理员密码
    /// </summary>
    public string SystemAdminPassword { get; set; } = "123456+";
    #endregion

    #region 账户确认配置
    /// <summary>
    /// 是否要求确认账户才能登录
    /// </summary>
    public bool RequireConfirmedAccount { get; set; } = true;

    /// <summary>
    /// 是否要求确认邮箱才能登录
    /// </summary>
    public bool RequireConfirmedEmail { get; set; } = true;

    /// <summary>
    /// 是否要求确认手机号才能登录
    /// </summary>
    public bool RequireConfirmedPhoneNumber { get; set; } = false;
    #endregion

    #region 密码复杂度配置
    /// <summary>
    /// 密码是否必须包含数字
    /// </summary>
    public bool PasswordRequireDigit { get; set; } = true;

    /// <summary>
    /// 密码是否必须包含小写字母
    /// </summary>
    public bool PasswordRequireLowercase { get; set; } = true;

    /// <summary>
    /// 密码是否必须包含大写字母
    /// </summary>
    public bool PasswordRequireUppercase { get; set; } = true;

    /// <summary>
    /// 密码是否必须包含非字母数字字符
    /// </summary>
    public bool PasswordRequireNonAlphanumeric { get; set; } = true;

    /// <summary>
    /// 密码最小长度
    /// </summary>
    public int PasswordRequiredLength { get; set; } = 8;

    /// <summary>
    /// 密码中需要的唯一字符数
    /// </summary>
    public int PasswordRequiredUniqueChars { get; set; } = 1;
    #endregion

    #region 锁定策略配置
    /// <summary>
    /// 默认锁定时间（分钟）
    /// </summary>
    public int DefaultLockoutMinutes { get; set; } = 15;

    /// <summary>
    /// 最大失败访问尝试次数
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>
    /// 是否对新用户启用账户锁定
    /// </summary>
    public bool LockoutAllowedForNewUsers { get; set; } = true;
    #endregion

    #region 用户配置
    /// <summary>
    /// 是否要求唯一电子邮件
    /// </summary>
    public bool RequireUniqueEmail { get; set; } = true;

    /// <summary>
    /// 用户名允许使用的字符
    /// </summary>
    public string AllowedUserNameCharacters { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    #endregion

    #region 存储配置
    /// <summary>
    /// 主键的最大长度
    /// </summary>
    public int MaxLengthForKeys { get; set; } = 128;

    /// <summary>
    /// 是否保护个人数据
    /// </summary>
    public bool ProtectPersonalData { get; set; } = false;
    #endregion

    #region Cookie配置
    /// <summary>
    /// Cookie是否只能通过HTTP访问
    /// </summary>
    public bool CookieHttpOnly { get; set; } = true;

    /// <summary>
    /// Cookie安全策略
    /// </summary>
    public CookieSecurePolicy CookieSecurePolicy { get; set; } = CookieSecurePolicy.SameAsRequest;

    /// <summary>
    /// Cookie过期时间（分钟）
    /// </summary>
    public int CookieExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 是否使用滑动过期
    /// </summary>
    public bool CookieSlidingExpiration { get; set; } = true;

    /// <summary>
    /// 登录路径
    /// </summary>
    public string LoginPath { get; set; } = "/Account/Login";

    /// <summary>
    /// 注销路径
    /// </summary>
    public string LogoutPath { get; set; } = "/Account/Logout";

    /// <summary>
    /// 访问拒绝路径
    /// </summary>
    public string AccessDeniedPath { get; set; } = "/Account/AccessDenied";
    #endregion

    #region 外部认证提供程序配置
    /// <summary>
    /// 是否添加邮件令牌提供程序
    /// </summary>
    public bool AddEmailTokenProvider { get; set; } = false;

    /// <summary>
    /// Google认证客户端ID
    /// </summary>
    public string GoogleClientId { get; set; } = "";

    /// <summary>
    /// Google认证客户端密钥
    /// </summary>
    public string GoogleClientSecret { get; set; } = "";
    #endregion

    /// <summary>
    /// 角色和权限配置
    /// </summary>
    public Dictionary<string, RoleConfig> Roles { get; set; } = new Dictionary<string, RoleConfig>
    {
        {
            "SystemAdmin", new RoleConfig
            {
                ShowName = "系统管理员",
                Permissions = new[]
                {
                    "Users.Manage",
                    "Roles.Manage",
                    "System.Configure"
                }
            }
        }
    };

}


/// <summary>
/// 角色配置信息
/// </summary>
public class RoleConfig
{
    /// <summary>
    /// 角色显示名称
    /// </summary>
    public string ShowName { get; set; } = string.Empty;

    /// <summary>
    /// 角色权限列表
    /// </summary>
    public string[] Permissions { get; set; } = [];
}
