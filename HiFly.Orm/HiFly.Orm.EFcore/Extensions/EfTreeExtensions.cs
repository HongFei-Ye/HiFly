// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
