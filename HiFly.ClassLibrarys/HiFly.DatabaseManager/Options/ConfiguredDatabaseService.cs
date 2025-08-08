// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HiFly.DatabaseManager.Options;

/// <summary>
/// 配置感知的数据库服务实现
/// </summary>
public class ConfiguredDatabaseService(IDatabaseService databaseService, IOptions<DatabaseOptions> options) : IDatabaseService
{
    private readonly IDatabaseService _databaseService = databaseService;
    private readonly DatabaseOptions _options = options.Value;

    // 测试数据库是否存在
    public Task<(bool Exists, bool CanConnect, string? ErrorMessage)> CheckDatabaseExistsAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.CheckDatabaseExistsAsync<TContext>(connectionString, configureOptions);
    }

    // 创建数据库（如果不存在）- 会创建表结构
    public Task<(bool Success, string? ErrorMessage)> EnsureDatabaseCreatedAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.EnsureDatabaseCreatedAsync<TContext>(connectionString, configureOptions);
    }

    // 创建空数据库（如果不存在）- 只创建数据库，不创建表结构
    public Task<(bool Success, string? ErrorMessage)> EnsureEmptyDatabaseCreatedAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.EnsureEmptyDatabaseCreatedAsync<TContext>(connectionString, configureOptions);
    }


    // 删除数据库（如果存在）
    public Task<(bool Success, string? ErrorMessage)> DeleteDatabaseAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.DeleteDatabaseAsync<TContext>(connectionString, configureOptions);
    }

    // 获取当前数据库的迁移版本
    public Task<(bool Success, string Version, string? ErrorMessage)> GetCurrentMigrationVersionAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.GetCurrentMigrationVersionAsync<TContext>(connectionString, configureOptions);
    }

    // 应用所有待处理的迁移
    public Task<(bool Success, int MigrationsApplied, string? ErrorMessage)> ApplyPendingMigrationsAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.ApplyPendingMigrationsAsync<TContext>(connectionString, configureOptions);
    }

    // 获取所有待处理的迁移
    public Task<(bool Success, IEnumerable<string> PendingMigrations, string? ErrorMessage)> GetPendingMigrationsAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.GetPendingMigrationsAsync<TContext>(connectionString, configureOptions);
    }

    // 执行原始SQL查询并返回结果集
    public Task<(bool Success, string JsonResult, string? ErrorMessage)> ExecuteQueryAsync<TContext>(
        string sql,
        string? connectionString = null,
        object[]? parameters = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.ExecuteQueryAsync<TContext>(connectionString, sql, parameters, configureOptions);
    }

    // 执行原始SQL命令（非查询）
    public Task<(bool Success, int RowsAffected, string? ErrorMessage)> ExecuteNonQueryAsync<TContext>(
        string? connectionString = null,
        string sql = "",
        object[]? parameters = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.ExecuteNonQueryAsync<TContext>(connectionString, sql, parameters, configureOptions);
    }

    // 获取数据库架构信息
    public (bool Success, string SchemaInfo, string? ErrorMessage) GetDatabaseSchemaInfo<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.GetDatabaseSchemaInfo<TContext>(connectionString, configureOptions);
    }

    // 创建数据库上下文实例
    public TContext CreateDbContext<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.CreateDbContext<TContext>(connectionString, configureOptions);
    }

    // 获取数据库统计信息
    public Task<(bool Success, string Statistics, string? ErrorMessage)> GetDatabaseStatisticsAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.GetDatabaseStatisticsAsync<TContext>(connectionString, configureOptions);
    }

    // 备份数据库
    public Task<(bool Success, string? ErrorMessage)> BackupDatabaseAsync<TContext>(
        string? connectionString = null,
        string? backupPath = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;

        if (string.IsNullOrEmpty(backupPath))
        {
            var directory = _options.BackupDirectory;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            backupPath = Path.Combine(directory, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        }

        return _databaseService.BackupDatabaseAsync<TContext>(connectionString, backupPath, configureOptions);
    }

    // 从备份恢复数据库
    public Task<(bool Success, string? ErrorMessage)> RestoreDatabaseAsync<TContext>(
        string? connectionString = null,
        string backupPath = "",
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.RestoreDatabaseAsync<TContext>(connectionString, backupPath, configureOptions);
    }

    // 创建标准的DbContextOptionsBuilder
    public DbContextOptionsBuilder<TContext> CreateStandardOptionsBuilder<TContext>(
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.CreateStandardOptionsBuilder<TContext>(connectionString, configureOptions);
    }

    // 获取数据库中特定表的记录数
    public Task<(bool Success, long RecordCount, string? ErrorMessage)> GetTableRecordCountAsync<TContext>(
        string tableName,
        string? connectionString = null,
        string? schemaName = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        connectionString ??= _options.ConnectionString;
        return _databaseService.GetTableRecordCountAsync<TContext>(connectionString, tableName, schemaName, configureOptions);
    }
}

