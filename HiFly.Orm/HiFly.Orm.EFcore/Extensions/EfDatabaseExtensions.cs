// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace HiFly.Orm.EFcore.Extensions;

/// <summary>
/// EF Core 数据库快速配置扩展方法
/// </summary>
public static class EfDatabaseExtensions
{
    /// <summary>
    /// 快速配置 SQL Server EF Core
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEfSqlServer<TContext>(
        this IServiceCollection services,
        string connectionString,
        Assembly[]? assemblies = null) where TContext : DbContext
    {
        // 注册 DbContextFactory
        services.AddDbContextFactory<TContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(60);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            }));

        // 注册数据服务
        RegisterDataServices<TContext>(services, assemblies);

        return services;
    }

    /// <summary>
    /// 快速配置 PostgreSQL EF Core
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEfPostgreSQL<TContext>(
        this IServiceCollection services,
        string connectionString,
        Assembly[]? assemblies = null) where TContext : DbContext
    {
        // 注册 DbContextFactory
        services.AddDbContextFactory<TContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(60);
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            }));

        // 注册数据服务
        RegisterDataServices<TContext>(services, assemblies);

        return services;
    }

    /// <summary>
    /// 快速配置 SQLite EF Core
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEfSqlite<TContext>(
        this IServiceCollection services,
        string connectionString,
        Assembly[]? assemblies = null) where TContext : DbContext
    {
        // 注册 DbContextFactory
        services.AddDbContextFactory<TContext>(options =>
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(60);
                sqliteOptions.MigrationsHistoryTable("__EFMigrationsHistory");
            }));

        // 注册数据服务
        RegisterDataServices<TContext>(services, assemblies);

        return services;
    }

    /// <summary>
    /// 快速配置 Oracle EF Core
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEfOracle<TContext>(
        this IServiceCollection services,
        string connectionString,
        Assembly[]? assemblies = null) where TContext : DbContext
    {
        // 注册 DbContextFactory
        services.AddDbContextFactory<TContext>(options =>
            options.UseOracle(connectionString, oracleOptions =>
            {
                oracleOptions.CommandTimeout(60);
                oracleOptions.MigrationsHistoryTable("__EFMigrationsHistory");
            }));

        // 注册数据服务
        RegisterDataServices<TContext>(services, assemblies);

        return services;
    }

    /// <summary>
    /// 快速配置 Cosmos DB EF Core
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="assemblies">要扫描的程序集（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEfCosmosDB<TContext>(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        Assembly[]? assemblies = null) where TContext : DbContext
    {
        // 注册 DbContextFactory
        services.AddDbContextFactory<TContext>(options =>
            options.UseCosmos(connectionString, databaseName));

        // 注册数据服务
        RegisterDataServices<TContext>(services, assemblies);

        return services;
    }

    /// <summary>
    /// 通用数据库配置方法
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">DbContext 配置委托</param>
    /// <param name="assemblies">要扫描的程序集（可选）</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEfDatabase<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions,
        Assembly[]? assemblies = null) where TContext : DbContext
    {
        // 注册 DbContextFactory
        services.AddDbContextFactory<TContext>(configureOptions);

        // 注册数据服务
        RegisterDataServices<TContext>(services, assemblies);

        return services;
    }

    /// <summary>
    /// 注册数据服务的辅助方法
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集（可选）</param>
    private static void RegisterDataServices<TContext>(IServiceCollection services, Assembly[]? assemblies) where TContext : DbContext
    {
        if (assemblies?.Length > 0)
        {
            services.AddEfDataServicesFromAssemblies<TContext>(assemblies);
        }
        else
        {
            services.AddAllEfDataServices<TContext>();
        }
    }

}
