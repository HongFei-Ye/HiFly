// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace HiFly.Orm.EFcore.Extensions;

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
