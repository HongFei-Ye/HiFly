// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Diagnostics;

namespace HiFly.DatabaseManager;

/// <summary>
/// PostgreSQL 数据库服务实现
/// </summary>
public class PostgreSqlDatabaseService : DatabaseServiceBase
{
    /// <summary>
    /// 获取当前数据库提供程序类型
    /// </summary>
    protected override DatabaseProviderType ProviderType => DatabaseProviderType.PostgreSQL;

    /// <summary>
    /// 配置数据库选项
    /// </summary>
    protected override void ConfigureDbContextOptions<TContext>(
        DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
    {
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(60);
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });
    }

    /// <summary>
    /// 获取参数前缀
    /// </summary>
    protected override string ParameterPrefix => "@";

    /// <summary>
    /// 获取默认架构名称
    /// </summary>
    protected override string DefaultSchemaName => "public";

    /// <summary>
    /// 获取PostgreSQL统计查询
    /// </summary>
    protected override string StatisticsQuery => @"
SELECT 
    current_database() AS DatabaseName,
    pg_database_size(current_database())/1024/1024 AS DatabaseSizeMB,
    (SELECT COUNT(*) FROM pg_tables WHERE schemaname = 'public') AS TableCount,
    (SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public') AS IndexCount;

SELECT
    tablename AS TableName,
    (SELECT reltuples::bigint FROM pg_class WHERE oid = (quote_ident(schemaname) || '.' || quote_ident(tablename))::regclass) AS ApproxRowCount,
    pg_total_relation_size(quote_ident(schemaname) || '.' || quote_ident(tablename))/1024/1024 AS TotalSizeMB
FROM 
    pg_tables
WHERE 
    schemaname = 'public'
ORDER BY 
    pg_total_relation_size(quote_ident(schemaname) || '.' || quote_ident(tablename)) DESC;";

    /// <summary>
    /// PostgreSQL备份实现 - 使用pg_dump命令行工具
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
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            // 获取连接参数
            string host = builder.ContainsKey("Host")
                ? builder["Host"]?.ToString() ?? "localhost"
                : "localhost";

            string port = builder.ContainsKey("Port")
                ? builder["Port"]?.ToString() ?? "5432"
                : "5432";

            string username = builder.ContainsKey("Username")
                ? builder["Username"]?.ToString()
                : builder.ContainsKey("User ID")
                    ? builder["User ID"]?.ToString()
                    : "postgres";

            string password = builder.ContainsKey("Password")
                ? builder["Password"]?.ToString() ?? ""
                : "";

            // 确保备份目录存在
            string? backupDir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // 构建pg_dump命令
            string pgDumpCmd = $"pg_dump -h {host} -p {port} -U {username} -F c -b -v -f \"{backupPath}\" {databaseName}";

            // 使用临时环境变量传递密码
            var envVars = new Dictionary<string, string?>
            {
                { "PGPASSWORD", password }
            };

            // 执行pg_dump命令
            var (exitCode, stdOut, stdErr) = await RunProcessAsync("pg_dump",
                $"-h {host} -p {port} -U {username} -F c -b -v -f \"{backupPath}\" {databaseName}",
                envVars);

            if (exitCode != 0)
            {
                return (false, $"pg_dump 退出代码: {exitCode}, 错误: {stdErr}");
            }

            return (true, $"数据库 {databaseName} 已成功备份到 {backupPath}");
        }
        catch (Exception ex)
        {
            return (false, $"备份PostgreSQL数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// PostgreSQL恢复实现 - 使用pg_restore命令行工具
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
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            // 获取连接参数
            string host = builder.ContainsKey("Host")
                ? builder["Host"]?.ToString() ?? "localhost"
                : "localhost";

            string port = builder.ContainsKey("Port")
                ? builder["Port"]?.ToString() ?? "5432"
                : "5432";

            string username = builder.ContainsKey("Username")
                ? builder["Username"]?.ToString()
                : builder.ContainsKey("User ID")
                    ? builder["User ID"]?.ToString()
                    : "postgres";

            string password = builder.ContainsKey("Password")
                ? builder["Password"]?.ToString() ?? ""
                : "";

            // 先删除现有数据库
            await dbContext.Database.EnsureDeletedAsync();

            // 创建新的空数据库
            await dbContext.Database.EnsureCreatedAsync();

            // 使用临时环境变量传递密码
            var envVars = new Dictionary<string, string?>
            {
                { "PGPASSWORD", password }
            };

            // 执行pg_restore命令
            var (exitCode, stdOut, stdErr) = await RunProcessAsync("pg_restore",
                $"-h {host} -p {port} -U {username} -d {databaseName} \"{backupPath}\"",
                envVars);

            if (exitCode != 0 && !stdErr.Contains("but no errors were detected"))
            {
                return (false, $"pg_restore 退出代码: {exitCode}, 错误: {stdErr}");
            }

            return (true, $"数据库 {databaseName} 已成功从 {backupPath} 恢复");
        }
        catch (Exception ex)
        {
            return (false, $"恢复PostgreSQL数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 构建获取表记录数的SQL查询
    /// </summary>
    protected override string BuildTableCountQuery(string tableName, string? schemaName)
    {
        if (!string.IsNullOrEmpty(schemaName))
        {
            return $"SELECT COUNT(*) FROM \"{schemaName}\".\"{tableName}\"";
        }
        else
        {
            return $"SELECT COUNT(*) FROM \"{tableName}\"";
        }
    }

    /// <summary>
    /// 执行外部进程并等待完成
    /// </summary>
    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName,
        string arguments,
        Dictionary<string, string?>? environmentVars = null)
    {
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // 设置环境变量
        if (environmentVars != null)
        {
            foreach (var kvp in environmentVars)
            {
                if (kvp.Value != null)
                {
                    process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }
        }

        process.Start();

        // 异步读取输出
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        // 等待读取完成
        string stdOut = await outputTask;
        string stdErr = await errorTask;

        return (process.ExitCode, stdOut, stdErr);
    }
}
