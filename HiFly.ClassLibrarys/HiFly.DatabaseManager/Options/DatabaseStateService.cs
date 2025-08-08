// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.DatabaseManager.Options;

/// <summary>
/// 数据库状态服务 - 用于跨组件共享数据库相关状态
/// </summary>
public class DatabaseStateService
{
    /// <summary>
    /// 数据库是否需要初始化设置
    /// </summary>
    public bool NeedsDatabaseSetup { get; set; }

    /// <summary>
    /// 是否存在待处理的数据库迁移
    /// </summary>
    public bool HasPendingMigrations { get; set; }

    /// <summary>
    /// 是否需要重启应用以应用配置更改
    /// </summary>
    public bool NeedsRestart { get; set; }

    /// <summary>
    /// 上次修改连接字符串的时间
    /// </summary>
    public DateTime? LastConnectionStringChangeTime { get; set; }

    /// <summary>
    /// 设置需要重启的状态并记录修改时间
    /// </summary>
    public void SetRestartRequired()
    {
        NeedsRestart = true;
        LastConnectionStringChangeTime = DateTime.Now;
    }

    /// <summary>
    /// 重置重启状态
    /// </summary>
    public void ResetRestartState()
    {
        NeedsRestart = false;
        LastConnectionStringChangeTime = null;
    }

    /// <summary>
    /// 获取距离上次修改的时间说明
    /// </summary>
    public string GetTimeSinceLastChange()
    {
        if (!LastConnectionStringChangeTime.HasValue)
            return string.Empty;

        var span = DateTime.Now - LastConnectionStringChangeTime.Value;

        if (span.TotalHours >= 24)
            return $"{(int)span.TotalDays} 天前";
        else if (span.TotalMinutes >= 60)
            return $"{(int)span.TotalHours} 小时前";
        else if (span.TotalSeconds >= 60)
            return $"{(int)span.TotalMinutes} 分钟前";
        else
            return "刚刚";
    }

}
