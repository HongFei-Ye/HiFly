// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.DatabaseManager;

/// <summary>
/// 数据库服务工厂，用于创建对应数据库类型的服务实现
/// </summary>
public static class DatabaseServiceFactory
{
    /// <summary>
    /// 创建指定类型的数据库服务
    /// </summary>
    /// <param name="providerType">数据库提供程序类型</param>
    /// <returns>数据库服务实现</returns>
    public static IDatabaseService Create(DatabaseProviderType providerType)
    {
        return providerType switch
        {
            DatabaseProviderType.SqlServer => new SqlServerDatabaseService(),
            DatabaseProviderType.PostgreSQL => new PostgreSqlDatabaseService(),
            DatabaseProviderType.MySQL => new MySqlDatabaseService(),
            DatabaseProviderType.SQLite => new SQLiteDatabaseService(),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), $"不支持的数据库提供程序类型: {providerType}")
        };
    }

    /// <summary>
    /// 尝试从连接字符串推断数据库提供程序类型
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    /// <returns>推断的数据库提供程序类型</returns>
    public static DatabaseProviderType DetectProviderFromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("连接字符串不能为空", nameof(connectionString));

        string lowerConnStr = connectionString.ToLowerInvariant();

        // 检查SQL Server特定关键字
        if (lowerConnStr.Contains("data source=") ||
            lowerConnStr.Contains("server=") && lowerConnStr.Contains("initial catalog=") ||
            lowerConnStr.Contains("trusted_connection=") ||
            lowerConnStr.Contains("integrated security="))
        {
            return DatabaseProviderType.SqlServer;
        }

        // 检查PostgreSQL特定关键字
        if (lowerConnStr.Contains("host=") && lowerConnStr.Contains("database=") &&
           (lowerConnStr.Contains("username=") || lowerConnStr.Contains("user id=")) &&
            lowerConnStr.Contains("port="))
        {
            return DatabaseProviderType.PostgreSQL;
        }

        // 检查MySQL特定关键字
        if (lowerConnStr.Contains("server=") && lowerConnStr.Contains("database=") &&
           (lowerConnStr.Contains("uid=") || lowerConnStr.Contains("user id=")) &&
            lowerConnStr.Contains("pwd="))
        {
            return DatabaseProviderType.MySQL;
        }

        // 检查SQLite特定关键字
        if (lowerConnStr.Contains("data source=") &&
           (lowerConnStr.Contains(".db") || lowerConnStr.Contains(".sqlite")))
        {
            return DatabaseProviderType.SQLite;
        }

        // 默认返回SQL Server
        return DatabaseProviderType.SqlServer;
    }

    /// <summary>
    /// 根据连接字符串创建适合的数据库服务
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    /// <returns>数据库服务实现</returns>
    public static IDatabaseService CreateFromConnectionString(string connectionString)
    {
        var providerType = DetectProviderFromConnectionString(connectionString);
        return Create(providerType);
    }

}
