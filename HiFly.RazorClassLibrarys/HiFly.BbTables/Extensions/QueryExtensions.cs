// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using System.Linq.Expressions;

namespace HiFly.BbTables.Extensions;

public static class QueryExtensions
{
    #region  FilterKeyValueAction 查询扩展

    /// <summary>
    /// 属性过滤器
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="query"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IQueryable<TItem> PropertyFilter<TItem>(this IQueryable<TItem> query, FilterKeyValueAction filter)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var combinedExpression = BuildExpression(filter, parameter);

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<TItem, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// 递归构建属性过滤表达式
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static Expression? BuildExpression_Old(FilterKeyValueAction filter, ParameterExpression parameter)
    {
        Expression? combinedExpression = null;

        if (filter.FieldKey != null)
        {
            var property = Expression.Property(parameter, filter.FieldKey);
            var conditionExpression = GetConditionExpression(property, filter.FieldValue, filter.FilterAction);

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
                var subExpression = BuildExpression(subFilter, parameter);

                if (subExpression == null)
                {
                    continue;
                }

                combinedExpression = combinedExpression switch
                {
                    null => subExpression,
                    _ => subFilter.FilterLogic switch
                    {
                        FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                        FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                        _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{filter.FilterLogic}' 这个参数")
                    }
                };

            }
        }

        return combinedExpression;
    }

    private static Expression? BuildExpression(FilterKeyValueAction filter, ParameterExpression parameter)
    {
        Expression? combinedExpression = null;

        // 如果有字段键，构建表达式
        if (filter.FieldKey != null)
        {
            var property = Expression.Property(parameter, filter.FieldKey);
            var conditionExpression = GetConditionExpression(property, filter.FieldValue, filter.FilterAction);

            // 根据 filter.FilterLogic 组合当前条件表达式
            combinedExpression = combinedExpression switch
            {
                null => conditionExpression,  // 如果没有组合的表达式，则直接使用当前条件
                _ => filter.FilterLogic switch  // 根据 filter.FilterLogic 处理组合逻辑
                {
                    FilterLogic.And => Expression.AndAlso(combinedExpression, conditionExpression),
                    FilterLogic.Or => Expression.OrElse(combinedExpression, conditionExpression),
                    _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{filter.FilterLogic}' 这个参数")
                }
            };
        }

        // 如果有子过滤器，则递归处理子过滤器
        if (filter.Filters != null && filter.Filters.Count != 0)
        {
            combinedExpression = BuildSubExpressions(filter.Filters, parameter, combinedExpression);
        }

        return combinedExpression;  // 返回组合后的表达式
    }

    // 递归处理子过滤器的逻辑
    private static Expression? BuildSubExpressions(List<FilterKeyValueAction> filters, ParameterExpression parameter, Expression? combinedExpression)
    {
        foreach (var filter in filters)
        {
            // 递归调用子过滤器，得到子表达式
            var subExpression = BuildExpression(filter, parameter);

            if (subExpression != null)
            {
                // 根据子过滤器的 FilterLogic 处理子表达式与现有的 combinedExpression
                combinedExpression = combinedExpression switch
                {
                    null => subExpression,  // 如果没有组合的表达式，则直接使用子表达式
                    _ => filter.FilterLogic switch  // 使用 subFilter.FilterLogic 来组合
                    {
                        FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                        FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                        _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{filter.FilterLogic}' 这个参数")
                    }
                };
            }
        }

        return combinedExpression;  // 返回组合后的表达式
    }


    #endregion



    #region  自动识别类型过滤器

    /// <summary>
    /// 自动识别类型过滤器
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="query"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IQueryable<TItem> AutoFilter<TItem>(this IQueryable<TItem> query, PropertyFilterParameters? filter)
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
    /// <typeparam name="TItem"></typeparam>
    /// <param name="filter"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Expression? RecursiveBuildAutoExpression<TItem>(PropertyFilterParameters filter, ParameterExpression parameter)
    {
        Expression? combinedExpression = null;

        var conditionExpression = BuildAutoExpression<TItem>(filter, parameter);
        if (conditionExpression != null)
        {
            // 根据 filter.FilterLogic 组合当前条件表达式
            combinedExpression = combinedExpression switch
            {
                null => conditionExpression,  // 如果没有组合的表达式，则直接使用当前条件
                _ => filter.FilterLogic switch  // 根据 filter.FilterLogic 处理组合逻辑
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
                // 递归调用子过滤器，得到子表达式
                var subExpression = BuildAutoExpression<TItem>(subFilter, parameter);
                if (subExpression != null)
                {
                    // 根据子过滤器的 FilterLogic 处理子表达式与现有的 combinedExpression
                    combinedExpression = combinedExpression switch
                    {
                        null => subExpression,  // 如果没有组合的表达式，则直接使用子表达式
                        _ => subFilter.FilterLogic switch  // 使用 subFilter.FilterLogic 来组合
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
    /// <typeparam name="TItem"></typeparam>
    /// <param name="filter"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
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
    /// <typeparam name="TItem"></typeparam>
    /// <param name="filter"></param>
    /// <returns></returns>
    private static FilterFieldType? GetFilterFieldType<TItem>(PropertyFilterParameters filter)
    {
        if (filter == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(filter.ReferenceTypeField) == false && string.IsNullOrEmpty(filter.ValueTypeField) == false)
        {
            // 获取访问属性表达式
            var parentProperty = Expression.Property(Expression.Parameter(typeof(TItem), "x"), filter.ReferenceTypeField);

            if (IsCollectionType(parentProperty.Type) == true)
            {
                return FilterFieldType.CollectionType;
            }
            else
            {
                return FilterFieldType.ClassType;
            }
        }
        else if (string.IsNullOrEmpty(filter.ReferenceTypeField) == true && string.IsNullOrEmpty(filter.ValueTypeField) == false)
        {
            return FilterFieldType.ValueType;
        }

        return null;
    }

    #endregion

    #region  基础类型过滤器

    /// <summary>
    /// 基础类型过滤器
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="query"></param>
    /// <param name="filter"></param>
    /// <param name="isRecursive"></param>
    /// <returns></returns>
    public static IQueryable<TItem> ValueTypeFilter<TItem>(this IQueryable<TItem> query, PropertyFilterParameters filter, bool isRecursive = false)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var combinedExpression = ValueTypeBuildExpression(filter, parameter, isRecursive);

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<TItem, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// 递归构建基础类型过滤表达式
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="parameter"></param>
    /// <param name="isRecursive"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static Expression? ValueTypeBuildExpression_Old(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        // 如果有值类型字段，构建表达式
        if (filter.ValueTypeField != null)
        {
            var property = Expression.Property(parameter, filter.ValueTypeField);
            var conditionExpression = GetConditionExpression(property, filter.MatchValue, filter.FilterAction);

            // 根据 filter.FilterLogic 组合当前条件表达式
            combinedExpression = combinedExpression switch
            {
                null => conditionExpression,  // 如果没有组合的表达式，则直接使用当前条件
                _ => filter.FilterLogic switch  // 根据 filter.FilterLogic 处理组合逻辑
                {
                    FilterLogic.And => Expression.AndAlso(combinedExpression, conditionExpression),
                    FilterLogic.Or => Expression.OrElse(combinedExpression, conditionExpression),
                    _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{filter.FilterLogic}' 这个参数")
                }
            };
        }

        // 如果有子过滤器，并且递归标志为 true，则递归处理子过滤器
        if (filter.Filters != null && filter.Filters.Count != 0 && isRecursive)
        {
            foreach (var subFilter in filter.Filters)
            {
                // 递归调用子过滤器，得到子表达式
                var subExpression = ValueTypeBuildExpression(subFilter, parameter, true);

                if (subExpression != null)
                {
                    // 根据子过滤器的 FilterLogic 处理子表达式与现有的 combinedExpression
                    combinedExpression = combinedExpression switch
                    {
                        null => subExpression,  // 如果没有组合的表达式，则直接使用子表达式
                        _ => subFilter.FilterLogic switch  // 使用 subFilter.FilterLogic 来组合
                        {
                            FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                            FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                            _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{subFilter.FilterLogic}' 这个参数")
                        }
                    };
                }
            }
        }

        return combinedExpression;  // 返回组合后的表达式
    }

    private static Expression? ValueTypeBuildExpression(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        // 如果有值类型字段，构建表达式
        if (filter.ValueTypeField != null)
        {
            var property = Expression.Property(parameter, filter.ValueTypeField);
            var conditionExpression = GetConditionExpression(property, filter.MatchValue, filter.FilterAction);

            // 根据 filter.FilterLogic 组合当前条件表达式
            combinedExpression = combinedExpression switch
            {
                null => conditionExpression,  // 如果没有组合的表达式，则直接使用当前条件
                _ => filter.FilterLogic switch  // 根据 filter.FilterLogic 处理组合逻辑
                {
                    FilterLogic.And => Expression.AndAlso(combinedExpression, conditionExpression),
                    FilterLogic.Or => Expression.OrElse(combinedExpression, conditionExpression),
                    _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{filter.FilterLogic}' 这个参数")
                }
            };
        }

        // 如果有子过滤器，并且递归标志为 true，则递归处理子过滤器
        if (filter.Filters != null && filter.Filters.Count != 0 && isRecursive)
        {
            combinedExpression = RecursiveBuildValueTypes(filter.Filters, parameter, combinedExpression);
        }

        return combinedExpression;  // 返回组合后的表达式
    }

    /// <summary>
    /// 递归处理子过滤器的逻辑
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="parameter"></param>
    /// <param name="combinedExpression"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static Expression? RecursiveBuildValueTypes(List<PropertyFilterParameters> filters, ParameterExpression parameter, Expression? combinedExpression)
    {
        foreach (var filter in filters)
        {
            // 递归调用子过滤器，得到子表达式
            var subExpression = ValueTypeBuildExpression(filter, parameter, true);

            if (subExpression != null)
            {
                // 根据子过滤器的 FilterLogic 处理子表达式与现有的 combinedExpression
                combinedExpression = combinedExpression switch
                {
                    null => subExpression,  // 如果没有组合的表达式，则直接使用子表达式
                    _ => filter.FilterLogic switch  // 使用 subFilter.FilterLogic 来组合
                    {
                        FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                        FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                        _ => throw new NotImplementedException($"未实现 FilterLogic 的 '{filter.FilterLogic}' 这个参数")
                    }
                };
            }
        }

        return combinedExpression;  // 返回组合后的表达式
    }

    #endregion

    #region  集合类型过滤器

    /// <summary>
    /// 是否为集合类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsCollectionType(Type type)
    {
        return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) || type.GetGenericTypeDefinition() == typeof(ICollection<>) || type.GetGenericTypeDefinition() == typeof(List<>));
    }

    /// <summary>
    /// 集合类型过滤器
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="query"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static IQueryable<TItem> CollectionTypeFilter<TItem>(this IQueryable<TItem> query, PropertyFilterParameters? filter, bool isRecursive = false)
    {
        if (filter == null)
        {
            return query;
        }

        // 参数表达式，表示实体类型
        var parameter = Expression.Parameter(typeof(TItem), "x");

        // 构建组合表达式
        var combinedExpression = CollectionTypeBuildExpression<TItem>(filter, parameter, isRecursive);

        if (combinedExpression != null)
        {
            // 创建 Lambda 表达式
            var lambda = Expression.Lambda<Func<TItem, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// 构建集合类型过滤表达式
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="filter"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static Expression CollectionTypeBuildExpression_Old<TItem>(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        if (!string.IsNullOrEmpty(filter.ReferenceTypeField) && !string.IsNullOrEmpty(filter.ValueTypeField))
        {
            // 获取 类属性 表达式
            var parentProperty = Expression.Property(parameter, filter.ReferenceTypeField);

            if (IsCollectionType(parentProperty.Type) == true)
            {
                if (string.IsNullOrEmpty(filter.ValueTypeField) == false)
                {
                    // 获取集合中的元素类型
                    var itemType = parentProperty.Type.GetGenericArguments()[0];

                    // 构建表示集合中元素的参数表达式
                    var lambdaParameter = Expression.Parameter(itemType, "ur");

                    // 获取 子属性(类集合的类的属性)
                    var childProperty = Expression.Property(lambdaParameter, filter.ValueTypeField);

                    // 根据 FilterAction 枚举值构建不同的条件表达式
                    var conditionExpression = GetConditionExpression(childProperty, filter.MatchValue, filter.FilterAction);

                    // 构建 Any 方法的调用表达式
                    var anyCall = Expression.Call(
                        typeof(Enumerable),
                        nameof(Enumerable.Any),
                        [itemType],
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
            else
            {
                throw new ArgumentException($"父属性 {filter.ReferenceTypeField} 不是泛型集合。");
            }
        }

        if (filter.Filters != null && filter.Filters.Count > 0 && isRecursive == true)
        {
            foreach (var subFilter in filter.Filters)
            {
                var subFilterExpression = CollectionTypeBuildExpression<TItem>(subFilter, parameter);

                if (subFilterExpression != null)
                {
                    combinedExpression = combinedExpression switch
                    {
                        null => subFilterExpression,
                        _ => subFilter.FilterLogic switch
                        {
                            FilterLogic.And => Expression.AndAlso(combinedExpression, subFilterExpression),
                            FilterLogic.Or => Expression.OrElse(combinedExpression, subFilterExpression),
                            _ => throw new NotImplementedException($"FilterLogic '{subFilter.FilterLogic}' 尚未实现。")
                        }
                    };
                }
            }
        }

        combinedExpression ??= Expression.Constant(true);

        return combinedExpression;
    }

    public static Expression CollectionTypeBuildExpression<TItem>(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        if (!string.IsNullOrEmpty(filter.ReferenceTypeField) && !string.IsNullOrEmpty(filter.ValueTypeField))
        {
            // 获取类属性表达式
            var parentProperty = Expression.Property(parameter, filter.ReferenceTypeField);

            if (IsCollectionType(parentProperty.Type) == true)
            {
                if (!string.IsNullOrEmpty(filter.ValueTypeField))
                {
                    // 获取集合中的元素类型
                    var itemType = parentProperty.Type.GetGenericArguments()[0];

                    // 构建表示集合中元素的参数表达式
                    var lambdaParameter = Expression.Parameter(itemType, "ur");

                    // 获取子属性（类集合的类的属性）
                    var childProperty = Expression.Property(lambdaParameter, filter.ValueTypeField);

                    // 根据 FilterAction 枚举值构建不同的条件表达式
                    var conditionExpression = GetConditionExpression(childProperty, filter.MatchValue, filter.FilterAction);

                    // 构建 Any 方法的调用表达式
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
            else
            {
                throw new ArgumentException($"父属性 {filter.ReferenceTypeField} 不是泛型集合。");
            }
        }

        // 如果有子过滤器，且递归标志为 true，则递归处理子过滤器
        if (filter.Filters != null && filter.Filters.Count > 0 && isRecursive)
        {
            combinedExpression = RecursiveBuildCollectionTypes(filter.Filters, parameter, combinedExpression);
        }

        // 如果没有匹配的表达式，返回常量 true
        combinedExpression ??= Expression.Constant(true);

        return combinedExpression;
    }

    /// <summary>
    /// 递归处理子过滤器的逻辑
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="parameter"></param>
    /// <param name="combinedExpression"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static Expression? RecursiveBuildCollectionTypes(List<PropertyFilterParameters> filters, ParameterExpression parameter, Expression? combinedExpression)
    {
        foreach (var filter in filters)
        {
            // 递归调用 CollectionTypeBuildExpression，得到子表达式
            var subExpression = CollectionTypeBuildExpression<object>(filter, parameter, true);

            if (subExpression != null)
            {
                // 根据子过滤器的 FilterLogic 处理子表达式与现有的 combinedExpression
                combinedExpression = combinedExpression switch
                {
                    null => subExpression,  // 如果没有组合的表达式，则直接使用子表达式
                    _ => filter.FilterLogic switch  // 使用 subFilter.FilterLogic 来组合
                    {
                        FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                        FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                        _ => throw new NotImplementedException($"FilterLogic '{filter.FilterLogic}' 尚未实现。")
                    }
                };
            }
        }

        return combinedExpression;  // 返回组合后的表达式
    }


    #endregion

    #region  Class类型过滤器


    /// <summary>
    /// Class类型过滤器
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="query"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    public static IQueryable<TItem> ClassTypeFilter<TItem>(this IQueryable<TItem> query, PropertyFilterParameters? filter, bool isRecursive = false)
    {
        if (filter == null)
        {
            return query;
        }

        // 参数表达式，表示实体类型
        var parameter = Expression.Parameter(typeof(TItem), "x");

        var combinedExpression = ClassTypeBuildExpression<TItem>(filter, parameter, isRecursive);

        if (combinedExpression != null)
        {
            var lambda = Expression.Lambda<Func<TItem, bool>>(combinedExpression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// 递归构建Class类型过滤表达式
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="filter"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static Expression ClassTypeBuildExpression_Old<TItem>(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        if (!string.IsNullOrEmpty(filter.ReferenceTypeField) && !string.IsNullOrEmpty(filter.ValueTypeField))
        {
            // 获取 类属性 表达式
            var parentProperty = Expression.Property(parameter, filter.ReferenceTypeField);

            // 获取 类属性的属性 表达式
            var childProperty = Expression.Property(parentProperty, filter.ValueTypeField);

            // 构建 类属性 不为空的条件表达式
            var notNull = Expression.NotEqual(parentProperty, Expression.Constant(null));

            // 根据 FilterAction 枚举值构建不同的条件表达式
            var conditionExpression = GetConditionExpression(childProperty, filter.MatchValue, filter.FilterAction);

            // 构建两个条件的 AND 表达式
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

        if (filter.Filters != null && filter.Filters.Count > 0 && isRecursive == true)
        {
            foreach (var subFilter in filter.Filters)
            {
                var subFilterExpression = ClassTypeBuildExpression<TItem>(subFilter, parameter);

                if (subFilterExpression == null)
                {
                    continue;
                }

                combinedExpression = combinedExpression switch
                {
                    null => subFilterExpression,
                    _ => subFilter.FilterLogic switch
                    {
                        FilterLogic.And => Expression.AndAlso(combinedExpression, subFilterExpression),
                        FilterLogic.Or => Expression.OrElse(combinedExpression, subFilterExpression),
                        _ => throw new NotImplementedException($"FilterLogic '{subFilter.FilterLogic}' is not implemented.")
                    }
                };
            }
        }

        combinedExpression ??= Expression.Constant(true);

        return combinedExpression;
    }

    public static Expression ClassTypeBuildExpression<TItem>(PropertyFilterParameters filter, ParameterExpression parameter, bool isRecursive = false)
    {
        Expression? combinedExpression = null;

        if (!string.IsNullOrEmpty(filter.ReferenceTypeField) && !string.IsNullOrEmpty(filter.ValueTypeField))
        {
            // 获取类属性表达式
            var parentProperty = Expression.Property(parameter, filter.ReferenceTypeField);

            // 获取类属性的属性表达式
            var childProperty = Expression.Property(parentProperty, filter.ValueTypeField);

            // 构建类属性不为空的条件表达式
            var notNull = Expression.NotEqual(parentProperty, Expression.Constant(null));

            // 根据 FilterAction 枚举值构建不同的条件表达式
            var conditionExpression = GetConditionExpression(childProperty, filter.MatchValue, filter.FilterAction);

            // 构建两个条件的 AND 表达式
            var notNullConditionExpression = Expression.AndAlso(notNull, conditionExpression);

            // 根据 filter.FilterLogic 处理组合逻辑
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

        // 如果有子过滤器，并且需要递归，则递归处理子过滤器
        if (filter.Filters != null && filter.Filters.Count > 0 && isRecursive)
        {
            combinedExpression = RecursiveBuildClassTypes(filter.Filters, parameter, combinedExpression);
        }

        // 如果没有匹配的表达式，返回常量 true
        combinedExpression ??= Expression.Constant(true);

        return combinedExpression;
    }

    /// <summary>
    /// 递归处理子过滤器的逻辑
    /// </summary>
    /// <param name="subFilters"></param>
    /// <param name="parameter"></param>
    /// <param name="combinedExpression"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static Expression? RecursiveBuildClassTypes(List<PropertyFilterParameters> subFilters, ParameterExpression parameter, Expression? combinedExpression)
    {
        foreach (var subFilter in subFilters)
        {
            // 递归调用 ClassTypeBuildExpression，得到子表达式
            var subExpression = ClassTypeBuildExpression<object>(subFilter, parameter, true);

            if (subExpression != null)
            {
                // 根据子过滤器的 FilterLogic 处理子表达式与现有的 combinedExpression
                combinedExpression = combinedExpression switch
                {
                    null => subExpression,  // 如果没有组合的表达式，则直接使用子表达式
                    _ => subFilter.FilterLogic switch  // 使用 subFilter.FilterLogic 来组合
                    {
                        FilterLogic.And => Expression.AndAlso(combinedExpression, subExpression),
                        FilterLogic.Or => Expression.OrElse(combinedExpression, subExpression),
                        _ => throw new NotImplementedException($"FilterLogic '{subFilter.FilterLogic}' is not implemented.")
                    }
                };
            }
        }

        return combinedExpression;  // 返回组合后的表达式
    }

    #endregion



    /// <summary>
    /// 获取条件表达式
    /// </summary>
    /// <param name="memberExpression"></param>
    /// <param name="matchValue"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static Expression GetConditionExpression(MemberExpression memberExpression, object? matchValue, FilterAction action)
    {
        var constantExpression = Expression.Constant(matchValue);

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



}
