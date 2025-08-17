# HiFly.Tables 项目组

HiFly.Tables 是 HiFly.BbTables 的拆分重构版本，采用分层架构设计，提供了更好的可维护性和可扩展性。

## 🏗️ 项目结构

### HiFly.Tables.Core (核心抽象层)
- **核心模型和接口**
- **属性过滤参数**
- **枚举定义**
- **特性标记**

**主要文件：**
- `Models/DataOperationVerification.cs` - 数据操作权限验证
- `Models/PropertyFilterParameters.cs` - 属性过滤参数
- `Enums/FilterFieldType.cs` - 过滤器字段类型
- `Interfaces/ICrudService.cs` - CRUD 服务接口
- `Interfaces/ITableComponent.cs` - 表格组件接口
- `Attributes/CrudEntityAttribute.cs` - CRUD 实体特性

### HiFly.Tables.Services (业务服务层)
- **泛型 CRUD 服务**
- **查询扩展方法**
- **业务逻辑处理**

**主要文件：**
- `GenericCrudService.cs` - 泛型 CRUD 服务实现
- `Extensions/QueryExtensions.cs` - 查询扩展方法
- `Extensions/FilterKeyValueActionExtensions.cs` - 过滤器扩展
- `Extensions/SaveExtensions.cs` - 保存操作扩展
- `Extensions/DeleteExtensions.cs` - 删除操作扩展
- `Extensions/TreeExtensions.cs` - 树形结构扩展
- `Extensions/ServiceCollectionExtensions.cs` - 服务注册扩展

### HiFly.Tables.Components (Blazor 组件层)
- **Blazor 表格组件**
- **UI 相关功能**
- **组件样式**

**主要文件：**
- `TItemTable.razor` - 主要表格组件
- `TItemTable.razor.cs` - 表格组件逻辑
- `TItemTable.razor.css` - 表格组件样式
- `Components/LoadingTip.razor` - 加载提示组件

### HiFly.Tables.Controllers (Web API 控制器层)
- **Web API 控制器基类**
- **HTTP 接口定义**

**主要文件：**
- `GenericControllerBase.cs` - 泛型控制器基类

## 📦 依赖关系

```
Components -> Services -> Core
Controllers -> Services -> Core
```

## 🚀 快速开始

### 1. 安装包引用

在你的项目中添加必要的包引用：

```xml
<PackageReference Include="BootstrapBlazor" Version="9.9.2" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.8" />
<PackageReference Include="AutoMapper" Version="15.0.1" />
```

### 2. 项目引用

根据需要添加项目引用：

```xml
<!-- 如果只需要核心功能 -->
<ProjectReference Include="..\HiFly.Tables.Core\HiFly.Tables.Core.csproj" />

<!-- 如果需要服务层 -->
<ProjectReference Include="..\HiFly.Tables.Services\HiFly.Tables.Services.csproj" />

<!-- 如果需要 Blazor 组件 -->
<ProjectReference Include="..\HiFly.Tables.Components\HiFly.Tables.Components.csproj" />

<!-- 如果需要 Web API 控制器 -->
<ProjectReference Include="..\HiFly.Tables.Controllers\HiFly.Tables.Controllers.csproj" />
```

### 3. 服务注册

在 `Program.cs` 中注册服务：

```csharp
using HiFly.Tables.Services.Extensions;

// 注册所有 CRUD 服务
builder.Services.AddAllGenericCrudServices<YourDbContext>();

// 或者基于特性注册
builder.Services.AddGenericCrudServicesByAttribute<YourDbContext>();

// 或者手动注册单个服务
builder.Services.AddGenericCrudService<YourDbContext, YourEntity>();
```

### 4. 使用表格组件

```razor
@page "/your-page"
@using HiFly.Tables.Components
@using YourNamespace.Data

<TItemTable TContext="YourDbContext" TItem="YourEntity">
    <TableColumns>
        <TableColumn @bind-Field="@context.Id" Text="ID" />
        <TableColumn @bind-Field="@context.Name" Text="名称" />
        <TableColumn @bind-Field="@context.CreateTime" Text="创建时间" />
    </TableColumns>
</TItemTable>
```

### 5. 使用控制器

```csharp
[Route("api/[controller]")]
public class YourEntityController : GenericControllerBase<YourDbContext, YourEntity, YourEntityService>
{
    public YourEntityController(
        ILogger<YourEntityController> logger,
        ICrudService<YourDbContext, YourEntity> crudService) 
        : base(logger, crudService)
    {
    }

    protected override object GetEntityId(YourEntity entity)
    {
        return entity.Id;
    }

    protected override async Task<ActionResult<YourEntity>> GetEntityByIdAsync(object id)
    {
        // 实现根据 ID 获取实体的逻辑
        // ...
    }
}
```

## 🔧 高级配置

### 实体特性标记

使用 `CrudEntityAttribute` 标记实体：

```csharp
[CrudEntity("用户实体", EnableTreeMode = false, DefaultSortField = "CreateTime")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreateTime { get; set; }
}
```

### 树形结构支持

对于支持树形结构的实体，确保有 `Id` 和 `ParentId` 属性：

```csharp
[CrudEntity("菜单实体", EnableTreeMode = true)]
public class Menu
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; }
}
```

### 自定义过滤参数

```csharp
var filterParams = new PropertyFilterParameters
{
    ValueTypeField = "Name",
    MatchValue = "搜索值",
    FilterAction = FilterAction.Contains,
    FilterLogic = FilterLogic.And
};
```

## 🔄 迁移指南

### 从 HiFly.BbTables 迁移

1. **命名空间更改：**
   ```csharp
   // 旧的
   using HiFly.BbTables;
   
   // 新的
   using HiFly.Tables.Core.Models;
   using HiFly.Tables.Services;
   using HiFly.Tables.Components;
   ```

2. **服务注册更改：**
   ```csharp
   // 旧的
   services.AddScoped<GenericCrudService<TContext, TItem>>();
   
   // 新的
   services.AddGenericCrudService<TContext, TItem>();
   ```

3. **组件使用保持不变：**
   ```razor
   <!-- 组件用法基本保持一致 -->
   <TItemTable TContext="YourDbContext" TItem="YourEntity">
       <!-- ... -->
   </TItemTable>
   ```

## 🎯 最佳实践

1. **服务注册：** 推荐使用 `AddGenericCrudServicesByAttribute` 方法自动注册标记了特性的实体服务
2. **权限控制：** 使用 `DataOperationVerification` 控制用户的增删改查权限
3. **性能优化：** 对于大数据量，合理使用分页和过滤参数
4. **错误处理：** 在控制器中实现适当的错误处理和日志记录

## 📄 版本兼容性

- **目标框架：** .NET 9
- **BootstrapBlazor：** 9.9.2+
- **Entity Framework Core：** 9.0.8+

## 🤝 贡献

欢迎提交 Issue 和 Pull Request 来改进项目。

---

**注意：** 原有的 `HiFly.BbTables` 项目将保留，新项目可以逐步迁移使用。
