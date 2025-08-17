// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Tables.Cache.Configuration;
using HiFly.Tables.Cache.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HiFly.Tables.Extensions;

/// <summary>
/// HiFly Tables 统一配置选项 - 重构后不依赖 EF Core
/// </summary>
public class HiFlyTablesOptions
{
    /// <summary>
    /// 是否启用缓存功能
    /// </summary>
    public bool EnableCache { get; set; } = false;

    /// <summary>
    /// 缓存配置节点名称
    /// </summary>
    public string CacheConfigurationSection { get; set; } = CacheOptions.SectionName;

    /// <summary>
    /// 是否启用自动服务注册
    /// </summary>
    public bool EnableAutoServiceRegistration { get; set; } = true;

    /// <summary>
    /// 服务注册策略
    /// </summary>
    public ServiceRegistrationStrategy RegistrationStrategy { get; set; } = ServiceRegistrationStrategy.AutoDetect;

    /// <summary>
    /// 要扫描的程序集列表（当使用标记接口或特性策略时）
    /// </summary>
    public List<System.Reflection.Assembly> AssembliesToScan { get; set; } = new();

    /// <summary>
    /// 日志工厂
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// 自定义缓存配置委托
    /// </summary>
    public Action<CacheOptions>? ConfigureCache { get; set; }

    /// <summary>
    /// 是否启用开发模式（更详细的日志）
    /// </summary>
    public bool EnableDevelopmentMode { get; set; } = false;

    /// <summary>
    /// 数据提供者类型
    /// </summary>
    public DataProviderType DataProvider { get; set; } = DataProviderType.EntityFramework;
}

/// <summary>
/// 数据提供者类型
/// </summary>
public enum DataProviderType
{
    /// <summary>
    /// Entity Framework Core
    /// </summary>
    EntityFramework,

    /// <summary>
    /// FreeSql ORM
    /// </summary>
    FreeSql,

    /// <summary>
    /// Web API
    /// </summary>
    WebApi,

    /// <summary>
    /// 内存数据
    /// </summary>
    Memory,

    /// <summary>
    /// 自定义
    /// </summary>
    Custom
}

/// <summary>
/// 服务注册策略
/// </summary>
public enum ServiceRegistrationStrategy
{
    /// <summary>
    /// 自动检测DbContext中的DbSet属性（仅适用于 EF Core）
    /// </summary>
    AutoDetect,

    /// <summary>
    /// 基于标记接口扫描
    /// </summary>
    MarkerInterface,

    /// <summary>
    /// 基于特性扫描
    /// </summary>
    Attribute,

    /// <summary>
    /// 手动注册（不自动注册任何服务）
    /// </summary>
    Manual
}

/// <summary>
/// HiFly Tables 统一注册扩展方法 - 重构后的版本
/// </summary>
public static class HiFlyTablesServiceCollectionExtensions
{
    /// <summary>
    /// 添加 HiFly Tables 服务的统一入口 - 需要指定数据提供者
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHiFlyTables(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<HiFlyTablesOptions>? configureOptions = null)
    {
        // 创建配置选项
        var options = new HiFlyTablesOptions();
        configureOptions?.Invoke(options);

        var logger = options.LoggerFactory?.CreateLogger("HiFlyTables") ??
                     CreateConsoleLogger("HiFlyTables", options.EnableDevelopmentMode);

        logger.LogInformation("开始配置 HiFly Tables 服务，数据提供者: {DataProvider}", options.DataProvider);

        try
        {
            // 1. 注册缓存服务（如果启用）
            if (options.EnableCache)
            {
                logger.LogInformation("启用缓存功能");
                services.AddTableCache(configuration);

                // 应用自定义缓存配置
                if (options.ConfigureCache != null)
                {
                    services.PostConfigure<CacheOptions>(options.ConfigureCache);
                }
            }

            // 2. 根据数据提供者类型进行服务注册
            switch (options.DataProvider)
            {
                case DataProviderType.EntityFramework:
                    logger.LogWarning("Entity Framework 数据提供者需要使用 HiFly.Orm.EFcore 包的扩展方法");
                    break;

                case DataProviderType.FreeSql:
                    logger.LogWarning("FreeSql 数据提供者需要使用 HiFly.Orm.FreeSql 包的扩展方法");
                    break;

                case DataProviderType.WebApi:
                    logger.LogWarning("Web API 数据提供者尚未实现");
                    break;

                case DataProviderType.Memory:
                    logger.LogWarning("内存数据提供者尚未实现");
                    break;

                case DataProviderType.Custom:
                    logger.LogInformation("使用自定义数据提供者，请确保已手动注册");
                    break;
            }

            // 3. 添加其他核心服务
            AddCoreServices(services, options);

            logger.LogInformation("HiFly Tables 服务配置完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "配置 HiFly Tables 服务时发生错误");
            throw;
        }

        return services;
    }

    /// <summary>
    /// 添加 HiFly Tables 服务（简化版本，默认使用 EF Core）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="enableCache">是否启用缓存</param>
    /// <returns>服务集合</returns>
    [Obsolete("请使用 HiFly.Orm.EFcore 包中的扩展方法来添加 EF Core 支持")]
    public static IServiceCollection AddHiFlyTables(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableCache = true)
    {
        return services.AddHiFlyTables(configuration, options =>
        {
            options.EnableCache = enableCache;
            options.DataProvider = DataProviderType.EntityFramework;
        });
    }

    /// <summary>
    /// 添加核心服务
    /// </summary>
    private static void AddCoreServices(IServiceCollection services, HiFlyTablesOptions options)
    {
        // 这里可以添加其他核心服务
        // 例如：权限验证、审计日志等
    }

    /// <summary>
    /// 检查是否已启用缓存配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节点名称</param>
    /// <returns>是否启用缓存</returns>
    public static bool IsCacheEnabled(IConfiguration configuration, string sectionName = CacheOptions.SectionName)
    {
        var cacheSection = configuration.GetSection(sectionName);
        return cacheSection.Exists() && !string.IsNullOrEmpty(cacheSection["DefaultExpirationMinutes"]);
    }

    /// <summary>
    /// 创建控制台日志记录器
    /// </summary>
    private static ILogger CreateConsoleLogger(string categoryName, bool enableDebug = false)
    {
        return new ConsoleLogger(categoryName, enableDebug);
    }

    /// <summary>
    /// 简单的控制台日志记录器
    /// </summary>
    private class ConsoleLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly bool _enableDebug;

        public ConsoleLogger(string categoryName, bool enableDebug = false)
        {
            _categoryName = categoryName;
            _enableDebug = enableDebug;
        }

        public IDisposable BeginScope<TState>(TState state) => new NullScope();

        public bool IsEnabled(LogLevel logLevel) => 
            logLevel >= (_enableDebug ? LogLevel.Debug : LogLevel.Information);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            
            Console.WriteLine($"[{timestamp}] [{logLevel}] {_categoryName}: {message}");
            
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception}");
            }
        }

        private class NullScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}

/// <summary>
/// HiFly Tables 构建器，用于链式配置
/// </summary>
public class HiFlyTablesBuilder
{
    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }
    public HiFlyTablesOptions Options { get; }

    internal HiFlyTablesBuilder(IServiceCollection services, IConfiguration configuration, HiFlyTablesOptions options)
    {
        Services = services;
        Configuration = configuration;
        Options = options;
    }

    /// <summary>
    /// 启用缓存功能
    /// </summary>
    /// <param name="configureCache">缓存配置委托</param>
    /// <returns>构建器实例</returns>
    public HiFlyTablesBuilder WithCache(Action<CacheOptions>? configureCache = null)
    {
        Options.EnableCache = true;
        Options.ConfigureCache = configureCache;
        return this;
    }

    /// <summary>
    /// 配置服务注册策略
    /// </summary>
    /// <param name="strategy">注册策略</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>构建器实例</returns>
    public HiFlyTablesBuilder WithRegistrationStrategy(
        ServiceRegistrationStrategy strategy,
        params System.Reflection.Assembly[] assemblies)
    {
        Options.RegistrationStrategy = strategy;
        Options.AssembliesToScan.AddRange(assemblies);
        return this;
    }

    /// <summary>
    /// 启用开发模式
    /// </summary>
    /// <param name="loggerFactory">日志工厂</param>
    /// <returns>构建器实例</returns>
    public HiFlyTablesBuilder WithDevelopmentMode(ILoggerFactory? loggerFactory = null)
    {
        Options.EnableDevelopmentMode = true;
        Options.LoggerFactory = loggerFactory;
        return this;
    }
}
