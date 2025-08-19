// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.BbTables.Attributes;
using HiFly.BbTables.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace HiFly.BbTables.Extensions;

/// <summary>
/// 非泛型静态辅助类
/// </summary>
public static class GenericCrudServiceExtensions
{
    /// <summary>
    /// 为 ServiceCollection 添加泛型 CRUD 服务注册扩展方法
    /// </summary>
    public static IServiceCollection AddGenericCrudService<TContext, TItem>(
        this IServiceCollection services)
        where TContext : DbContext
        where TItem : class, new()
    {
        services.AddScoped<GenericCrudService<TContext, TItem>>();
        return services;
    }

    /// <summary>
    /// 自动注册指定 DbContext 的所有实体的 GenericCrudService
    /// </summary>
    /// <typeparam name="TContext">数据库上下文类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAllGenericCrudServices<TContext>(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null)
        where TContext : DbContext
    {
        // 创建临时日志记录器
        var logger = loggerFactory?.CreateLogger("ServiceRegistration") ??
                     CreateConsoleLogger("ServiceRegistration");

        // 通过临时创建 DbContext 实例来获取所有实体类型
        var contextType = typeof(TContext);
        var dbSetProperties = contextType.GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        logger.LogInformation("开始注册 GenericCrudService，发现 {Count} 个 DbSet 属性", dbSetProperties.Count);

        foreach (var property in dbSetProperties)
        {
            var entityType = property.PropertyType.GetGenericArguments()[0];

            // 检查实体类型是否满足约束条件
            if (entityType.IsClass &&
                !entityType.IsAbstract &&
                entityType.GetConstructor(Type.EmptyTypes) != null)
            {
                // 创建泛型服务类型
                var serviceType = typeof(GenericCrudService<,>).MakeGenericType(contextType, entityType);

                // 注册服务
                services.AddScoped(serviceType);

                logger.LogInformation("已注册: GenericCrudService<{ContextType}, {EntityType}>",
                    contextType.Name, entityType.Name);
            }
            else
            {
                logger.LogWarning("跳过实体 {EntityType}：不满足约束条件", entityType.Name);
            }
        }

        logger.LogInformation("GenericCrudService 注册完成");

        return services;
    }

    /// <summary>
    /// 基于标记接口自动注册 GenericCrudService
    /// </summary>
    /// <typeparam name="TContext">数据库上下文类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddGenericCrudServicesByMarker<TContext>(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null,
        params Assembly[] assemblies)
        where TContext : DbContext
    {
        var logger = loggerFactory?.CreateLogger("ServiceRegistration") ??
             CreateConsoleLogger("ServiceRegistration");

        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetExecutingAssembly()];
        }

        var entityTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass &&
                       !t.IsAbstract &&
                       t.GetInterfaces().Any(i => i == typeof(ICrudEntity)) &&
                       t.GetConstructor(Type.EmptyTypes) != null)
            .ToList();

        logger.LogInformation("发现 {Count} 个标记为 ICrudEntity 的实体类型", entityTypes.Count);

        foreach (var entityType in entityTypes)
        {
            var serviceType = typeof(GenericCrudService<,>).MakeGenericType(typeof(TContext), entityType);
            services.AddScoped(serviceType);

            logger.LogInformation("已注册: GenericCrudService<{ContextType}, {EntityType}>",
            typeof(TContext).Name, entityType.Name);
        }

        return services;
    }

    /// <summary>
    /// 基于特性自动注册 GenericCrudService
    /// </summary>
    public static IServiceCollection AddGenericCrudServicesByAttribute<TContext>(
        this IServiceCollection services,
        ILoggerFactory? loggerFactory = null,
        params Assembly[] assemblies)
        where TContext : DbContext
    {
        var logger = loggerFactory?.CreateLogger("ServiceRegistration") ??
                     CreateConsoleLogger("ServiceRegistration");

        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetExecutingAssembly()];
        }

        var entityTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass &&
                       !t.IsAbstract &&
                       t.GetCustomAttribute<CrudEntityAttribute>() != null &&
                       t.GetConstructor(Type.EmptyTypes) != null)
            .ToList();

        logger.LogInformation("发现 {Count} 个标记为 CrudEntity 的实体类型", entityTypes.Count);

        foreach (var entityType in entityTypes)
        {
            var serviceType = typeof(GenericCrudService<,>).MakeGenericType(typeof(TContext), entityType);
            services.AddScoped(serviceType);

            var attribute = entityType.GetCustomAttribute<CrudEntityAttribute>();
            logger.LogInformation("已注册: GenericCrudService<{ContextType}, {EntityType}> - {Description}",
                typeof(TContext).Name, entityType.Name, attribute?.Description ?? "无描述");
        }

        return services;
    }

    /// <summary>
    /// 创建简单的控制台日志记录器
    /// </summary>
    private static ILogger CreateConsoleLogger(string categoryName)
    {
        //using var loggerFactory = LoggerFactory.Create(builder =>
        //{
        //    builder.AddConsole()
        //           .SetMinimumLevel(LogLevel.Information);
        //});

        //return loggerFactory.CreateLogger(categoryName);


        return new DebugLogger(categoryName);
    }


    /// <summary>
    /// 调试输出日志记录器
    /// </summary>
    private class DebugLogger : ILogger
    {
        private readonly string _categoryName;

        public DebugLogger(string categoryName)
        {
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => new NullScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            System.Diagnostics.Debug.WriteLine($"[{logLevel}] {_categoryName}: {message}");
        }

        private class NullScope : IDisposable
        {
            public void Dispose() { }
        }
    }

}

