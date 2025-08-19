// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;

namespace HiFly.BbTables.Extensions;

public static class FilterKeyValueActionExtensions
{
    public static PropertyFilterParameters? ToPropertyFilterParameters(this FilterKeyValueAction filterKeyValueAction)
    {
        if (filterKeyValueAction == null)
        {
            return null;
        }

        var _propertyFilterParameters = BuildPropertyFilterParameters(filterKeyValueAction);

        // 处理嵌套过滤器
        if (filterKeyValueAction.Filters != null && filterKeyValueAction.Filters.Count > 0)
        {
            foreach (var nestedFilter in filterKeyValueAction.Filters)
            {
                var nestedPropertyFilter = nestedFilter.ToPropertyFilterParameters();
                if (nestedPropertyFilter != null)
                {
                    if (_propertyFilterParameters == null)
                    {
                        _propertyFilterParameters = nestedPropertyFilter;
                    }
                    else
                    {
                        _propertyFilterParameters.Filters ??= [];
                        _propertyFilterParameters.Filters.Add(nestedPropertyFilter);
                    }
                }
            }
        }

        return _propertyFilterParameters;
    }

    private static PropertyFilterParameters? BuildPropertyFilterParameters(FilterKeyValueAction filterKeyValueAction)
    {
        if (filterKeyValueAction.FieldKey == null)
        {
            return null;
        }

        var propertyFilterParameters = new PropertyFilterParameters
        {
            // 属性映射
            ReferenceTypeField = null,
            ValueTypeField = filterKeyValueAction.FieldKey, // 假设 FieldKey 用于引用类型和基础类型字段
            MatchValue = filterKeyValueAction.FieldValue,
            FilterLogic = filterKeyValueAction.FilterLogic,
            FilterAction = filterKeyValueAction.FilterAction,
            FilterFieldType = null, // // 假设没有具体的字段类型映射，暂时设置为 null
            Filters = []
        };

        return propertyFilterParameters;
    }



}

