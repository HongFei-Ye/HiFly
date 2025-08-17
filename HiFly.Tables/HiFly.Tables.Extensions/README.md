# HiFly Tables 统一注册使用指南

## 概述

HiFly Tables 现在提供统一的注册方法，简化了服务配置和使用流程。您可以通过一个方法调用完成所有必要的服务注册。

## 快速开始

### 1. 基础配置

在 `Program.cs` 中添加 HiFly Tables 服务：

```csharp
using HiFly.Tables.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 最简单的配置方式
builder.Services.AddHiFlyTables<YourDbContext>(builder.Configuration);

var app = builder.Build();
```

### 2. 自定义配置

```csharp
// 自定义配置选项
builder.Services.AddHiFlyTables<YourDbContext>(builder.Configuration, options =>
{
    options.EnableCache = true;
    options.EnableDevelopmentMode = true;
    options.RegistrationStrategy = ServiceRegistrationStrategy.AutoDetect;
    options.ConfigureCache = cacheOptions =>
    {
        cacheOptions.DefaultExpirationMinutes = 60;
        cacheOptions.EnableDistributedCache = true;
    };
});
```

### 3. 构建器模式配置

```csharp
// 使用构建器模式进行链式配置
builder.Services.AddHiFlyTablesWithBuilder<YourDbContext>(builder.Configuration, builder =>
{
    builder
        .WithCache(cache =>
        {
            cache.DefaultExpirationMinutes = 60;
            cache.EnableDistributedCache = true;
        })
        .WithRegistrationStrategy(ServiceRegistrationStrategy.AutoDetect)
        .WithDevelopmentMode(loggerFactory);
});
```

## 配置选项详解

### HiFlyTablesOptions 配置项

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableCache` | bool | false | 是否启用缓存功能 |
| `CacheConfigurationSection` | string | "Cache" | 缓存配置节点名称 |
| `EnableAutoServiceRegistration` | bool | true | 是否启用自动服务注册 |
| `RegistrationStrategy` | enum | AutoDetect | 服务注册策略 |
| `AssembliesToScan` | List | 空 | 要扫描的程序集列表 |
| `EnableDevelopmentMode` | bool | false | 是否启用开发模式 |

### 服务注册策略

1. **AutoDetect**：自动检测 DbContext 中的 DbSet 属性
2. **MarkerInterface**：基于标记接口扫描
3. **Attribute**：基于特性扫描
4. **Manual**：手动注册（不自动注册任何服务）

## 使用场景

### 场景1：简单项目（无缓存）

```csharp
// Program.cs
builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, enableCache: false);

// 在 Blazor 组件中
@inject ICrudService<MyDbContext, Product> ProductService

<TItemTable TContext="MyDbContext" TItem="Product"
            OnQueryAsync="ProductService.OnQueryAsync"
            OnSaveAsync="ProductService.OnSaveAsync"
            OnDeleteAsync="ProductService.OnDeleteAsync" />
```

### 场景2：高性能项目（启用缓存）

```csharp
// Program.cs
builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, options =>
{
    options.EnableCache = true;
    options.ConfigureCache = cache =>
    {
        cache.DefaultExpirationMinutes = 30;
        cache.EnableDistributedCache = true;
        cache.RedisConnectionString = "localhost:6379";
    };
});

// 在 Blazor 组件中
@inject ICrudService<MyDbContext, Product> ProductService

// 服务会自动使用缓存版本
<TItemTable TContext="MyDbContext" TItem="Product"
            OnQueryAsync="ProductService.OnQueryAsync" />
```

### 场景3：基于标记接口的项目

```csharp
// 实体类标记接口
public interface ICrudEntity { }

public class Product : ICrudEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// Program.cs
builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, options =>
{
    options.RegistrationStrategy = ServiceRegistrationStrategy.MarkerInterface;
    options.AssembliesToScan.Add(typeof(Product).Assembly);
});
```

### 场景4：基于特性的项目

```csharp
// 实体类使用特性
[CrudEntity(Description = "产品管理")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// Program.cs
builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, options =>
{
    options.RegistrationStrategy = ServiceRegistrationStrategy.Attribute;
    options.AssembliesToScan.Add(typeof(Product).Assembly);
});
```

### 场景5：手动控制注册

```csharp
// Program.cs - 禁用自动注册
builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, options =>
{
    options.RegistrationStrategy = ServiceRegistrationStrategy.Manual;
    options.EnableCache = true; // 仍然可以使用缓存基础设施
});

// 手动注册特定服务
builder.Services.AddHiFlyTablesCrudService<MyDbContext, Product>(enableCache: true);
builder.Services.AddHiFlyTablesCrudService<MyDbContext, User>(enableCache: false);
```

## 配置文件示例

### appsettings.json

```json
{
  "Cache": {
    "DefaultExpirationMinutes": 30,
    "EnableDistributedCache": false,
    "RedisConnectionString": "localhost:6379",
    "MemoryCache": {
      "MaxItems": 10000,
      "SizeLimitMB": 100
    }
  }
}
```

### appsettings.Development.json

```json
{
  "Cache": {
    "DefaultExpirationMinutes": 5,
    "EnableDistributedCache": false,
    "MemoryCache": {
      "MaxItems": 1000,
      "SizeLimitMB": 50
    }
  }
}
```

### appsettings.Production.json

```json
{
  "Cache": {
    "DefaultExpirationMinutes": 60,
    "EnableDistributedCache": true,
    "RedisConnectionString": "your-redis-connection-string",
    "MemoryCache": {
      "MaxItems": 50000,
      "SizeLimitMB": 500
    }
  }
}
```

## 最佳实践

### 1. 开发环境配置

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, options =>
    {
        options.EnableCache = false; // 开发时禁用缓存便于调试
        options.EnableDevelopmentMode = true;
        options.LoggerFactory = LoggerFactory.Create(b => b.AddConsole());
    });
}
else
{
    builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, options =>
    {
        options.EnableCache = true; // 生产环境启用缓存
    });
}
```

### 2. 微服务场景

```csharp
// 每个微服务只注册自己需要的实体
builder.Services.AddHiFlyTables<OrderDbContext>(builder.Configuration, options =>
{
    options.RegistrationStrategy = ServiceRegistrationStrategy.Attribute;
    options.AssembliesToScan.Add(typeof(Order).Assembly);
    options.EnableCache = true;
});
```

### 3. 多数据库上下文

```csharp
// 为不同的数据库上下文分别配置
builder.Services.AddHiFlyTables<UserDbContext>(builder.Configuration, options =>
{
    options.EnableCache = true;
    options.CacheConfigurationSection = "UserCache";
});

builder.Services.AddHiFlyTables<ProductDbContext>(builder.Configuration, options =>
{
    options.EnableCache = true;
    options.CacheConfigurationSection = "ProductCache";
});
```

## 迁移指南

### 从旧版本迁移

**旧版本代码：**
```csharp
// 旧版本需要分别注册
builder.Services.AddTableCache(builder.Configuration);
builder.Services.AddAllCachedGenericCrudServices<MyDbContext>();
```

**新版本代码：**
```csharp
// 新版本一行搞定
builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, enableCache: true);
```

### 渐进式迁移

如果您有大量现有代码，可以先使用手动策略：

```csharp
// 第1步：使用手动策略，保持现有注册不变
builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, options =>
{
    options.RegistrationStrategy = ServiceRegistrationStrategy.Manual;
    options.EnableCache = true;
});

// 保持现有的手动注册
builder.Services.AddGenericCrudService<MyDbContext, Product>();

// 第2步：逐步移除手动注册，改为自动检测
// builder.Services.AddHiFlyTables<MyDbContext>(builder.Configuration, enableCache: true);
```

## 注意事项

1. **配置文件**：确保配置文件中有正确的缓存配置节点
2. **依赖注入**：统一注册后，直接注入 `ICrudService<TContext, TItem>` 即可
3. **性能**：生产环境建议启用缓存以提升性能
4. **调试**：开发环境可以启用开发模式获得更详细的日志
5. **版本兼容**：新的统一注册方法向后兼容，不影响现有代码

通过这种统一的注册方式，您可以更简单、更灵活地配置 HiFly Tables，同时保持代码的清晰和可维护性。
