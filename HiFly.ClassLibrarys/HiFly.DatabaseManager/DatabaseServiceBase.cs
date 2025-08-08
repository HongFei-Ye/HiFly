// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace HiFly.DatabaseManager;

/// <summary>
/// 数据库服务基类，实现公共功能
/// </summary>
public abstract class DatabaseServiceBase : IDatabaseService
{
    /// <summary>
    /// 获取当前数据库提供程序类型
    /// </summary>
    protected abstract DatabaseProviderType ProviderType { get; }

    /// <summary>
    /// 配置数据库选项
    /// </summary>
    /// <typeparam name="TContext">DbContext类型</typeparam>
    /// <param name="optionsBuilder">选项构建器</param>
    /// <param name="connectionString">连接字符串</param>
    protected abstract void ConfigureDbContextOptions<TContext>(
        DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
        where TContext : DbContext;

    /// <summary>
    /// 获取参数前缀（例如@、:或?）
    /// </summary>
    protected abstract string ParameterPrefix { get; }

    /// <summary>
    /// 获取默认的架构名称
    /// </summary>
    protected abstract string DefaultSchemaName { get; }

    /// <summary>
    /// 获取用于统计数据库信息的SQL查询
    /// </summary>
    protected abstract string StatisticsQuery { get; }

    /// <summary>
    /// 测试数据库是否存在
    /// </summary>
    public async Task<(bool Exists, bool CanConnect, string? ErrorMessage)> CheckDatabaseExistsAsync<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, false, "连接字符串不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 检查数据库是否存在
            bool exists = await dbContext.Database.CanConnectAsync();

            if (exists)
            {
                try
                {
                    // 测试实际连接
                    await dbContext.Database.OpenConnectionAsync();
                    await dbContext.Database.CloseConnectionAsync();
                    return (true, true, null);
                }
                catch (Exception connEx)
                {
                    return (true, false, $"数据库存在但无法连接: {connEx.Message}");
                }
            }

            return (false, false, "数据库不存在");
        }
        catch (Exception ex)
        {
            return (false, false, $"检查数据库状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建数据库（如果不存在）- 会创建表结构
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> EnsureDatabaseCreatedAsync<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "连接字符串不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 创建数据库（如果不存在）
            bool created = await dbContext.Database.EnsureCreatedAsync();

            return (true, created ? "数据库已成功创建" : "数据库已存在，无需创建");
        }
        catch (Exception ex)
        {
            return (false, $"创建数据库失败: {ex.Message}");
        }
    }


    /// <summary>
    /// 创建空数据库（如果不存在）- 只创建数据库，不创建表结构
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> EnsureEmptyDatabaseCreatedAsync<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "连接字符串不能为空或空白。");

        try
        {
            // 创建一个上下文实例以获取连接
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);
            var connection = dbContext.Database.GetDbConnection();

            // 获取数据库名称
            string databaseName = connection.Database;

            // 修改连接字符串以连接到主/系统数据库
            var masterConnection = CreateMasterConnection(connection);

            try
            {
                // 确保连接打开
                await masterConnection.OpenAsync();

                // 检查数据库是否存在
                using (var command = masterConnection.CreateCommand())
                {
                    command.CommandText = GetDatabaseExistsQuery(databaseName);
                    var result = await command.ExecuteScalarAsync();
                    bool exists = Convert.ToInt32(result ?? 0) > 0;

                    if (!exists)
                    {
                        // 数据库不存在，创建它
                        using var createCommand = masterConnection.CreateCommand();
                        createCommand.CommandText = GetCreateDatabaseQuery(databaseName);
                        await createCommand.ExecuteNonQueryAsync();
                        return (true, $"空数据库 '{databaseName}' 已成功创建");
                    }
                    else
                    {
                        // 数据库已存在
                        return (true, $"数据库 '{databaseName}' 已存在，无需创建");
                    }
                }
            }
            finally
            {
                if (masterConnection.State == ConnectionState.Open)
                    await masterConnection.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            return (false, $"创建空数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建连接到主系统数据库的连接
    /// </summary>
    protected virtual DbConnection CreateMasterConnection(DbConnection originalConnection)
    {
        // 创建连接字符串构建器
        var builder = new DbConnectionStringBuilder { ConnectionString = originalConnection.ConnectionString };

        // 根据数据库提供程序类型修改连接字符串
        switch (ProviderType)
        {
            case DatabaseProviderType.SqlServer:
                builder.Remove("Initial Catalog");
                builder.Remove("Database");
                builder["Initial Catalog"] = "master";
                break;
            case DatabaseProviderType.MySQL:
                builder.Remove("Database");
                builder["Database"] = "mysql";
                break;
            case DatabaseProviderType.PostgreSQL:
                builder.Remove("Database");
                builder["Database"] = "postgres";
                break;
            case DatabaseProviderType.SQLite:
                // SQLite 不需要修改连接字符串，因为它是文件数据库
                // 但我们仍需要创建一个新连接
                break;
            default:
                throw new NotSupportedException($"不支持的数据库提供程序类型: {ProviderType}");
        }

        // 创建新连接
        var connectionTypeName = originalConnection.GetType().FullName;
        var connection = (DbConnection)Activator.CreateInstance(originalConnection.GetType(), builder.ConnectionString)!;

        return connection;
    }

    /// <summary>
    /// 获取检查数据库是否存在的SQL查询
    /// </summary>
    protected virtual string GetDatabaseExistsQuery(string databaseName)
    {
        // 根据数据库提供程序类型返回适当的SQL
        switch (ProviderType)
        {
            case DatabaseProviderType.SqlServer:
                return $"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'";
            case DatabaseProviderType.MySQL:
                return $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{databaseName}'";
            case DatabaseProviderType.PostgreSQL:
                return $"SELECT COUNT(*) FROM pg_database WHERE datname = '{databaseName}'";
            case DatabaseProviderType.SQLite:
                // SQLite 不需要检查数据库是否存在，因为连接到文件时会自动创建
                return "SELECT 0";
            default:
                throw new NotSupportedException($"不支持的数据库提供程序类型: {ProviderType}");
        }
    }

    /// <summary>
    /// 获取创建数据库的SQL命令
    /// </summary>
    protected virtual string GetCreateDatabaseQuery(string databaseName)
    {
        // 根据数据库提供程序类型返回适当的SQL
        switch (ProviderType)
        {
            case DatabaseProviderType.SqlServer:
                return $"CREATE DATABASE [{databaseName}]";
            case DatabaseProviderType.MySQL:
                return $"CREATE DATABASE `{databaseName}`";
            case DatabaseProviderType.PostgreSQL:
                return $"CREATE DATABASE \"{databaseName}\"";
            case DatabaseProviderType.SQLite:
                // 对于SQLite，我们不需要专门的创建数据库命令
                return string.Empty;
            default:
                throw new NotSupportedException($"不支持的数据库提供程序类型: {ProviderType}");
        }
    }










    /// <summary>
    /// 删除数据库（如果存在）
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> DeleteDatabaseAsync<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "连接字符串不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 删除数据库（如果存在）
            bool deleted = await dbContext.Database.EnsureDeletedAsync();

            return (true, deleted ? "数据库已成功删除" : "数据库不存在，无法删除");
        }
        catch (Exception ex)
        {
            return (false, $"删除数据库失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取当前数据库的迁移版本
    /// </summary>
    public async Task<(bool Success, string Version, string? ErrorMessage)> GetCurrentMigrationVersionAsync<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, string.Empty, "连接字符串不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 获取应用的迁移列表
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

            // 如果没有应用的迁移，返回特定消息
            if (!appliedMigrations.Any())
            {
                return (true, "未应用任何迁移", null);
            }

            // 返回最新的迁移版本
            var latestMigration = appliedMigrations.Last();
            return (true, latestMigration, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"获取迁移版本失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用所有待处理的迁移
    /// </summary>
    public async Task<(bool Success, int MigrationsApplied, string? ErrorMessage)> ApplyPendingMigrationsAsync<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, 0, "连接字符串不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 获取已应用的迁移
            var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();

            // 获取所有本地迁移（使用反射获取迁移程序集中的迁移）
            var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
            var availableMigrations = migrationsAssembly.Migrations.Keys.ToList();

            // 计算待处理的迁移数量
            var pendingMigrations = availableMigrations.Except(appliedMigrations).ToList();

            if (!pendingMigrations.Any())
            {
                return (true, 0, "没有待处理的迁移");
            }

            // 应用所有迁移
            await dbContext.Database.MigrateAsync();

            return (true, pendingMigrations.Count, $"已成功应用 {pendingMigrations.Count} 个迁移");
        }
        catch (Exception ex)
        {
            return (false, 0, $"应用迁移失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取所有待处理的迁移
    /// </summary>
    public async Task<(bool Success, IEnumerable<string> PendingMigrations, string? ErrorMessage)> GetPendingMigrationsAsync<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, Array.Empty<string>(), "连接字符串不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 获取已应用的迁移
            var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();

            // 获取所有本地迁移
            var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
            var availableMigrations = migrationsAssembly.Migrations.Keys.ToList();

            // 计算待处理的迁移
            var pendingMigrations = availableMigrations.Except(appliedMigrations).ToList();

            return (true, pendingMigrations, null);
        }
        catch (Exception ex)
        {
            return (false, Array.Empty<string>(), $"获取待处理迁移失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 执行原始SQL查询并返回结果集
    /// </summary>
    public async Task<(bool Success, string JsonResult, string? ErrorMessage)> ExecuteQueryAsync<TContext>(
        string? connectionString,
        string sql,
        object[]? parameters = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, string.Empty, "连接字符串不能为空或空白。");
        if (string.IsNullOrWhiteSpace(sql))
            return (false, string.Empty, "SQL查询不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);
            var connection = dbContext.Database.GetDbConnection();

            // 确保连接打开
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // 创建命令
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            // 添加参数（如果有）
            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"{ParameterPrefix}p{i}";
                    parameter.Value = parameters[i] ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }
            }

            // 执行查询
            using var reader = await command.ExecuteReaderAsync();

            // 将结果转换为列表
            var results = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.GetValue(i);
                    if (value == DBNull.Value)
                    {
                        // 对于 DBNull，使用类型的默认值
                        row[columnName] = default!;
                    }
                    else
                    {
                        // 对于非空值，直接使用
                        row[columnName] = value;
                    }
                }
                results.Add(row);
            }

            // 序列化结果为JSON
            var jsonResult = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return (true, jsonResult, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"执行SQL查询失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 执行原始SQL命令（非查询）
    /// </summary>
    public async Task<(bool Success, int RowsAffected, string? ErrorMessage)> ExecuteNonQueryAsync<TContext>(
        string? connectionString,
        string sql,
        object[]? parameters = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, 0, "连接字符串不能为空或空白。");
        if (string.IsNullOrWhiteSpace(sql))
            return (false, 0, "SQL命令不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 转换参数（如果有）
            var sqlParameters = parameters ?? Array.Empty<object>();

            // 执行命令
            int rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(sql, sqlParameters);

            return (true, rowsAffected, null);
        }
        catch (Exception ex)
        {
            return (false, 0, $"执行SQL命令失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取数据库架构信息
    /// </summary>
    public (bool Success, string SchemaInfo, string? ErrorMessage) GetDatabaseSchemaInfo<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, string.Empty, "连接字符串不能为空或空白。");

        try
        {
            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);

            // 收集实体类型信息
            var entityTypes = dbContext.Model.GetEntityTypes();
            var schemaInfo = new List<Dictionary<string, object>>();

            foreach (var entityType in entityTypes)
            {
                var entityInfo = new Dictionary<string, object>
                {
                    ["EntityName"] = entityType.Name,
                    ["TableName"] = entityType.GetTableName() ?? "未知",
                    ["Schema"] = entityType.GetSchema() ?? DefaultSchemaName,
                    ["PrimaryKey"] = string.Join(", ", entityType.FindPrimaryKey()?.Properties.Select(p => p.Name) ?? Enumerable.Empty<string>()),
                    ["Properties"] = entityType.GetProperties().Select(p => new
                    {
                        Name = p.Name,
                        Type = p.ClrType.Name,
                        IsRequired = !p.IsNullable,
                        IsKey = entityType.FindPrimaryKey()?.Properties.Contains(p) == true,
                        IsForeignKey = entityType.GetForeignKeys().Any(fk => fk.Properties.Contains(p))
                    }).ToList()
                };

                schemaInfo.Add(entityInfo);
            }

            // 序列化为JSON
            var jsonResult = JsonSerializer.Serialize(schemaInfo, new JsonSerializerOptions { WriteIndented = true });
            return (true, jsonResult, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"获取数据库架构信息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建数据库上下文实例
    /// </summary>
    public TContext CreateDbContext<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("连接字符串不能为空或空白。", nameof(connectionString));

        // 创建 DbContextOptionsBuilder
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        // 配置数据库选项
        ConfigureDbContextOptions(optionsBuilder, connectionString);

        // 应用额外配置
        configureOptions?.Invoke(optionsBuilder);

        var options = optionsBuilder.Options;

        // 创建上下文实例
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    /// <summary>
    /// 获取数据库统计信息
    /// </summary>
    public async Task<(bool Success, string Statistics, string? ErrorMessage)> GetDatabaseStatisticsAsync<TContext>(
        string? connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        try
        {
            // 如果没有统计查询，返回错误
            if (string.IsNullOrWhiteSpace(StatisticsQuery))
            {
                return (false, string.Empty, $"{ProviderType} 提供程序不支持获取数据库统计信息");
            }

            // 执行统计查询
            var queryResult = await ExecuteQueryAsync<TContext>(connectionString, StatisticsQuery, null, configureOptions);
            if (!queryResult.Success)
            {
                return (false, string.Empty, $"获取数据库统计信息失败: {queryResult.ErrorMessage}");
            }

            return (true, queryResult.JsonResult, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"获取数据库统计信息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 备份数据库（需要子类实现）
    /// </summary>
    public abstract Task<(bool Success, string? ErrorMessage)> BackupDatabaseAsync<TContext>(
        string? connectionString,
        string backupPath,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 从备份恢复数据库（需要子类实现）
    /// </summary>
    public abstract Task<(bool Success, string? ErrorMessage)> RestoreDatabaseAsync<TContext>(
        string? connectionString,
        string backupPath,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 创建标准的DbContextOptionsBuilder
    /// </summary>
    public DbContextOptionsBuilder<TContext> CreateStandardOptionsBuilder<TContext>(
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        // 配置数据库选项
        ConfigureDbContextOptions(optionsBuilder, connectionString);

        // 应用额外配置
        configureOptions?.Invoke(optionsBuilder);

        return optionsBuilder;
    }

    /// <summary>
    /// 获取数据库中特定表的记录数
    /// </summary>
    public async Task<(bool Success, long RecordCount, string? ErrorMessage)> GetTableRecordCountAsync<TContext>(
        string? connectionString,
        string tableName,
        string? schemaName = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, 0, "连接字符串不能为空或空白。");
        if (string.IsNullOrWhiteSpace(tableName))
            return (false, 0, "表名不能为空或空白。");

        // 使用默认架构（如果未指定）
        schemaName = schemaName ?? DefaultSchemaName;

        try
        {
            // 根据数据库类型构造SQL查询
            string sqlQuery = BuildTableCountQuery(tableName, schemaName);

            // 创建上下文实例
            using var dbContext = CreateDbContext<TContext>(connectionString, configureOptions);
            var connection = dbContext.Database.GetDbConnection();

            // 确保连接打开
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            // 创建命令
            using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;

            // 执行查询并获取结果
            var result = await command.ExecuteScalarAsync();
            if (result == null)
                return (false, 0, "查询返回空结果");

            return (true, Convert.ToInt64(result), null);
        }
        catch (Exception ex)
        {
            return (false, 0, $"获取表记录数失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 构建获取表记录数的SQL查询
    /// </summary>
    protected virtual string BuildTableCountQuery(string tableName, string? schemaName)
    {
        // 默认使用SQL Server样式
        if (!string.IsNullOrEmpty(schemaName))
        {
            return $"SELECT COUNT(*) FROM [{schemaName}].[{tableName}]";
        }
        else
        {
            return $"SELECT COUNT(*) FROM [{tableName}]";
        }
    }
}
