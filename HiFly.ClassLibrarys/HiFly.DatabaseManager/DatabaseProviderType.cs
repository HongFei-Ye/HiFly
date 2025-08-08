// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.DatabaseManager;

/// <summary>
/// 支持的数据库提供程序类型
/// </summary>
public enum DatabaseProviderType
{
    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SqlServer,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    PostgreSQL,

    /// <summary>
    /// MySQL/MariaDB
    /// </summary>
    MySQL,

    /// <summary>
    /// SQLite
    /// </summary>
    SQLite
}
