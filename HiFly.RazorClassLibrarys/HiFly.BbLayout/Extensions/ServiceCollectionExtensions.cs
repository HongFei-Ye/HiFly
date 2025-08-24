// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.BbLayout.Services;
using HiFly.BbLayout.Services.Interfaces;
using HiFly.DatabaseManager;
using HiFly.Openiddict.NavMenus.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHiFlyBbLayoutServices(this IServiceCollection services)
    {
        // 注册BB服务
        services.AddBootstrapBlazor();

        // 增加中文编码支持网页源码显示汉字
        services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

        // 设置控制台编码格式
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;

        // 增加多语言支持配置信息
        //services.AddRequestLocalization<IOptionsMonitor<BootstrapBlazorOptions>>((localizerOption, blazorOption) =>
        //{
        //    blazorOption.OnChange(op => Invoke(op));
        //    Invoke(blazorOption.CurrentValue);

        //    void Invoke(BootstrapBlazorOptions option)
        //    {
        //        var supportedCultures = option.GetSupportedCultures();
        //        localizerOption.SupportedCultures = supportedCultures;
        //        localizerOption.SupportedUICultures = supportedCultures;
        //    }
        //});


        // BB默认模板数据服务
        services.AddScoped<ILayoutDataService, LayoutDataService>();

        services.AddScoped<ILayoutSetService, LayoutSetService>();

        return services;
    }

    /// <summary>
    /// 配置菜单与Tab页面的绑定关系
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ConfigureHiFlyBbTabItemMenuBindOptions<TContext, TMenuPage>(this IServiceCollection services)
        where TContext : DbContext
        where TMenuPage : class, IMenuPage, new()
    {
        try
        {
            // 使用范围工厂避免直接构建服务提供者
            using var scope = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false }).CreateScope();

            var logger = scope.ServiceProvider.GetService<ILogger<TContext>>();

            // 尝试获取DatabaseService以检查数据库状态
            var databaseService = scope.ServiceProvider.GetService<IDatabaseService>();
            if (databaseService != null)
            {
                // 检查数据库是否存在且可连接
                var (exists, canConnect, errorMessage1) = databaseService.CheckDatabaseExistsAsync<TContext>().GetAwaiter().GetResult();
                if (!exists || !canConnect)
                {
                    logger?.LogWarning("无法配置Tab与Menu绑定：数据库不存在或无法连接 - {ErrorMessage}", errorMessage1);
                    return services; // 提前返回，避免尝试访问不存在的数据库
                }

                // 检查待处理迁移
                var (success, pendingMigrations, errorMessage2) = databaseService.GetPendingMigrationsAsync<TContext>().GetAwaiter().GetResult();
                if (success && pendingMigrations.Any())
                {
                    logger?.LogWarning("无法配置Tab与Menu绑定：数据库存在迁移未完成 - {ErrorMessage}", errorMessage2);
                    return services; // 提前返回，避免尝试访问不存在的数据库
                }
            }

            // 数据库可连接，继续处理
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();

            List<TMenuPage> menuPages = [];
            try
            {
                using var dbContext = dbContextFactory.CreateDbContext();
                menuPages = dbContext.Set<TMenuPage>().ToList();
            }
            catch (Exception ex)
            {
                // 捕获与数据库通信的异常
                logger?.LogError(ex, "获取菜单页面数据时发生错误");

                return services; // 发生错误时提前返回
            }

            if (menuPages == null || menuPages.Count == 0)
            {
                // 如果没有菜单页面，则直接返回
                return services;
            }

            // 配置 Tab 与 Menu 联动字典
            services.ConfigureTabItemMenuBindOptions(op =>
            {
                // 先清空现有的绑定，避免重复键问题
                op.Binders.Clear();

                foreach (var menuPage in menuPages)
                {
                    string url = menuPage.Url ?? "";

                    // 确保键不重复
                    if (!op.Binders.ContainsKey(url))
                    {
                        op.Binders.Add(url, new TabItemOptionAttribute
                        {
                            Text = menuPage.Text ?? "",
                            Icon = menuPage.Icon ?? ""
                        });
                    }
                }
            });

            logger?.LogInformation("导航菜单联动字典绑定完成");
        }
        catch (Exception ex)
        {
            // 捕获所有未处理的异常
            // 在这里我们使用一个临时ServiceProvider来获取logger，因为我们不想让异常阻止应用程序启动
            using var tempScope = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = false }).CreateScope();
            var logger = tempScope.ServiceProvider.GetService<ILogger<TContext>>();
            logger?.LogError(ex, "配置Tab与Menu绑定时发生未处理异常");
        }

        return services;
    }


}
