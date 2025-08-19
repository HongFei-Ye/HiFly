// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Core.Enums;
using HiFly.Tables.Core.Models;
using System.Linq.Expressions;

namespace HiFly.Orm.EFcore.Extensions;

/// <summary>
/// Entity Framework Core 查询扩展方法
/// </summary>
internal static class EfQueryExtensions
{

    /// <summary>
    /// 应用智能过滤器（自动选择最优的过滤策略）
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="filters">过滤参数</param>
    /// <returns>过滤后的查询</returns>
    internal static IQueryable<TItem> ApplySmartFilter<TItem>(
        this IQueryable<TItem> query,
        PropertyFilterParameters? filters)
        where TItem : class
    {
        if (filters == null)
        {
            return query;
        }

        try
        {
            // 🧠 智能过滤策略：根据字段类型自动选择过滤方法
            return query.ApplyAutoFilter(filters);
        }
        catch (Exception)
        {
            // 最终兜底：返回原查询
            return query;
        }
    }

    /// <summary>
    /// 内部实现的自动识别类型过滤器
    /// </summary>
    /// <typeparam name="TItem">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="filter">过滤参数</param>
    /// <returns>过滤后的查询</returns>
    private static IQueryable<TItem> ApplyAutoFilter<TItem>(this IQueryable<TItem> query, PropertyFilterParameters? filter)
        where TItem : class
    {
        if (filter == null)
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var combinedExpression = RecursiveBuildAutoExpression<TItem>(filter, parameter);

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<TItem, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// 递归构建自动模式过滤表达式
    /// </summary>
    private static Expression? RecursiveBuildAutoExpression<TItem>(PropertyFilterParameters filter, ParameterExpression parameter)
    {
        Expression? combinedExpression = null;

        var conditionExpression = BuildAutoExpression<TItem>(filter, parameter);
        if (conditionExpression != null)
        {
            combinedExpression = combinedExpression switch
            {
                null => conditionExpression,
                _ => filter.FilterLogic switch
                {
                    FilterLogic.And => Expression.AndAlso(combinedExpression, conditionExpression),
                    FilterLogic.Or => Expression.OrElse(combinedExpression, conditionExpression),
                    _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{filter.FilterLogic}' 这个参数")
                }
            };
        }

        if (filter.Filters != null && filter.Filters.Count != 0)
        {
            foreach (var subFilter in filter.Filters)
            {
                var subExpression = BuildAutoExpression<TItem>(subFilter, parameter);
                if (subExpression != null)
                {
                    combinedExpression = combinedExpression switch
                    {
                        null => subExpression,
                        _ => subFilter.FilterLogic switch
                        {
                            FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                            FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                            _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{subFilter.FilterLogic}' 这个参数")
                        }
                    };
                }
            }
        }

        return combinedExpression;
    }

    /// <summary>
    /// 构建自动模式过滤表达式
    /// </summary>
    private static Expression? BuildAutoExpression<TItem>(PropertyFilterParameters filter, ParameterExpression parameter)
    {
        if (filter == null)
        {
            return null;
        }

        filter.FilterFieldType ??= GetFilterFieldType<TItem>(filter);
        if (filter.FilterFieldType == null)
        {
            return null;
        }

        Expression? combinedExpression = null;

        if (filter.FilterFieldType == FilterFieldType.ValueType)
        {
            // 基础类型过滤器
            combinedExpression = ValueTypeBuildExpression(filter, parameter, false);
        }
        else if (filter.FilterFieldType == FilterFieldType.CollectionType)
        {
            // 集合类型过滤器
            combinedExpression = CollectionTypeBuildExpression<TItem>(filter, parameter, false);
        }
        else if (filter.FilterFieldType == FilterFieldType.ClassType)
        {
            // Class类型过滤器
            combinedExpression = ClassTypeBuildExpression<TItem>(filter, parameter, false);
        }

        return combinedExpression;
    }

    /// <summary>
    /// 获取过滤器字段类型
    /// </summary>
    private static FilterFieldType? GetFilterFieldType<TItem>(PropertyFilterParameters filter)
    {
        if (filter == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(filter.ReferenceTypeField) && !string.IsNullOrEmpty(filter.ValueTypeField))
        {
            try
            {
                var parentProperty = Expression.Property(Expression.Parameter(typeof(TItem), "x"), filter.ReferenceTypeField);

                if (IsCollectionType(parentProperty.Type))
                {
                    return FilterFieldType.CollectionType;
                }
                else
                {
                    return FilterFieldType.ClassType;
                }
            }
            catch
            {
                return null;
            }
        }
        else if (string.IsNullOrEmpty(filter.ReferenceTypeField) && !string.IsNullOrEmpty(filter.ValueTypeField))
        {
            return FilterFieldType.ValueType;
        }

        return null;
    }

    /// <summary>
    /// 基础类型过滤表达式构建
    /// </summary>
    private static Expression? ValueTypeBuildExpression(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        if (filter.ValueTypeField != null)
        {
            try
            {
                var property = Expression.Property(parameter, filter.ValueTypeField);
                var conditionExpression = GetConditionExpression(property, filter.MatchValue, filter.FilterAction);

                combinedExpression = combinedExpression switch
                {
                    null => conditionExpression,
                    _ => filter.FilterLogic switch
                    {
                        FilterLogic.And => Expression.AndAlso(combinedExpression, conditionExpression),
                        FilterLogic.Or => Expression.OrElse(combinedExpression, conditionExpression),
                        _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{filter.FilterLogic}' 这个参数")
                    }
                };
            }
            catch
            {
                // 忽略属性访问错误
            }
        }

        if (filter.Filters != null && filter.Filters.Count != 0 && isRecursive)
        {
            foreach (var subFilter in filter.Filters)
            {
                var subExpression = ValueTypeBuildExpression(subFilter, parameter, true);
                if (subExpression != null)
                {
                    combinedExpression = combinedExpression switch
                    {
                        null => subExpression,
                        _ => subFilter.FilterLogic switch
                        {
                            FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                            FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                            _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{subFilter.FilterLogic}' 这个参数")
                        }
                    };
                }
            }
        }

        return combinedExpression;
    }

    /// <summary>
    /// 集合类型过滤表达式构建
    /// </summary>
    private static Expression? CollectionTypeBuildExpression<TItem>(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        if (!string.IsNullOrEmpty(filter.ReferenceTypeField) && !string.IsNullOrEmpty(filter.ValueTypeField))
        {
            try
            {
                var parentProperty = Expression.Property(parameter, filter.ReferenceTypeField);

                if (IsCollectionType(parentProperty.Type))
                {
                    var itemType = parentProperty.Type.GetGenericArguments()[0];
                    var lambdaParameter = Expression.Parameter(itemType, "ur");
                    var childProperty = Expression.Property(lambdaParameter, filter.ValueTypeField);
                    var conditionExpression = GetConditionExpression(childProperty, filter.MatchValue, filter.FilterAction);

                    var anyCall = Expression.Call(
                        typeof(Enumerable),
                        nameof(Enumerable.Any),
                        new[] { itemType },
                        parentProperty,
                        Expression.Lambda(conditionExpression, lambdaParameter)
                    );

                    combinedExpression = combinedExpression switch
                    {
                        null => anyCall,
                        _ => filter.FilterLogic switch
                        {
                            FilterLogic.And => Expression.AndAlso(combinedExpression, anyCall),
                            FilterLogic.Or => Expression.OrElse(combinedExpression, anyCall),
                            _ => throw new NotImplementedException($"FilterLogic '{filter.FilterLogic}' 尚未实现。")
                        }
                    };
                }
            }
            catch
            {
                // 忽略属性访问错误
            }
        }

        if (filter.Filters != null && filter.Filters.Count > 0 && isRecursive)
        {
            foreach (var subFilter in filter.Filters)
            {
                var subExpression = CollectionTypeBuildExpression<TItem>(subFilter, parameter, true);
                if (subExpression != null)
                {
                    combinedExpression = combinedExpression switch
                    {
                        null => subExpression,
                        _ => subFilter.FilterLogic switch
                        {
                            FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                            FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                            _ => throw new NotImplementedException($"FilterLogic '{subFilter.FilterLogic}' 尚未实现。")
                        }
                    };
                }
            }
        }

        return combinedExpression ?? Expression.Constant(true);
    }

    /// <summary>
    /// Class类型过滤表达式构建
    /// </summary>
    private static Expression? ClassTypeBuildExpression<TItem>(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        if (!string.IsNullOrEmpty(filter.ReferenceTypeField) && !string.IsNullOrEmpty(filter.ValueTypeField))
        {
            try
            {
                var parentProperty = Expression.Property(parameter, filter.ReferenceTypeField);
                var childProperty = Expression.Property(parentProperty, filter.ValueTypeField);
                var notNull = Expression.NotEqual(parentProperty, Expression.Constant(null));
                var conditionExpression = GetConditionExpression(childProperty, filter.MatchValue, filter.FilterAction);
                var notNullConditionExpression = Expression.AndAlso(notNull, conditionExpression);

                combinedExpression = combinedExpression switch
                {
                    null => notNullConditionExpression,
                    _ => filter.FilterLogic switch
                    {
                        FilterLogic.And => Expression.AndAlso(combinedExpression, notNullConditionExpression),
                        FilterLogic.Or => Expression.OrElse(combinedExpression, notNullConditionExpression),
                        _ => throw new NotImplementedException($"FilterLogic '{filter.FilterLogic}' is not implemented.")
                    }
                };
            }
            catch
            {
                // 忽略属性访问错误
            }
        }

        if (filter.Filters != null && filter.Filters.Count > 0 && isRecursive)
        {
            foreach (var subFilter in filter.Filters)
            {
                var subExpression = ClassTypeBuildExpression<TItem>(subFilter, parameter, true);
                if (subExpression != null)
                {
                    combinedExpression = combinedExpression switch
                    {
                        null => subExpression,
                        _ => subFilter.FilterLogic switch
                        {
                            FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                            FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                            _ => throw new NotImplementedException($"FilterLogic '{subFilter.FilterLogic}' is not implemented.")
                        }
                    };
                }
            }
        }

        return combinedExpression ?? Expression.Constant(true);
    }

    /// <summary>
    /// 是否为集合类型
    /// </summary>
    private static bool IsCollectionType(Type type)
    {
        return type.IsGenericType && (
            type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
            type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
            type.GetGenericTypeDefinition() == typeof(List<>) ||
            type.GetGenericTypeDefinition() == typeof(IList<>) ||
            typeof(IEnumerable<>).MakeGenericType(type.GetGenericArguments()[0]).IsAssignableFrom(type)
        );
    }

    /// <summary>
    /// 获取条件表达式
    /// </summary>
    private static Expression GetConditionExpression(MemberExpression memberExpression, object? matchValue, FilterAction action)
    {
        var constantExpression = Expression.Constant(matchValue, memberExpression.Type);

        Expression conditionExpression = action switch
        {
            FilterAction.Equal => Expression.Equal(memberExpression, constantExpression),
            FilterAction.NotEqual => Expression.NotEqual(memberExpression, constantExpression),
            FilterAction.GreaterThan => Expression.GreaterThan(memberExpression, constantExpression),
            FilterAction.GreaterThanOrEqual => Expression.GreaterThanOrEqual(memberExpression, constantExpression),
            FilterAction.LessThan => Expression.LessThan(memberExpression, constantExpression),
            FilterAction.LessThanOrEqual => Expression.LessThanOrEqual(memberExpression, constantExpression),
            FilterAction.Contains => Expression.Call(memberExpression, "Contains", null, constantExpression),
            FilterAction.NotContains => Expression.Not(Expression.Call(memberExpression, "Contains", null, constantExpression)),
            _ => throw new NotImplementedException($"未实现 FilterAction 的 '{action}' 这个参数"),
        };

        return conditionExpression;
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

