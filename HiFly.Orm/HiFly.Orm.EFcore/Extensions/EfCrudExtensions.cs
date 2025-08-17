// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace HiFly.Orm.EFcore.Extensions;

/// <summary>
/// Entity Framework Core 查询扩展方法
/// </summary>
internal static class EfQueryExtensions
{
    /// <summary>
    /// 应用简单的过滤逻辑
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="filters">过滤参数</param>
    /// <returns>过滤后的查询</returns>
    internal static IQueryable<TItem> ApplySimpleFilter<TItem>(
        this IQueryable<TItem> query, 
        PropertyFilterParameters filters)
        where TItem : class
    {
        // 简化的过滤实现
        // TODO: 实现完整的过滤逻辑
        return query;
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="sortName">排序字段</param>
    /// <param name="sortOrder">排序方向</param>
    /// <returns>排序后的查询</returns>
    internal static IQueryable<TItem> ApplySort<TItem>(
        this IQueryable<TItem> query,
        string sortName,
        SortOrder sortOrder)
        where TItem : class
    {
        if (string.IsNullOrEmpty(sortName))
            return query;

        try
        {
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, sortName);
            var lambda = Expression.Lambda(property, parameter);

            var methodName = sortOrder == SortOrder.Asc ? "OrderBy" : "OrderByDescending";
            var orderByMethod = typeof(Queryable).GetMethods()
                .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
                .FirstOrDefault()?
                .MakeGenericMethod(typeof(TItem), property.Type);

            if (orderByMethod != null)
            {
                return (IQueryable<TItem>)orderByMethod.Invoke(null, new object[] { query, lambda })!;
            }
        }
        catch
        {
            // 排序失败时返回原查询
        }

        return query;
    }
}

/// <summary>
/// 保存操作扩展方法
/// </summary>
internal static class EfSaveExtensions
{
    /// <summary>
    /// 处理添加操作
    /// </summary>
    internal static async Task<bool> HandleAddAsync<TContext, TItem>(
        TContext context, 
        TItem item, 
        IKey primaryKey, 
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            // 检查实体是否已存在
            var entityExists = await CheckEntityExistsAsync(context, item, primaryKey);

            if (entityExists)
            {
                logger.LogWarning("尝试添加已存在的实体: {EntityType}", typeof(TItem).Name);
                return false;
            }

            // 添加新实体
            await context.Set<TItem>().AddAsync(item);
            var result = await context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加实体时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 处理更新操作
    /// </summary>
    internal static async Task<bool> HandleUpdateAsync<TContext, TItem>(
        TContext context, 
        TItem item, 
        IKey primaryKey, 
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            var existingEntity = await FindEntityByKeyAsync(context, item, primaryKey);

            if (existingEntity != null)
            {
                context.Entry(existingEntity).CurrentValues.SetValues(item);
                var result = await context.SaveChangesAsync();
                return result > 0;
            }

            logger.LogWarning("尝试更新不存在的实体: {EntityType}", typeof(TItem).Name);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "更新实体时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    private static async Task<bool> CheckEntityExistsAsync<TContext, TItem>(
        TContext context, 
        TItem item, 
        IKey primaryKey)
        where TContext : DbContext
        where TItem : class, new()
    {
        var existingEntity = await FindEntityByKeyAsync(context, item, primaryKey);
        return existingEntity != null;
    }

    /// <summary>
    /// 根据主键查找实体
    /// </summary>
    internal static async Task<TItem?> FindEntityByKeyAsync<TContext, TItem>(
        TContext context, 
        TItem item, 
        IKey primaryKey)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            // 提取主键值
            var keyValues = primaryKey.Properties
                .Select(p => typeof(TItem).GetProperty(p.Name)?.GetValue(item))
                .ToArray();

            // 检查主键值是否有null
            if (keyValues.Any(v => v == null))
            {
                return null;
            }

            // 使用 FindAsync
            return await context.Set<TItem>().FindAsync(keyValues);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// 删除操作扩展方法
/// </summary>
internal static class EfDeleteExtensions
{
    /// <summary>
    /// 处理标准删除
    /// </summary>
    internal static async Task<bool> HandleStandardDeleteAsync<TContext, TItem>(
        TContext context, 
        List<TItem> items, 
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            context.Set<TItem>().RemoveRange(items);
            var result = await context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除实体时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 处理树形删除
    /// </summary>
    internal static async Task<bool> HandleTreeDeleteAsync<TContext, TItem>(
        TContext context, 
        List<TItem> items, 
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            var (idProp, parentIdProp) = ValidateTreeProperties<TItem>();
            var allItemsToDelete = new List<TItem>();

            foreach (var item in items)
            {
                allItemsToDelete.Add(item);
                await CollectChildrenForDeletion(context, item, allItemsToDelete, idProp, parentIdProp);
            }

            context.Set<TItem>().RemoveRange(allItemsToDelete);
            var result = await context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "树形删除时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 收集要删除的子节点
    /// </summary>
    private static async Task CollectChildrenForDeletion<TContext, TItem>(
        TContext context, 
        TItem parent, 
        List<TItem> collector, 
        PropertyInfo idProp, 
        PropertyInfo parentIdProp)
        where TContext : DbContext
        where TItem : class, new()
    {
        var parentId = idProp.GetValue(parent);
        if (parentId == null) return;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, parentIdProp);
        var value = Expression.Constant(parentId, parentIdProp.PropertyType);
        var equalExpression = Expression.Equal(property, value);
        var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

        var children = await context.Set<TItem>().Where(lambda).ToListAsync();

        foreach (var child in children)
        {
            collector.Add(child);
            await CollectChildrenForDeletion(context, child, collector, idProp, parentIdProp);
        }
    }

    /// <summary>
    /// 验证树形结构必需的属性
    /// </summary>
    private static (PropertyInfo idProp, PropertyInfo parentIdProp) ValidateTreeProperties<TItem>()
        where TItem : class
    {
        var idProp = typeof(TItem).GetProperty("Id");
        var parentIdProp = typeof(TItem).GetProperty("ParentId");

        if (idProp == null)
        {
            throw new InvalidOperationException($"树形表格要求 {typeof(TItem).Name} 类有 Id 属性");
        }

        if (parentIdProp == null)
        {
            throw new InvalidOperationException($"树形表格要求 {typeof(TItem).Name} 类有 ParentId 属性");
        }

        return (idProp, parentIdProp);
    }
}

/// <summary>
/// 树形结构扩展方法
/// </summary>
internal static class EfTreeExtensions
{
    /// <summary>
    /// 加载子节点
    /// </summary>
    internal static async Task LoadChildNodesAsync<TContext, TItem>(
        TContext context, 
        TItem parent, 
        List<TItem> collector, 
        PropertyInfo idProp, 
        PropertyInfo parentIdProp, 
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            var parentId = idProp.GetValue(parent);
            if (parentId == null) return;

            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);
            var value = Expression.Constant(parentId, parentIdProp.PropertyType);
            var equalExpression = Expression.Equal(property, value);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            var children = await context.Set<TItem>().Where(lambda).ToListAsync();

            foreach (var child in children)
            {
                collector.Add(child);
                await LoadChildNodesAsync(context, child, collector, idProp, parentIdProp, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "加载子节点时发生错误: {EntityType}", typeof(TItem).Name);
        }
    }
}
