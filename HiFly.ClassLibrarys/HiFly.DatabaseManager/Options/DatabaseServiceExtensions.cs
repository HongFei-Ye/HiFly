// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HiFly.DatabaseManager.Options;

/// <summary>
/// 数据库服务扩展方法
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// 添加数据库管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHiFlyDatabaseService(
    this IServiceCollection services,
    Action<DatabaseOptions> configureOptions)
    {
        // 注册并配置选项
        services.Configure(configureOptions);

        // 注册原始服务作为命名服务
        services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var databaseService = DatabaseServiceFactory.Create(options.ProviderType);
            return new ConfiguredDatabaseService(databaseService, serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>());
        });

        // 注册数据库状态服务
        services.AddSingleton<DatabaseStateService>();

        return services;
    }

    /// <summary>
    /// 添加数据库管理服务（简化版）
    /// </summary>
    public static IServiceCollection AddHiFlyDatabaseServiceSimple(
        this IServiceCollection services,
        Action<DatabaseOptions> configureOptions)
    {
        // 注册并配置选项
        services.Configure(configureOptions);

        // 注册数据库服务
        services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return DatabaseServiceFactory.Create(options.ProviderType);
        });

        // 注册数据库状态服务
        services.AddSingleton<DatabaseStateService>();

        return services;
    }

}
