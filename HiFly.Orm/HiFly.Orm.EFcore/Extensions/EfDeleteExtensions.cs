// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace HiFly.Orm.EFcore.Extensions;

/// <summary>
/// 删除操作扩展方法
/// </summary>
internal static class EfDeleteExtensions
{
    /// <summary>
    /// 处理标准删除（支持并发冲突处理）
    /// </summary>
    internal static async Task<bool> HandleStandardDeleteAsync<TContext, TItem>(
        TContext context,
        List<TItem> items,
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        if (!items.Any())
        {
            return true; // 空集合视为成功
        }

        try
        {
            // 检查是否为身份实体（具有并发令牌）
            var hasIdentityBase = typeof(TItem).BaseType?.Name.Contains("IdentityUser") == true ||
                                 typeof(TItem).BaseType?.Name.Contains("IdentityRole") == true;

            if (hasIdentityBase)
            {
                return await HandleIdentityEntityDeleteAsync(context, items, logger);
            }
            else
            {
                return await HandleRegularEntityDeleteAsync(context, items, logger);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "删除操作发生并发冲突，尝试重新加载实体: {EntityType}", typeof(TItem).Name);
            return await HandleConcurrencyConflictDeleteAsync(context, items, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除实体时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 处理普通实体删除
    /// </summary>
    private static async Task<bool> HandleRegularEntityDeleteAsync<TContext, TItem>(
        TContext context,
        List<TItem> items,
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        context.Set<TItem>().RemoveRange(items);
        var result = await context.SaveChangesAsync();
        return result > 0;
    }

    /// <summary>
    /// 处理身份实体删除（刷新并发令牌）
    /// </summary>
    private static async Task<bool> HandleIdentityEntityDeleteAsync<TContext, TItem>(
        TContext context,
        List<TItem> items,
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        var deletedCount = 0;

        foreach (var item in items)
        {
            try
            {
                // 为每个实体单独处理删除，避免批量操作的并发问题
                var success = await DeleteSingleEntityWithRefreshAsync(context, item, logger);
                if (success)
                {
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "删除单个实体失败，跳过: {EntityType}", typeof(TItem).Name);
            }
        }

        return deletedCount > 0;
    }

    /// <summary>
    /// 删除单个实体并刷新并发令牌
    /// </summary>
    private static async Task<bool> DeleteSingleEntityWithRefreshAsync<TContext, TItem>(
        TContext context,
        TItem item,
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            // 获取主键值
            var entityType = context.Model.FindEntityType(typeof(TItem));
            var primaryKey = entityType?.FindPrimaryKey();
            if (primaryKey == null)
            {
                logger.LogWarning("无法找到实体主键: {EntityType}", typeof(TItem).Name);
                return false;
            }

            var keyValues = primaryKey.Properties.Select(p => p.PropertyInfo?.GetValue(item)).ToArray();

            // 重新从数据库加载最新实体
            var freshEntity = await context.Set<TItem>().FindAsync(keyValues);
            if (freshEntity == null)
            {
                logger.LogWarning("要删除的实体不存在: {EntityType}", typeof(TItem).Name);
                return false; // 实体已不存在，视为删除成功
            }

            // 删除最新实体
            context.Set<TItem>().Remove(freshEntity);
            var result = await context.SaveChangesAsync();

            return result > 0;
        }
        catch (DbUpdateConcurrencyException)
        {
            // 并发冲突，实体可能已被其他进程删除
            logger.LogInformation("实体可能已被其他进程删除: {EntityType}", typeof(TItem).Name);
            return true; // 视为删除成功
        }
    }

    /// <summary>
    /// 处理并发冲突的删除操作
    /// </summary>
    private static async Task<bool> HandleConcurrencyConflictDeleteAsync<TContext, TItem>(
        TContext context,
        List<TItem> items,
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        // 清除所有更改，重新开始
        foreach (var entry in context.ChangeTracker.Entries())
        {
            entry.State = EntityState.Detached;
        }

        var deletedCount = 0;

        // 逐个处理删除，避免批量操作的并发问题
        foreach (var item in items)
        {
            try
            {
                var success = await DeleteSingleEntityWithRefreshAsync(context, item, logger);
                if (success)
                {
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "重试删除实体失败: {EntityType}", typeof(TItem).Name);
            }
        }

        logger.LogInformation("并发冲突处理完成: {EntityType}, 成功删除 {Count}/{Total}",
            typeof(TItem).Name, deletedCount, items.Count);

        return deletedCount > 0;
    }

    /// <summary>
    /// 处理树形删除（支持并发冲突处理）
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

            return await HandleStandardDeleteAsync(context, allItemsToDelete, logger);
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
