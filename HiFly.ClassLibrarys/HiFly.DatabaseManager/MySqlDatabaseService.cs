// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Diagnostics;


namespace HiFly.DatabaseManager;

/// <summary>
/// MySQL 数据库服务实现
/// </summary>
public class MySqlDatabaseService : DatabaseServiceBase
{
    /// <summary>
    /// 获取当前数据库提供程序类型
    /// </summary>
    protected override DatabaseProviderType ProviderType => DatabaseProviderType.MySQL;

    /// <summary>
    /// 配置数据库选项
    /// </summary>
    protected override void ConfigureDbContextOptions<TContext>(
        DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
    {
        // 创建MySQL服务器版本 - 使用固定版本或从连接字符串自动检测
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 31));

        // 或者使用静态方法创建
        // var serverVersion = MySqlServerVersion.AutoDetect(connectionString);

        optionsBuilder.UseMySql(connectionString, serverVersion, mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            mySqlOptions.CommandTimeout(60);
            mySqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
        });
    }

    /// <summary>
    /// 获取参数前缀
    /// </summary>
    protected override string ParameterPrefix => "?";

    /// <summary>
    /// 获取默认架构名称（MySQL没有架构的概念，使用空字符串）
    /// </summary>
    protected override string DefaultSchemaName => "";

    /// <summary>
    /// 获取MySQL统计查询
    /// </summary>
    protected override string StatisticsQuery => @"
SELECT 
    DATABASE() AS DatabaseName,
    COUNT(*) AS TableCount
FROM 
    information_schema.tables 
WHERE 
    table_schema = DATABASE();
    
SELECT 
    table_name AS TableName,
    table_rows AS RowCount,
    ROUND(data_length/1024/1024, 2) AS DataSizeMB,
    ROUND(index_length/1024/1024, 2) AS IndexSizeMB,
    ROUND((data_length + index_length)/1024/1024, 2) AS TotalSizeMB
FROM 
    information_schema.tables
WHERE 
    table_schema = DATABASE()
ORDER BY 
    data_length + index_length DESC;";

    /// <summary>
    /// MySQL备份实现 - 使用mysqldump命令行工具
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

            // 获取数据库名称
            var databaseName = dbContext.Database.GetDbConnection().Database;

            // 解析连接字符串
            var csb = new DbConnectionStringBuilder { ConnectionString = connectionString };

            // 获取连接参数
            string server = csb.ContainsKey("Server") ? csb["Server"]?.ToString() ?? "localhost" : "localhost";
            string port = "3306"; // 默认端口

            // 检查服务器字段是否包含端口信息
            if (server.Contains(':'))
            {
                var parts = server.Split(':');
                server = parts[0];
                port = parts.Length > 1 ? parts[1] : "3306";
            }

            string user = csb.ContainsKey("User ID")
                ? csb["User ID"]?.ToString()
                : csb.ContainsKey("Uid")
                    ? csb["Uid"]?.ToString()
                    : "root";

            string password = csb.ContainsKey("Password")
                ? csb["Password"]?.ToString()
                : csb.ContainsKey("Pwd")
                    ? csb["Pwd"]?.ToString()
                    : "";

            // 确保备份目录存在
            string? backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // 执行mysqldump命令
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "mysqldump",
                Arguments = $"-h {server} -P {port} -u {user} {(string.IsNullOrEmpty(password) ? "" : $"-p{password}")} --result-file=\"{backupPath}\" --databases {databaseName}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return (false, $"备份MySQL数据库失败，退出代码: {process.ExitCode}, 错误: {error}");
            }

            return (true, $"数据库 {databaseName} 已成功备份到 {backupPath}");
        }
        catch (Exception ex)
        {
            return (false, $"备份MySQL数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// MySQL恢复实现 - 使用mysql命令行工具
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

            // 解析连接字符串
            var csb = new DbConnectionStringBuilder { ConnectionString = connectionString };

            // 获取连接参数
            string server = csb.ContainsKey("Server") ? csb["Server"]?.ToString() ?? "localhost" : "localhost";
            string port = "3306"; // 默认端口

            // 检查服务器字段是否包含端口信息
            if (server.Contains(':'))
            {
                var parts = server.Split(':');
                server = parts[0];
                port = parts.Length > 1 ? parts[1] : "3306";
            }

            string user = csb.ContainsKey("User ID")
                ? csb["User ID"]?.ToString()
                : csb.ContainsKey("Uid")
                    ? csb["Uid"]?.ToString()
                    : "root";

            string password = csb.ContainsKey("Password")
                ? csb["Password"]?.ToString()
                : csb.ContainsKey("Pwd")
                    ? csb["Pwd"]?.ToString()
                    : "";

            // 先删除并重建数据库
            await DeleteAndRecreateDatabaseAsync(dbContext, databaseName);

            // 执行mysql命令恢复数据
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "mysql",
                Arguments = $"-h {server} -P {port} -u {user} {(string.IsNullOrEmpty(password) ? "" : $"-p{password}")} {databaseName} < \"{backupPath}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return (false, $"恢复MySQL数据库失败，退出代码: {process.ExitCode}");
            }

            return (true, $"数据库 {databaseName} 已成功从 {backupPath} 恢复");
        }
        catch (Exception ex)
        {
            return (false, $"恢复MySQL数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除并重建数据库
    /// </summary>
    private async Task DeleteAndRecreateDatabaseAsync<TContext>(TContext dbContext, string databaseName)
        where TContext : DbContext
    {
        // 获取连接
        var connection = dbContext.Database.GetDbConnection();

        // 修改连接字符串，连接到系统数据库
        var builder = new DbConnectionStringBuilder { ConnectionString = connection.ConnectionString };
        builder["Database"] = "mysql";
        connection.ConnectionString = builder.ToString();

        // 删除并重建数据库
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS `{databaseName}`; CREATE DATABASE `{databaseName}`;";
        await command.ExecuteNonQueryAsync();

        // 关闭连接
        await connection.CloseAsync();

        // 恢复原连接字符串
        builder["Database"] = databaseName;
        connection.ConnectionString = builder.ToString();
    }

    /// <summary>
    /// 构建获取表记录数的SQL查询
    /// </summary>
    protected override string BuildTableCountQuery(string tableName, string? schemaName)
    {
        // MySQL使用反引号包裹标识符
        return $"SELECT COUNT(*) FROM `{tableName}`";
    }
}
