# HiFly.Orm.FreeSql 快速开始指南

## 🎯 概述

`HiFly.Orm.FreeSql` 是 HiFly Tables 的 FreeSql 数据访问实现包，提供了完整的 CRUD 操作支持。FreeSql 是一个功能强大、易于使用的 .NET ORM 组件。

## 📦 安装

```xml
<PackageReference Include="HiFly.Tables.Components" Version="1.0.0" />
<PackageReference Include="HiFly.Orm.FreeSql" Version="1.0.0" />
<!-- 根据需要添加特定数据库提供程序 -->
<PackageReference Include="FreeSql.Provider.Sqlite" Version="3.5.102" />
<!-- 或者其他数据库 -->
<!-- <PackageReference Include="FreeSql.Provider.MySql" Version="3.5.102" /> -->
<!-- <PackageReference Include="FreeSql.Provider.SqlServer" Version="3.5.102" /> -->
```

## 🚀 快速开始

### 1. 定义实体类

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.Now;
}
```

### 2. 注册服务（推荐方式）

```csharp
// Program.cs
using HiFly.Orm.FreeSql.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 方式1：快速配置 SQLite
var assemblies = new[] { typeof(Product).Assembly };
builder.Services.AddFreeSqlSqlite(
    "Data Source=app.db", 
    assemblies);

// 方式2：快速配置 MySQL
// builder.Services.AddFreeSqlMySql(
//     builder.Configuration.GetConnectionString("DefaultConnection"), 
//     assemblies);

// 方式3：快速配置 SQL Server
// builder.Services.AddFreeSqlSqlServer(
//     builder.Configuration.GetConnectionString("DefaultConnection"), 
//     assemblies);

var app = builder.Build();
app.Run();
```

### 3. 自定义配置（高级用法）

```csharp
// Program.cs
using HiFly.Orm.FreeSql.Extensions;

var builder = WebApplication.CreateBuilder(args);

var assemblies = new[] { typeof(Product).Assembly };

builder.Services.AddFreeSqlWithDataServices(
    () => new FreeSqlBuilder()
        .UseConnectionString(DataType.Sqlite, "Data Source=app.db")
        .UseAutoSyncStructure(builder.Environment.IsDevelopment()) // 开发环境自动同步结构
        .UseGenerateCommandParameterWithLambda(true)
        .UseMonitorCommand(cmd => // 监控SQL
        {
            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine($"SQL: {cmd.CommandText}");
            }
        }),
    assemblies);

var app = builder.Build();
app.Run();
```

### 4. 在组件中使用

```razor
@page "/products"
@inject IHiFlyDataService<Product> ProductService

<TItemTable TItem="Product" 
            ShowSearch="true"
            ShowAddButton="true">
    <TableColumns>
        <TableColumn @bind-Field="@context.Name" Text="产品名称" />
        <TableColumn @bind-Field="@context.Price" Text="价格" />
        <TableColumn @bind-Field="@context.Stock" Text="库存" />
    </TableColumns>
</TItemTable>
```

## 🔧 高级用法

### 手动注册特定实体

```csharp
// 先注册 FreeSql 实例
builder.Services.AddSingleton<IFreeSql>(provider =>
{
    return new FreeSqlBuilder()
        .UseConnectionString(DataType.Sqlite, "Data Source=app.db")
        .UseAutoSyncStructure(true)
        .Build();
});

// 手动注册特定实体
builder.Services.AddFreeSqlDataService<Product>();
builder.Services.AddFreeSqlDataService<User>();
```

### 自定义数据服务

```csharp
public class CustomProductService : FreeSqlDataService<Product>
{
    public CustomProductService(
        IFreeSql freeSql,
        ILogger<FreeSqlDataService<Product>> logger) 
        : base(freeSql, logger)
    {
    }

    public override async Task<QueryData<Product>> OnQueryAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters = null,
        bool isTree = false)
    {
        // 自定义查询逻辑
        var result = await base.OnQueryAsync(options, propertyFilterParameters, isTree);
        
        // 添加业务处理
        foreach (var item in result.Items)
        {
            item.StockStatus = item.Stock > 0 ? "有库存" : "缺货";
        }
        
        return result;
    }
}

// 注册自定义服务
builder.Services.AddScoped<IHiFlyDataService<Product>, CustomProductService>();
```

### 树形结构支持

确保实体有 `Id` 和 `ParentId` 属性：

```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? ParentId { get; set; }
}
```

在组件中启用树形模式：

```razor
<TItemTable TItem="Category" IsTree="true">
    <TableColumns>
        <TableColumn @bind-Field="@context.Name" Text="分类名称" />
    </TableColumns>
</TItemTable>
```

## 📋 支持的数据库

| 数据库 | 扩展方法 | NuGet 包 | 状态 |
|--------|----------|----------|------|
| SQLite | `AddFreeSqlSqlite()` | `FreeSql.Provider.Sqlite` | ✅ 完整支持 |
| MySQL | `AddFreeSqlMySql()` | `FreeSql.Provider.MySql` | ✅ 完整支持 |
| SQL Server | `AddFreeSqlSqlServer()` | `FreeSql.Provider.SqlServer` | ✅ 完整支持 |
| PostgreSQL | `AddFreeSqlPostgreSQL()` | `FreeSql.Provider.PostgreSQL` | ✅ 完整支持 |
| Oracle | `AddFreeSqlOracle()` | `FreeSql.Provider.Oracle` | ✅ 完整支持 |
| 达梦数据库 | `AddFreeSqlDameng()` | `FreeSql.Provider.Dameng` | ✅ 完整支持 |
| 人大金仓 | `AddFreeSqlKingbaseES()` | `FreeSql.Provider.KingbaseES` | ✅ 完整支持 |

### 🔥 新增数据库支持

现在 HiFly.Orm.FreeSql 已经内置支持 **7 种主流数据库**，包括：

#### 🌟 主流商业数据库
- **SQLite** - 轻量级嵌入式数据库
- **MySQL** - 开源关系型数据库
- **SQL Server** - 微软企业级数据库
- **PostgreSQL** - 开源对象关系型数据库
- **Oracle** - 企业级商业数据库

#### 🇨🇳 国产数据库（独有优势）
- **达梦数据库** - 国产商业数据库
- **人大金仓** - 国产安全数据库

### 🚀 快速配置示例

#### SQLite（轻量级）
```csharp
var assemblies = new[] { typeof(Product).Assembly };
builder.Services.AddFreeSqlSqlite("Data Source=app.db", assemblies);
```

#### PostgreSQL（高性能）
```csharp
var assemblies = new[] { typeof(Product).Assembly };
builder.Services.AddFreeSqlPostgreSQL(
    "Host=localhost;Database=mydb;Username=user;Password=pass", 
    assemblies);
```

#### Oracle（企业级）
```csharp
var assemblies = new[] { typeof(Product).Assembly };
builder.Services.AddFreeSqlOracle(
    "Data Source=localhost:1521/ORCL;User Id=hr;Password=pass;", 
    assemblies);
```

#### 达梦数据库（国产）
```csharp
var assemblies = new[] { typeof(Product).Assembly };
builder.Services.AddFreeSqlDameng(
    "Server=localhost;UserId=SYSDBA;PWD=SYSDBA;", 
    assemblies);
```

## 🎯 FreeSql vs EF Core

| 特性 | FreeSql | EF Core |
|------|---------|---------|
| **学习曲线** | 更容易 | 稍复杂 |
| **性能** | 更高 | 较高 |
| **功能丰富度** | 丰富 | 丰富 |
| **代码生成** | 支持 | 支持 |
| **多数据库** | 优秀 | 好 |
| **社区支持** | 中等 | 强大 |

### 何时选择 FreeSql？

✅ **推荐使用 FreeSql：**
- 追求更高性能
- 需要简单易用的API
- 项目对性能要求较高
- 团队更喜欢流式API

✅ **推荐使用 EF Core：**
- 团队熟悉 EF Core
- 需要更强的社区支持
- 使用复杂的实体关系
- 微软技术栈项目

## 📋 API 参考

### 服务注册方法

- `AddFreeSqlSqlite()` - 快速配置 SQLite
- `AddFreeSqlMySql()` - 快速配置 MySQL  
- `AddFreeSqlSqlServer()` - 快速配置 SQL Server
- `AddFreeSqlWithDataServices()` - 自定义配置
- `AddFreeSqlDataService<TItem>()` - 注册特定实体

### 数据服务接口

- `IHiFlyDataService<TItem>` - 统一的数据服务接口
- `FreeSqlDataService<TItem>` - FreeSql 具体实现

### 核心方法

- `OnQueryAsync()` - 查询数据
- `OnSaveAsync()` - 保存数据  
- `OnDeleteAsync()` - 删除数据

## 🔄 迁移指南

### 从 EF Core 迁移

1. 更新包引用：
   ```xml
   <!-- 移除 -->
   <PackageReference Include="HiFly.Orm.EFcore" />
   
   <!-- 添加 -->
   <PackageReference Include="HiFly.Orm.FreeSql" />
   <PackageReference Include="FreeSql.Provider.Sqlite" />
   ```

2. 更新服务注册：
   ```csharp
   // EF Core 方式
   // builder.Services.AddAllEfDataServices<MyDbContext>();
   
   // FreeSql 方式
   var assemblies = new[] { typeof(Product).Assembly };
   builder.Services.AddFreeSqlSqlite("Data Source=app.db", assemblies);
   ```

3. 组件使用不变：
   ```razor
   <!-- 无需修改，使用相同的接口 -->
   <TItemTable TItem="Product" />
   ```

### 从旧版本迁移

服务注入保持不变，依然使用 `IHiFlyDataService<TItem>`：

```csharp
[Inject] IHiFlyDataService<Product> ProductService { get; set; }
```

## ⚡ 性能优化建议

1. **使用合适的查询方式**：
   ```csharp
   // 推荐：只查询需要的字段
   var products = await freeSql.Select<Product>()
       .Where(p => p.CategoryId == categoryId)
       .ToListAsync(p => new { p.Id, p.Name, p.Price });
   ```

2. **启用SQL监控**（仅开发环境）：
   ```csharp
   .UseMonitorCommand(cmd => Console.WriteLine($"SQL: {cmd.CommandText}"))
   ```

3. **合理使用自动同步结构**：
   ```csharp
   .UseAutoSyncStructure(Environment.IsDevelopment()) // 仅开发环境
   ```

## 🛠️ 故障排除

### 常见问题

1. **实体未注册**
   - 确保实体类有 `Id` 属性
   - 检查程序集是否正确传入
   - 查看日志确认注册情况

2. **数据库连接问题**
   - 检查连接字符串
   - 确认数据库文件路径（SQLite）
   - 验证数据库服务是否启动

3. **性能问题**
   - 启用SQL监控查看执行的SQL
   - 检查是否有N+1查询问题
   - 考虑添加适当的索引

## 🆘 获取帮助

- **FreeSql 官方文档**: https://freesql.net/
- **技术支持**: felix@hongfei8.com
- **官方网站**: www.hongfei8.cn

## 📄 许可证

MIT License - 详见 LICENSE 文件

---

🎉 **恭喜！** 现在您可以享受 FreeSql 带来的高性能和易用性了！
