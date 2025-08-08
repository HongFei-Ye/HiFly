// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.DatabaseManager.Options;

/// <summary>
/// 数据库服务配置选项
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// 数据库提供程序类型
    /// </summary>
    public DatabaseProviderType ProviderType { get; set; } = DatabaseProviderType.SqlServer;

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 是否从连接字符串自动检测提供程序类型
    /// </summary>
    public bool AutoDetectProviderFromConnectionString { get; set; } = false;

    /// <summary>
    /// 备份文件保存路径
    /// </summary>
    public string BackupDirectory { get; set; } = "Backups";

    /// <summary>
    /// 默认数据库命令超时时间(秒)
    /// </summary>
    public int CommandTimeout { get; set; } = 60;

    /// <summary>
    /// 是否启用失败重试
    /// </summary>
    public bool EnableRetryOnFailure { get; set; } = true;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// 最大重试延迟(秒)
    /// </summary>
    public int MaxRetryDelaySec { get; set; } = 30;
}
