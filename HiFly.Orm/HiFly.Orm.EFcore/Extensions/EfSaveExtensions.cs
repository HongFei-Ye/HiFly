// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HiFly.Orm.EFcore.Extensions;

/// <summary>
/// 保存操作扩展方法
/// </summary>
internal static class EfSaveExtensions
{
    /// <summary>
    /// 处理添加操作（支持并发冲突处理）
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

            // 对于身份实体，确保并发标识是最新的
            EnsureFreshConcurrencyToken(item);

            // 添加新实体
            await context.Set<TItem>().AddAsync(item);
            
            // 确保 PostgreSQL DateTime 兼容性
            context.EnsurePostgreSqlDateTimeCompatibility();
            
            var result = await context.SaveChangesAsync();
            return result > 0;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "添加实体时发生并发冲突: {EntityType}", typeof(TItem).Name);
            return await HandleAddConcurrencyConflictAsync(context, item, primaryKey, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "添加实体时发生错误: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 处理更新操作（支持并发冲突处理）
    /// </summary>
    internal static async Task<bool> HandleUpdateAsync<TContext, TItem>(
        TContext context,
        TItem item,
        IKey primaryKey,
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        var maxRetries = 3;
        var currentRetry = 0;

        while (currentRetry < maxRetries)
        {
            try
            {
                var existingEntity = await FindEntityByKeyAsync(context, item, primaryKey);

                if (existingEntity == null)
                {
                    logger.LogWarning("尝试更新不存在的实体: {EntityType}", typeof(TItem).Name);
                    return false;
                }

                // 保存原始值用于比较
                var originalEntry = context.Entry(existingEntity);
                var originalValues = originalEntry.OriginalValues.Clone();

                // 更新实体值
                originalEntry.CurrentValues.SetValues(item);
                
                // 确保 PostgreSQL DateTime 兼容性
                context.EnsurePostgreSqlDateTimeCompatibility();
                
                var result = await context.SaveChangesAsync();
                return result > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                currentRetry++;
                logger.LogWarning(ex, "更新实体时发生并发冲突（重试 {Retry}/{MaxRetries}): {EntityType}", 
                    currentRetry, maxRetries, typeof(TItem).Name);

                if (currentRetry >= maxRetries)
                {
                    return await HandleUpdateConcurrencyConflictAsync(context, item, primaryKey, logger);
                }

                // 清除变更跟踪，准备重试
                foreach (var entry in context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                // 短暂延迟后重试
                await Task.Delay(100 * currentRetry);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "更新实体时发生错误: {EntityType}", typeof(TItem).Name);
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// 处理添加操作的并发冲突
    /// </summary>
    private static async Task<bool> HandleAddConcurrencyConflictAsync<TContext, TItem>(
        TContext context,
        TItem item,
        IKey primaryKey,
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            // 清除所有更改
            foreach (var trackedEntry in context.ChangeTracker.Entries())
            {
                trackedEntry.State = EntityState.Detached;
            }

            // 检查实体是否现在存在
            var existingEntity = await FindEntityByKeyAsync(context, item, primaryKey);
            if (existingEntity != null)
            {
                logger.LogInformation("实体已存在，无需添加: {EntityType}", typeof(TItem).Name);
                return true; // 实体已存在，视为成功
            }

            // 重新生成并发标识并尝试添加
            EnsureFreshConcurrencyToken(item);
            await context.Set<TItem>().AddAsync(item);
            
            var result = await context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理添加并发冲突失败: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 处理更新操作的并发冲突
    /// </summary>
    private static async Task<bool> HandleUpdateConcurrencyConflictAsync<TContext, TItem>(
        TContext context,
        TItem item,
        IKey primaryKey,
        ILogger logger)
        where TContext : DbContext
        where TItem : class, new()
    {
        try
        {
            // 清除所有更改
            foreach (var trackedEntry in context.ChangeTracker.Entries())
            {
                trackedEntry.State = EntityState.Detached;
            }

            // 重新加载最新的实体
            var freshEntity = await FindEntityByKeyAsync(context, item, primaryKey);
            if (freshEntity == null)
            {
                logger.LogWarning("要更新的实体不存在: {EntityType}", typeof(TItem).Name);
                return false;
            }

            // 使用最新实体进行更新
            var entry = context.Entry(freshEntity);
            entry.CurrentValues.SetValues(item);
            
            // 更新并发标识
            EnsureFreshConcurrencyToken(freshEntity);
            
            var result = await context.SaveChangesAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "处理更新并发冲突失败: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// 确保并发令牌是最新的
    /// </summary>
    private static void EnsureFreshConcurrencyToken<TItem>(TItem item)
        where TItem : class
    {
        // 检查是否有ConcurrencyStamp属性
        var concurrencyStampProperty = typeof(TItem).GetProperty("ConcurrencyStamp");
        if (concurrencyStampProperty != null && concurrencyStampProperty.CanWrite)
        {
            concurrencyStampProperty.SetValue(item, Guid.NewGuid().ToString());
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
