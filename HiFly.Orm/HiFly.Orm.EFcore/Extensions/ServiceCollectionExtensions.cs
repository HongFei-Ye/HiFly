// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Tables.Core.Interfaces;
using HiFly.Orm.EFcore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// EF Core 服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 EF Core 数据服务
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEfDataService<TContext, TItem>(this IServiceCollection services)
        where TContext : DbContext
        where TItem : class, new()
    {
        // 注册统一接口
        services.AddScoped<IHiFlyDataService<TItem>, EfDataService<TContext, TItem>>();
        
        return services;
    }

    /// <summary>
    /// 自动注册指定 DbContext 的所有实体数据服务
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAllEfDataServices<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        var contextType = typeof(TContext);
        var dbSetProperties = contextType.GetProperties()
            .Where(p => p.PropertyType.IsGenericType && 
                       p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        // 从 DbContext 的 DbSet 属性获取实体类型
        foreach (var property in dbSetProperties)
        {
            var entityType = property.PropertyType.GetGenericArguments()[0];

            // 注册数据服务
            var serviceType = typeof(IHiFlyDataService<>).MakeGenericType(entityType);
            var implementationType = typeof(EfDataService<,>).MakeGenericType(contextType, entityType);
            
            services.AddScoped(serviceType, implementationType);
        }

        return services;
    }

    /// <summary>
    /// 自动注册指定程序集中的所有实体数据服务
    /// </summary>
    /// <typeparam name="TContext">DbContext 类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddEfDataServicesFromAssemblies<TContext>(this IServiceCollection services, params Assembly[] assemblies)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            var entityTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
                .Where(HasDefaultConstructor)
                .ToList();

            foreach (var entityType in entityTypes)
            {
                var serviceType = typeof(IHiFlyDataService<>).MakeGenericType(entityType);
                var implementationType = typeof(EfDataService<,>).MakeGenericType(typeof(TContext), entityType);
                
                services.AddScoped(serviceType, implementationType);
            }
        }

        return services;
    }

    /// <summary>
    /// 创建简单的控制台日志记录器
    /// </summary>
    private static ILogger CreateConsoleLogger(string categoryName)
    {
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

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();

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

    /// <summary>
    /// 检查类型是否有默认构造函数
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>是否有默认构造函数</returns>
    private static bool HasDefaultConstructor(Type type)
    {
        return type.GetConstructor(Type.EmptyTypes) != null;
    }
}
