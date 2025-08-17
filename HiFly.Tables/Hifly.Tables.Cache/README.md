# HiFly.Tables 动态多级混合缓存系统

## 概述

HiFly.Tables.Cache 是一个为 HiFly.Tables 组件设计的高性能多级缓存系统，提供了内存缓存(L1)和分布式缓存(L2)的混合解决方案。

## 特性

### 🚀 多级缓存架构
- **L1缓存**: 高速内存缓存，毫秒级访问
- **L2缓存**: 分布式缓存(Redis)，跨实例共享
- **智能降级**: 自动降级到可用的缓存层级

### 📊 智能缓存策略
- **动态过期时间**: 根据查询类型和数据特征自动调整
- **热点数据识别**: 优先缓存频繁访问的数据
- **树形结构优化**: 专门优化树形表格的缓存策略

### 🔧 灵活的配置
- **可配置的缓存层级**: 支持启用/禁用不同缓存层
- **细粒度控制**: 支持实体级别的缓存控制
- **监控统计**: 实时缓存命中率和性能统计

### 🌳 树形结构支持
- **完整子树缓存**: 一次查询缓存完整树结构
- **智能无效化**: 数据变更时智能清理相关缓存

## 快速开始

### 1. 配置服务

在 `Program.cs` 中添加缓存服务：

```csharp
// 添加Table缓存服务
services.AddTableCache(configuration);

// 自动注册所有实体的带缓存CRUD服务
services.AddAllCachedGenericCrudServices<YourDbContext>();
```

### 2. 配置文件

在 `appsettings.json` 中添加缓存配置：

```json
{
  "Cache": {
    "DefaultExpirationMinutes": 30,
    "EnableDistributedCache": false,
    "RedisConnectionString": "localhost:6379",
    "KeyPrefix": "HiFly:Tables:",
    "MemoryCache": {
      "MaxItems": 10000,
      "SizeLimitMB": 100
    }
  }
}
```

### 3. 在组件中使用

```csharp
@inject CachedGenericCrudService<YourDbContext, YourEntity> CrudService

<TItemTable TContext="YourDbContext" TItem="YourEntity"
            OnQueryAsync="OnQueryAsync"
            OnSaveAsync="OnSaveAsync"
            OnDeleteAsync="OnDeleteAsync" />

@code {
    private async Task<QueryData<YourEntity>> OnQueryAsync(QueryPageOptions options)
    {
        // 自动使用缓存的查询
        return await CrudService.OnQueryAsync(options);
    }

    private async Task<bool> OnSaveAsync(YourEntity item, ItemChangedType changedType)
    {
        // 保存时自动清理相关缓存
        return await CrudService.OnSaveAsync(item, changedType);
    }

    private async Task<bool> OnDeleteAsync(IEnumerable<YourEntity> items)
    {
        // 删除时自动清理相关缓存
        return await CrudService.OnDeleteAsync(items);
    }
}
```

## 高级功能

### 缓存预热

```csharp
// 预热常用查询
await CrudService.WarmupCommonQueriesAsync(pageSize: 20, maxPages: 3);

// 自定义预热
var commonQueries = new[]
{
    new QueryPageOptions { PageIndex = 1, PageItems = 50 },
    new QueryPageOptions { PageIndex = 1, PageItems = 50, SortName = "Name", SortOrder = SortOrder.Asc }
};
await CrudService.WarmupCacheAsync(commonQueries);
```

### 缓存监控

```razor
@* 添加缓存管理组件 *@
<CacheManagerComponent ShowDetailedStats="true" AutoRefresh="true" />
```

### 手动缓存控制

```csharp
// 获取统计信息
var stats = await CrudService.GetCacheStatisticsAsync();

// 清除实体缓存
await CrudService.ClearEntityCacheAsync();

// 清除所有缓存
await _cacheService.ClearAllAsync();
```

## 缓存键设计

### 查询缓存键格式
```
HiFly:Tables:Query:{EntityName}:{QueryHash}
```

### 实体缓存键格式
```
HiFly:Tables:Entity:{EntityName}:{EntityId}
```

### 树形缓存键格式
```
HiFly:Tables:Tree:{EntityName}:{ParentId}:depth{Depth}
```

## 性能优化建议

### 1. 内存缓存配置
- **MaxItems**: 根据可用内存调整，建议 10000-50000
- **SizeLimitMB**: 建议设置为可用内存的 10-20%
- **CompactionPercentage**: 建议 0.2-0.3

### 2. 分布式缓存配置
- **Redis连接池**: 配置适当的连接池大小
- **过期时间**: 平衡数据新鲜度和性能需求
- **压缩**: 对大数据集启用压缩

### 3. 查询优化
- **分页查询**: 避免一次性查询大量数据
- **索引优化**: 确保数据库索引合理
- **预热策略**: 在应用启动时预热热点数据

## 监控指标

### 缓存命中率
- **优秀**: > 80%
- **良好**: 60-80%
- **需优化**: < 60%

### 内存使用
- 监控内存缓存的使用情况
- 避免内存泄漏和过度使用

### 响应时间
- L1缓存: < 1ms
- L2缓存: < 10ms
- 数据库查询: 监控并优化慢查询

## 故障排除

### 常见问题

1. **缓存未生效**
   - 检查服务注册是否正确
   - 验证配置文件格式
   - 查看日志中的错误信息

2. **内存使用过高**
   - 调整MaxItems和SizeLimitMB
   - 检查是否有内存泄漏
   - 考虑启用压缩

3. **Redis连接问题**
   - 验证连接字符串
   - 检查网络连接
   - 查看Redis服务器状态

### 调试技巧

1. **启用详细日志**
```json
{
  "Logging": {
    "LogLevel": {
      "HiFly.Tables.Cache": "Debug"
    }
  }
}
```

2. **使用缓存管理组件**
```razor
<CacheManagerComponent ShowDetailedStats="true" />
```

3. **检查缓存统计**
```csharp
var stats = await _cacheService.GetStatisticsAsync();
foreach (var kvp in stats)
{
    Console.WriteLine($"{kvp.Key}: 命中率 {kvp.Value.HitRate:P}");
}
```

## 版本历史

### v1.0.0
- 初始版本
- 支持多级缓存架构
- 支持树形结构缓存
- 提供缓存管理组件

## 许可证

Copyright (c) 弘飞帮联科技有限公司. All rights reserved.

## 联系方式

- 官方网站: www.hongfei8.cn
- 邮箱: felix@hongfei8.com
- 备用邮箱: hongfei8@outlook.com
