# HiFly.Tables 内存缓存系统

## 概述

HiFly.Tables.Cache 是一个为 HiFly.Tables 组件设计的高性能内存缓存系统，提供了快速、可靠的数据缓存解决方案。

## 特性

### 🚀 高性能内存缓存
- **毫秒级访问**: 超快的内存缓存访问速度
- **智能过期**: 自动管理缓存过期和清理
- **内存优化**: 智能的内存使用和压缩策略

### 📊 智能缓存策略
- **动态过期时间**: 根据查询类型和数据特征自动调整
- **热点数据识别**: 优先缓存频繁访问的数据
- **树形结构优化**: 专门优化树形表格的缓存策略

### 🔧 灵活的配置
- **细粒度控制**: 支持实体级别的缓存控制
- **监控统计**: 实时缓存命中率和性能统计
- **内存限制**: 可配置的内存使用限制和清理策略

### 🌳 树形结构支持
- **完整子树缓存**: 一次查询缓存完整树结构
- **智能无效化**: 数据变更时智能清理相关缓存

## 快速开始

### 1. 配置服务

在 `Program.cs` 中添加缓存服务：

```csharp
// 添加Table缓存服务
services.AddTableCache(configuration);

// 自动注册所有实体的带缓存数据服务
services.AddCacheForAllDataServices();
```

### 2. 配置文件

在 `appsettings.json` 中添加缓存配置：

```json
{
  "Cache": {
    "DefaultExpirationMinutes": 30,
    "KeyPrefix": "HiFly:Tables:",
    "EnableStatistics": true,
    "MemoryCache": {
      "MaxItems": 10000,
      "SizeLimitMB": 100,
      "ExpirationScanFrequencySeconds": 60,
      "CompactionPercentage": 0.25
    }
  }
}
```

### 3. 在组件中使用

```csharp
@inject IHiFlyDataService<YourEntity> DataService

<TItemTable TItem="YourEntity"
            OnQueryAsync="OnQueryAsync"
            OnSaveAsync="OnSaveAsync"
            OnDeleteAsync="OnDeleteAsync" />

@code {
    private async Task<QueryData<YourEntity>> OnQueryAsync(QueryPageOptions options)
    {
        // 自动使用缓存的查询
        return await DataService.QueryAsync(options);
    }

    private async Task<bool> OnSaveAsync(YourEntity item, ItemChangedType changedType)
    {
        // 保存时自动清理相关缓存
        return await DataService.SaveAsync(item, changedType);
    }

    private async Task<bool> OnDeleteAsync(IEnumerable<YourEntity> items)
    {
        // 删除时自动清理相关缓存
        return await DataService.DeleteAsync(items);
    }
}
```

## 高级功能

### 缓存预热

```csharp
// 预热常用查询（如果使用CachedDataService）
if (DataService is CachedDataService<YourEntity> cachedService)
{
    await cachedService.WarmupCommonQueriesAsync(pageSize: 20, maxPages: 3);
}
```

### 手动缓存控制

```csharp
// 获取统计信息
if (DataService is CachedDataService<YourEntity> cachedService)
{
    var stats = await cachedService.GetCacheStatisticsAsync();

    // 清除实体缓存
    await cachedService.ClearEntityCacheAsync();
}

// 直接操作缓存服务
@inject IMultiLevelCacheService CacheService

// 清除所有缓存
await CacheService.ClearAllAsync();
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
- **ExpirationScanFrequency**: 根据数据更新频率调整

### 2. 查询优化
- **分页查询**: 避免一次性查询大量数据
- **索引优化**: 确保数据库索引合理
- **预热策略**: 在应用启动时预热热点数据

### 3. 内存管理
- **监控内存使用**: 定期检查内存缓存占用
- **合理设置过期时间**: 平衡性能和数据新鲜度
- **避免内存泄漏**: 确保及时清理不再需要的缓存

## 监控指标

### 缓存命中率
- **优秀**: > 80%
- **良好**: 60-80%
- **需优化**: < 60%

### 内存使用
- 监控内存缓存的使用情况
- 避免内存泄漏和过度使用

### 响应时间
- 内存缓存: < 1ms
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
   - 减少缓存过期时间

3. **性能不佳**
   - 检查缓存命中率
   - 优化查询策略
   - 调整预热策略

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

2. **检查缓存统计**
```csharp
@inject IMultiLevelCacheService CacheService

var stats = await CacheService.GetStatisticsAsync();
foreach (var kvp in stats)
{
    Console.WriteLine($"{kvp.Key}: 命中率 {kvp.Value.HitRate:P}");
}
```

## 配置示例

### 基本配置
```json
{
  "Cache": {
    "DefaultExpirationMinutes": 30,
    "MemoryCache": {
      "MaxItems": 10000,
      "SizeLimitMB": 100
    }
  }
}
```

### 高性能配置
```json
{
  "Cache": {
    "DefaultExpirationMinutes": 60,
    "MemoryCache": {
      "MaxItems": 50000,
      "SizeLimitMB": 500,
      "ExpirationScanFrequencySeconds": 30,
      "CompactionPercentage": 0.2
    }
  }
}
```

### 内存受限配置
```json
{
  "Cache": {
    "DefaultExpirationMinutes": 15,
    "MemoryCache": {
      "MaxItems": 5000,
      "SizeLimitMB": 50,
      "ExpirationScanFrequencySeconds": 120,
      "CompactionPercentage": 0.4
    }
  }
}
```

## 版本历史

### v2.0.0
- 简化为纯内存缓存架构
- 移除Redis和分布式缓存依赖
- 优化内存使用和性能
- 简化配置和使用方式

### v1.0.0
- 初始版本
- 支持多级缓存架构
- 支持树形结构缓存

## 许可证

Copyright (c) 弘飞帮联科技有限公司. All rights reserved.

## 联系方式

- 官方网站: www.hongfei8.cn
- 邮箱: felix@hongfei8.com
- 备用邮箱: hongfei8@outlook.com
