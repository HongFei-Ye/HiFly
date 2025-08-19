// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using FreeSql;
using HiFly.Orm.FreeSql.Extensions;
using HiFly.Tables.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HiFly.Orm.FreeSql.Examples;

/// <summary>
/// FreeSql 完整使用示例
/// </summary>
public static class FreeSqlCompleteExample
{
    /// <summary>
    /// Program.cs 完整配置示例
    /// </summary>
    public static void ConfigureProgram()
    {
        var builder = WebApplication.CreateBuilder();

        // === 配置 FreeSql ===
        var assemblies = new[] { typeof(Product).Assembly };

        // 开发环境使用 SQLite
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddFreeSqlSqlite(
                "Data Source=hifly_dev.db",
                assemblies);
        }
        // 生产环境使用 SQL Server
        else
        {
            builder.Services.AddFreeSqlSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")!,
                assemblies);
        }

        // === 添加其他服务 ===
        builder.Services.AddBootstrapBlazor();
        builder.Services.AddControllers();

        var app = builder.Build();

        // === 配置管道 ===
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.MapControllers();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }

    /// <summary>
    /// 自定义FreeSql配置示例
    /// </summary>
    public static void CustomFreeSqlConfiguration(IServiceCollection services, IConfiguration configuration)
    {
        var assemblies = new[] { typeof(Product).Assembly };

        services.AddFreeSqlWithDataServices(
            () =>
            {
                var builder = new FreeSqlBuilder()
                    .UseConnectionString(DataType.Sqlite, "Data Source=hifly.db")
                    .UseAutoSyncStructure(true) // 自动同步数据库结构
                    .UseGenerateCommandParameterWithLambda(true) // 生成命令参数
                    .UseLazyLoading(false) // 禁用延迟加载
                    .UseNoneCommandParameter(false); // 使用参数化查询

                // 开发环境添加SQL监控
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    builder.UseMonitorCommand(cmd =>
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] SQL: {cmd.CommandText}");
                        if (cmd.Parameters?.Count > 0)
                        {
                            Console.WriteLine($"参数: {string.Join(", ", cmd.Parameters)}");
                        }
                    });
                }

                return builder;
            },
            assemblies);
    }

    /// <summary>
    /// 多数据库配置示例
    /// </summary>
    public static void MultiDatabaseConfiguration(IServiceCollection services, IConfiguration configuration)
    {
        // 主数据库 - 业务数据
        var businessAssemblies = new[] { typeof(Product).Assembly };
        services.AddFreeSqlSqlServer(
            configuration.GetConnectionString("BusinessConnection")!,
            businessAssemblies);

        // 日志数据库 - 单独的FreeSql实例
        services.AddSingleton<IFreeSql>(provider =>
        {
            return new FreeSqlBuilder()
                .UseConnectionString(DataType.Sqlite, "Data Source=logs.db")
                .UseAutoSyncStructure(true)
                .Build();
        });

        // 手动注册日志相关服务
        services.AddFreeSqlDataService<AuditLog>();
        services.AddFreeSqlDataService<SystemLog>();
    }
}

/// <summary>
/// Blazor 组件使用示例
/// </summary>
public partial class ProductListComponent : ComponentBase
{
    [Inject] public IHiFlyDataService<Product> ProductService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // 组件初始化时的逻辑
        await base.OnInitializedAsync();
    }

    private async Task HandleProductAdded(Product product)
    {
        var success = await ProductService.OnSaveAsync(product, BootstrapBlazor.Components.ItemChangedType.Add);
        if (success)
        {
            // 添加成功的处理
            StateHasChanged();
        }
    }
}

/// <summary>
/// Web API 控制器使用示例
/// </summary>
[Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IHiFlyDataService<Product> _productService;

    public ProductsController(IHiFlyDataService<Product> productService)
    {
        _productService = productService;
    }

    [HttpPost("query")]
    public async Task<ActionResult> Query([FromBody] BootstrapBlazor.Components.QueryPageOptions options)
    {
        var result = await _productService.OnQueryAsync(options);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] Product product)
    {
        var success = await _productService.OnSaveAsync(product, BootstrapBlazor.Components.ItemChangedType.Add);
        return success ? Ok() : BadRequest();
    }
}

/// <summary>
/// 示例实体：产品
/// </summary>
public class Product
{
    /// <summary>
    /// 主键
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 产品名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 产品描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 价格
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// 库存数量
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 计算属性：库存状态
    /// </summary>
    public string StockStatus => Stock > 0 ? "有库存" : "缺货";
}

/// <summary>
/// 示例实体：分类（树形结构）
/// </summary>
public class Category
{
    /// <summary>
    /// 主键
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 父分类ID
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 示例实体：审计日志
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string UserId { get; set; } = "";
    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 示例实体：系统日志
/// </summary>
public class SystemLog
{
    public long Id { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Exception { get; set; }
    public string Source { get; set; } = "";
    public DateTime CreateTime { get; set; } = DateTime.Now;
}
