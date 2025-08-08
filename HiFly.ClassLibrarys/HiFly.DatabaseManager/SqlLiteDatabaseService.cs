// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using System.Data.Common;


namespace HiFly.DatabaseManager;

/// <summary>
/// SQLite 数据库服务实现
/// </summary>
public class SQLiteDatabaseService : DatabaseServiceBase
{
    /// <summary>
    /// 获取当前数据库提供程序类型
    /// </summary>
    protected override DatabaseProviderType ProviderType => DatabaseProviderType.SQLite;

    /// <summary>
    /// 配置数据库选项
    /// </summary>
    protected override void ConfigureDbContextOptions<TContext>(
        DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
    {
        optionsBuilder.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(60);
            sqliteOptions.MigrationsHistoryTable("__EFMigrationsHistory");
        });
    }

    /// <summary>
    /// 获取参数前缀
    /// </summary>
    protected override string ParameterPrefix => "@";

    /// <summary>
    /// 获取默认架构名称（SQLite没有架构的概念，使用main）
    /// </summary>
    protected override string DefaultSchemaName => "main";


    /// <summary>
    /// 获取SQLite统计查询
    /// </summary>
    protected override string StatisticsQuery => @"
SELECT
    'SQLite' AS DatabaseName,
    (SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%') AS TableCount,
    (SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%') AS IndexCount;
    
SELECT
    m.name AS TableName,
    0 AS RowCount,  -- 将在代码中单独查询每个表的行数
    (SELECT page_count * page_size / 1024.0 / 1024.0 FROM pragma_page_count(), pragma_page_size() WHERE 1=1) AS SizeMB
FROM
    sqlite_master m
WHERE
    m.type='table' 
    AND m.name NOT LIKE 'sqlite_%'
ORDER BY
    m.name;";


    /// <summary>
    /// SQLite备份实现 - 直接复制文件或使用SQLite备份API
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
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 获取SQLite数据库文件路径
            string? dbFilePath = GetDatabaseFilePath(connectionString);
            if (string.IsNullOrEmpty(dbFilePath))
            {
                return (false, "无法从连接字符串中获取SQLite数据库文件路径");
            }

            // 确保数据库连接关闭
            await dbContext.Database.CloseConnectionAsync();

            // 确保备份目录存在
            string? backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // 方法1: 使用文件复制方式备份
            File.Copy(dbFilePath, backupPath, true);

            // 方法2: 如果需要，可以使用SQLite的备份API进行热备份
            // 此处省略，因为方法1在大多数情况下已足够

            return (true, $"SQLite数据库已成功备份到 {backupPath}");
        }
        catch (Exception ex)
        {
            return (false, $"备份SQLite数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// SQLite恢复实现 - 直接复制文件
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

            // 获取SQLite数据库文件路径
            string? dbFilePath = GetDatabaseFilePath(connectionString);
            if (string.IsNullOrEmpty(dbFilePath))
            {
                return (false, "无法从连接字符串中获取SQLite数据库文件路径");
            }

            // 确保数据库连接关闭
            await dbContext.Database.CloseConnectionAsync();

            // 如果目标数据库文件存在，先删除它
            if (File.Exists(dbFilePath))
            {
                File.Delete(dbFilePath);
            }

            // 复制备份文件到目标位置
            File.Copy(backupPath, dbFilePath);

            return (true, $"SQLite数据库已成功从 {backupPath} 恢复");
        }
        catch (Exception ex)
        {
            return (false, $"恢复SQLite数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从SQLite连接字符串中获取数据库文件路径
    /// </summary>
    private string? GetDatabaseFilePath(string connectionString)
    {
        // 解析连接字符串
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        if (builder.ContainsKey("Data Source"))
        {
            return builder["Data Source"]?.ToString();
        }

        if (builder.ContainsKey("DataSource"))
        {
            return builder["DataSource"]?.ToString();
        }

        return null;
    }

    /// <summary>
    /// 构建获取表记录数的SQL查询
    /// </summary>
    protected override string BuildTableCountQuery(string tableName, string? schemaName)
    {
        // SQLite使用双引号包裹表名
        return $"SELECT COUNT(*) FROM \"{tableName}\"";
    }
}


