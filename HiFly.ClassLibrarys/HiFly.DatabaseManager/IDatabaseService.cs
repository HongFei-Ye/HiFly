// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;

namespace HiFly.DatabaseManager;

/// <summary>
/// 数据库服务接口
/// </summary>
public interface IDatabaseService
{
    #region 数据库创建与管理

    /// <summary>
    /// 测试数据库是否存在
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含数据库状态信息的元组：是否存在、是否可连接、错误消息（如果有）</returns>
    Task<(bool Exists, bool CanConnect, string? ErrorMessage)> CheckDatabaseExistsAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 创建数据库（如果不存在）- 会创建表结构
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含操作结果的元组：是否成功、错误消息（如果有）</returns>
    Task<(bool Success, string? ErrorMessage)> EnsureDatabaseCreatedAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 创建空数据库（如果不存在）- 只创建数据库，不创建表结构
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含操作结果的元组：是否成功、错误消息（如果有）</returns>
    Task<(bool Success, string? ErrorMessage)> EnsureEmptyDatabaseCreatedAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 删除数据库（如果存在）
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含操作结果的元组：是否成功、错误消息（如果有）</returns>
    Task<(bool Success, string? ErrorMessage)> DeleteDatabaseAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    #endregion

    #region 迁移与架构管理

    /// <summary>
    /// 获取当前数据库的迁移版本
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>迁移版本信息（如果成功）或错误信息</returns>
    Task<(bool Success, string Version, string? ErrorMessage)> GetCurrentMigrationVersionAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 应用所有待处理的迁移
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含操作结果的元组：是否成功、应用的迁移数量、错误消息（如果有）</returns>
    Task<(bool Success, int MigrationsApplied, string? ErrorMessage)> ApplyPendingMigrationsAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 获取所有待处理的迁移
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含操作结果的元组：是否成功、待处理的迁移列表、错误消息（如果有）</returns>
    Task<(bool Success, IEnumerable<string> PendingMigrations, string? ErrorMessage)> GetPendingMigrationsAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    #endregion

    #region SQL执行与性能优化

    /// <summary>
    /// 执行原始SQL查询并返回结果集
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="sql">要执行的SQL命令</param>
    /// <param name="parameters">SQL参数</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含查询结果的元组：是否成功、结果集（JSON格式）、错误消息（如果有）</returns>
    Task<(bool Success, string JsonResult, string? ErrorMessage)> ExecuteQueryAsync<TContext>(
        string sql,
        string? connectionString = null,
        object[]? parameters = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 执行原始SQL命令（非查询）
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="sql">要执行的SQL命令</param>
    /// <param name="parameters">SQL参数</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含执行结果的元组：是否成功、影响的行数、错误消息（如果有）</returns>
    Task<(bool Success, int RowsAffected, string? ErrorMessage)> ExecuteNonQueryAsync<TContext>(
        string sql,
        string? connectionString = null,
        object[]? parameters = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    #endregion

    #region 数据库信息与诊断

    /// <summary>
    /// 获取数据库架构信息
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含数据库架构信息的元组：是否成功、架构信息（JSON格式）、错误消息（如果有）</returns>
    (bool Success, string SchemaInfo, string? ErrorMessage) GetDatabaseSchemaInfo<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 创建数据库上下文实例
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>数据库上下文实例</returns>
    TContext CreateDbContext<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 获取数据库统计信息
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含数据库统计信息的元组：是否成功、统计信息（JSON格式）、错误消息（如果有）</returns>
    Task<(bool Success, string Statistics, string? ErrorMessage)> GetDatabaseStatisticsAsync<TContext>(
        string? connectionString = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    #endregion

    #region 备份与恢复

    /// <summary>
    /// 备份数据库
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="backupPath">备份文件路径</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含备份结果的元组：是否成功、错误消息（如果有）</returns>
    Task<(bool Success, string? ErrorMessage)> BackupDatabaseAsync<TContext>(
        string? connectionString = null,
        string? backupPath = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 从备份恢复数据库
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="backupPath">备份文件路径</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含恢复结果的元组：是否成功、错误消息（如果有）</returns>
    Task<(bool Success, string? ErrorMessage)> RestoreDatabaseAsync<TContext>(
        string? connectionString = null,
        string? backupPath = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    #endregion

    #region 通用DbContext操作辅助方法

    /// <summary>
    /// 创建标准的DbContextOptionsBuilder
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>配置好的DbContextOptionsBuilder</returns>
    DbContextOptionsBuilder<TContext> CreateStandardOptionsBuilder<TContext>(
        string connectionString,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    /// <summary>
    /// 获取数据库中特定表的记录数
    /// </summary>
    /// <typeparam name="TContext">DbContext 的类型</typeparam>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="tableName">表名</param>
    /// <param name="schemaName">架构名（默认为dbo）</param>
    /// <param name="configureOptions">配置 DbContextOptionsBuilder 的委托</param>
    /// <returns>包含记录数的元组：是否成功、记录数、错误消息（如果有）</returns>
    Task<(bool Success, long RecordCount, string? ErrorMessage)> GetTableRecordCountAsync<TContext>(
        string tableName,
        string? connectionString = null,
        string? schemaName = null,
        Action<DbContextOptionsBuilder>? configureOptions = null)
        where TContext : DbContext;

    #endregion

}
