// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace HiFly.DatabaseManager;

/// <summary>
/// SQL Server 数据库服务实现
/// </summary>
public class SqlServerDatabaseService : DatabaseServiceBase
{
    /// <summary>
    /// 获取当前数据库提供程序类型
    /// </summary>
    protected override DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

    /// <summary>
    /// 配置数据库选项
    /// </summary>
    protected override void ConfigureDbContextOptions<TContext>(
        DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
    {
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60);
            sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
        });
    }

    /// <summary>
    /// 获取参数前缀
    /// </summary>
    protected override string ParameterPrefix => "@";

    /// <summary>
    /// 获取默认架构名称
    /// </summary>
    protected override string DefaultSchemaName => "dbo";

    /// <summary>
    /// 获取SQL Server统计查询
    /// </summary>
    protected override string StatisticsQuery => @"
SELECT 
    DB_NAME() AS DatabaseName,
    (SELECT ISNULL(SUM(size), 0) * 8.0 / 1024 FROM sys.database_files WHERE type = 0) AS DataSizeMB,
    (SELECT ISNULL(SUM(size), 0) * 8.0 / 1024 FROM sys.database_files WHERE type = 1) AS LogSizeMB,
    (SELECT COUNT(*) FROM sys.tables WHERE type = 'U') AS TableCount,
    (SELECT COUNT(*) FROM sys.indexes) AS IndexCount,
    (SELECT COUNT(*) FROM sys.foreign_keys) AS ForeignKeyCount,
    (SELECT COUNT(*) FROM sys.objects WHERE type = 'P') AS StoredProcedureCount;

SELECT 
    t.name AS TableName,
    SUM(p.rows) AS [RowCount],
    CAST(ROUND((SUM(a.total_pages) * 8.0 / 1024), 2) AS DECIMAL(18, 2)) AS TotalSpaceMB
FROM 
    sys.tables t
INNER JOIN      
    sys.indexes i ON t.object_id = i.object_id
INNER JOIN 
    sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
INNER JOIN 
    sys.allocation_units a ON p.partition_id = a.container_id
WHERE 
    t.is_ms_shipped = 0
    AND t.type = 'U'  -- 只包括用户表
    AND i.index_id <= 1  -- 避免重复计算行
GROUP BY 
    t.name
ORDER BY 
    TotalSpaceMB DESC;";

    /// <summary>
    /// SQL Server备份实现
    /// </summary>
    public override async Task<(bool Success, string? ErrorMessage)> BackupDatabaseAsync<TContext>(
        string? connectionString,
        string backupPath,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "连接字符串不能为空或空白。");

        if (string.IsNullOrWhiteSpace(backupPath))
            return (false, "备份路径不能为空或空白。");

        try
        {
            // 确保备份目录存在
            var backupDir = Path.GetDirectoryName(backupPath);

            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 获取数据库名称
            var databaseName = dbContext.Database.GetDbConnection().Database;

            // 创建备份命令
            string backupCommand = $"BACKUP DATABASE [{databaseName}] TO DISK = '{backupPath}' " +
                                   $"WITH FORMAT, MEDIANAME = '{databaseName}_backup', " +
                                   $"NAME = '{databaseName} 备份'";

            // 执行备份命令
            var result = await ExecuteNonQueryAsync<TContext>(
                connectionString, backupCommand, null, configureOptions);

            if (!result.Success)
            {
                return (false, $"备份数据库失败: {result.ErrorMessage}");
            }

            return (true, $"数据库 {databaseName} 已成功备份到 {backupPath}");
        }
        catch (Exception ex)
        {
            return (false, $"备份数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// SQL Server恢复实现
    /// </summary>
    public override async Task<(bool Success, string? ErrorMessage)> RestoreDatabaseAsync<TContext>(
        string? connectionString,
        string backupPath,
        Action<DbContextOptionsBuilder>? configureOptions = null)
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "连接字符串不能为空或空白。");

        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
            return (false, "备份文件不存在或路径无效。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 获取数据库名称
            var databaseName = dbContext.Database.GetDbConnection().Database;

            // 获取master连接字符串
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            builder["Initial Catalog"] = "master"; // 切换到master数据库
            var masterConnectionString = builder.ToString();

            // 创建恢复命令
            var restoreCommand = $@"
RESTORE DATABASE [{databaseName}] FROM DISK = '{backupPath}' 
WITH REPLACE, RECOVERY";

            // 执行恢复命令
            var result = await ExecuteNonQueryAsync<TContext>(
                masterConnectionString,
                restoreCommand,
                null,
                opt => opt.UseSqlServer(masterConnectionString));

            if (!result.Success)
            {
                return (false, $"恢复数据库失败: {result.ErrorMessage}");
            }

            return (true, $"数据库 {databaseName} 已成功从 {backupPath} 恢复");
        }
        catch (Exception ex)
        {
            return (false, $"恢复数据库失败: {ex.Message}");
        }
    }
}
