# HiFly.Orm.EFcore 快速开始指南

## 🎯 概述

`HiFly.Orm.EFcore` 是 HiFly Tables 的 Entity Framework Core 数据访问实现包，提供了完整的 CRUD 操作支持。

## 📦 安装

```xml
<PackageReference Include="HiFly.Tables.Components" Version="1.0.0" />
<PackageReference Include="HiFly.Orm.EFcore" Version="1.0.0" />
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

### 2. 创建 DbContext

```csharp
public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
}
```

### 3. 注册服务

```csharp
// Program.cs
using HiFly.Orm.EFcore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注册 DbContext
builder.Services.AddDbContextFactory<MyDbContext>(options =>
    options.UseSqlServer(connectionString));

// 自动注册所有实体的数据服务
builder.Services.AddAllEfDataServices<MyDbContext>();

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

### 自定义数据服务

```csharp
public class CustomProductService : EfDataService<MyDbContext, Product>
{
    public CustomProductService(
        IDbContextFactory<MyDbContext> dbContextFactory,
        ILogger<EfDataService<MyDbContext, Product>> logger) 
        : base(dbContextFactory, logger)
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

## 📋 API 参考

### 服务注册方法

- `AddAllEfDataServices<TContext>()` - 自动注册所有实体
- `AddEfDataService<TContext, TItem>()` - 注册特定实体

### 数据服务接口

- `IHiFlyDataService<TItem>` - 新的推荐接口
- `ICrudService<TContext, TItem>` - 已过时，仅用于兼容

### 核心方法

- `OnQueryAsync()` - 查询数据
- `OnSaveAsync()` - 保存数据  
- `OnDeleteAsync()` - 删除数据

## 🔄 从旧版本迁移

1. 更新包引用：
   ```xml
   <!-- 移除 -->
   <PackageReference Include="HiFly.Tables.Services" />
   
   <!-- 添加 -->
   <PackageReference Include="HiFly.Orm.EFcore" />
   ```

2. 更新服务注册：
   ```csharp
   // 旧版本
   services.AddHiFlyTables<MyDbContext>(configuration);
   
   // 新版本
   services.AddAllEfDataServices<MyDbContext>();
   ```

3. 更新组件使用：
   ```razor
   <!-- 旧版本 -->
   <TItemTable TContext="MyDbContext" TItem="Product" />
   
   <!-- 新版本 -->
   <TItemTable TItem="Product" />
   ```

4. 更新服务注入：
   ```csharp
   // 旧版本
   [Inject] ICrudService<MyDbContext, Product> ProductService { get; set; }
   
   // 新版本
   [Inject] IHiFlyDataService<Product> ProductService { get; set; }
   ```

## 📋 支持的数据库

| 数据库 | 扩展方法 | NuGet 包 | 状态 |
|--------|----------|----------|------|
| SQLite | `AddEfSqlite<TContext>()` | `Microsoft.EntityFrameworkCore.Sqlite` | ✅ 完整支持 |
| SQL Server | `AddEfSqlServer<TContext>()` | `Microsoft.EntityFrameworkCore.SqlServer` | ✅ 完整支持 |
| PostgreSQL | `AddEfPostgreSQL<TContext>()` | `Npgsql.EntityFrameworkCore.PostgreSQL` | ✅ 完整支持 |
| Oracle | `AddEfOracle<TContext>()` | `Oracle.EntityFrameworkCore` | ✅ 完整支持 |
| Cosmos DB | `AddEfCosmosDB<TContext>()` | `Microsoft.EntityFrameworkCore.Cosmos` | ✅ 完整支持 |

### 🔥 新增数据库支持

现在 HiFly.Orm.EFcore 已经内置支持 **5 种主流数据库**，包括：

#### 🌟 企业级数据库
- **SQL Server** - 微软企业级数据库
- **PostgreSQL** - 开源对象关系型数据库  
- **Oracle** - 企业级商业数据库
- **SQLite** - 轻量级嵌入式数据库
- **Cosmos DB** - 微软云原生 NoSQL 数据库

### 🚀 快速配置示例

#### SQL Server（企业级）
```csharp
builder.Services.AddEfSqlServer<MyDbContext>(
    "Server=localhost;Database=MyApp;Trusted_Connection=true;");
```

#### PostgreSQL（高性能）
```csharp
builder.Services.AddEfPostgreSQL<MyDbContext>(
    "Host=localhost;Database=mydb;Username=user;Password=pass");
```

#### SQLite（开发测试）
```csharp
builder.Services.AddEfSqlite<MyDbContext>("Data Source=app.db");
```

#### Oracle（企业级）
```csharp
builder.Services.AddEfOracle<MyDbContext>(
    "Data Source=localhost:1521/ORCL;User Id=hr;Password=pass;");
```

#### Cosmos DB（云原生）
```csharp
builder.Services.AddEfCosmosDB<MyDbContext>(
    "AccountEndpoint=https://...;AccountKey=...;", 
    "MyDatabase");
```

### 🎯 对比 FreeSql

| 特性 | HiFly.Orm.EFcore | HiFly.Orm.FreeSql |
|------|------------------|-------------------|
| **数据库支持** | 5个主流数据库 | 7个数据库（含国产） |
| **配置复杂度** | 简单（一行代码） | 简单（一行代码） |
| **企业级支持** | 优秀 | 优秀 |
| **国产数据库** | 无 | ✅ 达梦、人大金仓 |
| **微软生态** | ✅ 原生支持 | 良好 |
| **学习曲线** | 低（熟悉EF Core） | 低（简洁API） |

### 何时选择 EF Core？

✅ **推荐使用 EF Core：**
- 团队熟悉 EF Core 技术栈
- 使用微软云服务（Azure）
- 需要 Cosmos DB 支持
- 复杂的实体关系建模
- 企业级.NET应用

## 🆘 获取帮助

- **技术支持**: felix@hongfei8.com
- **官方网站**: www.hongfei8.cn

---

🎉 现在您已经成功独立了 EF Core 实现！享受更灵活和强大的数据访问体验吧！
