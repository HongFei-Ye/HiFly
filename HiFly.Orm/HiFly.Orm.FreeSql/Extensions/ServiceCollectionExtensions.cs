// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using FreeSql;
using HiFly.Tables.Core.Interfaces;
using HiFly.Orm.FreeSql.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace HiFly.Orm.FreeSql.Extensions;

/// <summary>
/// FreeSql 服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 为指定实体添加 FreeSql 数据服务
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlDataService<TItem>(this IServiceCollection services)
        where TItem : class, new()
    {
        services.AddScoped<IHiFlyDataService<TItem>, FreeSqlDataService<TItem>>();
        return services;
    }

    /// <summary>
    /// 自动注册所有实体的 FreeSql 数据服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAllFreeSqlDataServices(
        this IServiceCollection services,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger("FreeSqlDataServiceRegistration") ??
                     CreateConsoleLogger("FreeSqlDataServiceRegistration");

        logger.LogInformation("开始扫描程序集注册 FreeSqlDataService");

        var entityTypes = new List<Type>();

        // 扫描指定程序集中的实体类型
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .Where(t => HasIdProperty(t)) // 简单判断：有Id属性的类视为实体
                .ToList();

            entityTypes.AddRange(types);
            logger.LogInformation("程序集 {Assembly} 发现 {Count} 个实体类型", assembly.GetName().Name, types.Count);
        }

        logger.LogInformation("总共发现 {TotalCount} 个实体类型", entityTypes.Count);

        // 注册每个实体的数据服务
        foreach (var entityType in entityTypes)
        {
            try
            {
                // 构造泛型方法
                var method = typeof(ServiceCollectionExtensions)
                    .GetMethod(nameof(AddFreeSqlDataService))!
                    .MakeGenericMethod(entityType);

                // 调用注册方法
                method.Invoke(null, new object[] { services });

                logger.LogDebug("已注册 FreeSqlDataService<{EntityType}>", entityType.Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "注册实体 {EntityType} 的 FreeSqlDataService 时发生错误", entityType.Name);
            }
        }

        logger.LogInformation("FreeSqlDataService 注册完成，成功注册 {Count} 个服务", entityTypes.Count);
        return services;
    }

    /// <summary>
    /// 添加 FreeSql 实例和所有数据服务（简化版本）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="freeSqlBuilder">FreeSql 构建器委托</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlWithDataServices(
        this IServiceCollection services,
        Func<FreeSqlBuilder> freeSqlBuilder,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        // 注册 FreeSql 实例
        services.AddSingleton<IFreeSql>(provider =>
        {
            var freeSql = freeSqlBuilder().Build();
            
            // 自动同步结构（开发环境）
            freeSql.CodeFirst.SyncStructure(GetEntityTypes(assemblies));
            
            return freeSql;
        });

        // 注册所有数据服务
        services.AddAllFreeSqlDataServices(assemblies, loggerFactory);

        return services;
    }

    /// <summary>
    /// 添加 FreeSql 实例和数据服务（使用现有的 FreeSql 实例）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="freeSql">FreeSql 实例</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlWithDataServices(
        this IServiceCollection services,
        IFreeSql freeSql,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        // 注册 FreeSql 实例
        services.AddSingleton(freeSql);

        // 注册所有数据服务
        services.AddAllFreeSqlDataServices(assemblies, loggerFactory);

        return services;
    }

    /// <summary>
    /// 快速配置 SQLite FreeSql
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlSqlite(
        this IServiceCollection services,
        string connectionString,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        return services.AddFreeSqlWithDataServices(
            () => new FreeSqlBuilder()
                .UseConnectionString(DataType.Sqlite, connectionString)
                .UseAutoSyncStructure(true)
                .UseGenerateCommandParameterWithLambda(true),
            assemblies,
            loggerFactory);
    }

    /// <summary>
    /// 快速配置 MySQL FreeSql
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlMySql(
        this IServiceCollection services,
        string connectionString,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        return services.AddFreeSqlWithDataServices(
            () => new FreeSqlBuilder()
                .UseConnectionString(DataType.MySql, connectionString)
                .UseAutoSyncStructure(true)
                .UseGenerateCommandParameterWithLambda(true),
            assemblies,
            loggerFactory);
    }

    /// <summary>
    /// 快速配置 SQL Server FreeSql
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlSqlServer(
        this IServiceCollection services,
        string connectionString,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        return services.AddFreeSqlWithDataServices(
            () => new FreeSqlBuilder()
                .UseConnectionString(DataType.SqlServer, connectionString)
                .UseAutoSyncStructure(true)
                .UseGenerateCommandParameterWithLambda(true),
            assemblies,
            loggerFactory);
    }

    /// <summary>
    /// 快速配置 PostgreSQL FreeSql
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlPostgreSQL(
        this IServiceCollection services,
        string connectionString,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        return services.AddFreeSqlWithDataServices(
            () => new FreeSqlBuilder()
                .UseConnectionString(DataType.PostgreSQL, connectionString)
                .UseAutoSyncStructure(true)
                .UseGenerateCommandParameterWithLambda(true),
            assemblies,
            loggerFactory);
    }

    /// <summary>
    /// 快速配置 Oracle FreeSql
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlOracle(
        this IServiceCollection services,
        string connectionString,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        return services.AddFreeSqlWithDataServices(
            () => new FreeSqlBuilder()
                .UseConnectionString(DataType.Oracle, connectionString)
                .UseAutoSyncStructure(true)
                .UseGenerateCommandParameterWithLambda(true),
            assemblies,
            loggerFactory);
    }

    /// <summary>
    /// 快速配置 达梦数据库 FreeSql
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlDameng(
        this IServiceCollection services,
        string connectionString,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        return services.AddFreeSqlWithDataServices(
            () => new FreeSqlBuilder()
                .UseConnectionString(DataType.Dameng, connectionString)
                .UseAutoSyncStructure(true)
                .UseGenerateCommandParameterWithLambda(true),
            assemblies,
            loggerFactory);
    }

    /// <summary>
    /// 快速配置 人大金仓 FreeSql
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddFreeSqlKingbaseES(
        this IServiceCollection services,
        string connectionString,
        Assembly[] assemblies,
        ILoggerFactory? loggerFactory = null)
    {
        return services.AddFreeSqlWithDataServices(
            () => new FreeSqlBuilder()
                .UseConnectionString(DataType.KingbaseES, connectionString)
                .UseAutoSyncStructure(true)
                .UseGenerateCommandParameterWithLambda(true),
            assemblies,
            loggerFactory);
    }

    /// <summary>
    /// 检查类型是否有Id属性
    /// </summary>
    private static bool HasIdProperty(Type type)
    {
        return type.GetProperty("Id") != null;
    }

    /// <summary>
    /// 获取程序集中的实体类型
    /// </summary>
    private static Type[] GetEntityTypes(Assembly[] assemblies)
    {
        var entityTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .Where(t => HasIdProperty(t))
                .ToArray();

            entityTypes.AddRange(types);
        }

        return entityTypes.ToArray();
    }

    /// <summary>
    /// 创建控制台日志记录器
    /// </summary>
    private static ILogger CreateConsoleLogger(string categoryName)
    {
        return new DebugLogger(categoryName);
    }

    /// <summary>
    /// 简单的调试日志记录器
    /// </summary>
    private class DebugLogger : ILogger
    {
        private readonly string _categoryName;

        public DebugLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}");
            
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception}");
            }
        }
    }

    private class NullScope : IDisposable
    {
        public void Dispose() { }
    }
}
