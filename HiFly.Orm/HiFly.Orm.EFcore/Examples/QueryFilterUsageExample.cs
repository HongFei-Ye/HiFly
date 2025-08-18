// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Core.Models;
using HiFly.Tables.Core.Enums;
using HiFly.Orm.EFcore.Services;
using Microsoft.EntityFrameworkCore;

namespace HiFly.Orm.EFcore.Examples;

/// <summary>
/// 查询过滤器使用示例
/// </summary>
public class QueryFilterUsageExample
{
    /// <summary>
    /// 基础类型过滤示例
    /// </summary>
    public static PropertyFilterParameters CreateValueTypeFilter()
    {
        return new PropertyFilterParameters
        {
            ValueTypeField = "Name",
            MatchValue = "张三",
            FilterAction = FilterAction.Contains,
            FilterLogic = FilterLogic.And,
            FilterFieldType = FilterFieldType.ValueType
        };
    }

    /// <summary>
    /// 类类型过滤示例
    /// </summary>
    public static PropertyFilterParameters CreateClassTypeFilter()
    {
        return new PropertyFilterParameters
        {
            ReferenceTypeField = "Department",
            ValueTypeField = "Name",
            MatchValue = "技术部",
            FilterAction = FilterAction.Equal,
            FilterLogic = FilterLogic.And,
            FilterFieldType = FilterFieldType.ClassType
        };
    }

    /// <summary>
    /// 集合类型过滤示例
    /// </summary>
    public static PropertyFilterParameters CreateCollectionTypeFilter()
    {
        return new PropertyFilterParameters
        {
            ReferenceTypeField = "Roles",
            ValueTypeField = "Name",
            MatchValue = "管理员",
            FilterAction = FilterAction.Contains,
            FilterLogic = FilterLogic.And,
            FilterFieldType = FilterFieldType.CollectionType
        };
    }

    /// <summary>
    /// 复合过滤器示例
    /// </summary>
    public static PropertyFilterParameters CreateComplexFilter()
    {
        var mainFilter = new PropertyFilterParameters
        {
            ValueTypeField = "Age",
            MatchValue = 25,
            FilterAction = FilterAction.GreaterThan,
            FilterLogic = FilterLogic.And,
            FilterFieldType = FilterFieldType.ValueType
        };

        // 添加子过滤条件
        mainFilter.Add(new PropertyFilterParameters
        {
            ReferenceTypeField = "Department",
            ValueTypeField = "Name",
            MatchValue = "技术部",
            FilterAction = FilterAction.Equal,
            FilterLogic = FilterLogic.And,
            FilterFieldType = FilterFieldType.ClassType
        });

        mainFilter.Add(new PropertyFilterParameters
        {
            ValueTypeField = "IsActive",
            MatchValue = true,
            FilterAction = FilterAction.Equal,
            FilterLogic = FilterLogic.And,
            FilterFieldType = FilterFieldType.ValueType
        });

        return mainFilter;
    }

    /// <summary>
    /// 在 EfDataService 中使用过滤器的完整示例
    /// </summary>
    public static async Task<QueryData<Employee>> QueryEmployeesWithFilters<TContext>(
        EfDataService<TContext, Employee> dataService)
        where TContext : DbContext
    {
        // 创建查询选项
        var queryOptions = new QueryPageOptions
        {
            PageIndex = 1,
            PageItems = 20,
            SortName = "CreateTime",
            SortOrder = SortOrder.Desc
        };

        // 创建复合过滤条件
        var filters = CreateComplexFilter();

        // 执行查询（会自动应用所有过滤条件）
        return await dataService.OnQueryAsync(queryOptions, filters, false);
    }

    /// <summary>
    /// 动态构建过滤条件示例
    /// </summary>
    public static PropertyFilterParameters BuildDynamicFilter(
        string? searchText = null,
        string? departmentName = null,
        int? minAge = null,
        bool? isActive = null)
    {
        var filter = new PropertyFilterParameters
        {
            FilterLogic = FilterLogic.And
        };

        // 根据搜索文本添加过滤条件
        if (!string.IsNullOrEmpty(searchText))
        {
            filter.Add(new PropertyFilterParameters
            {
                ValueTypeField = "Name",
                MatchValue = searchText,
                FilterAction = FilterAction.Contains,
                FilterLogic = FilterLogic.Or,
                FilterFieldType = FilterFieldType.ValueType
            });

            filter.Add(new PropertyFilterParameters
            {
                ValueTypeField = "Email",
                MatchValue = searchText,
                FilterAction = FilterAction.Contains,
                FilterLogic = FilterLogic.Or,
                FilterFieldType = FilterFieldType.ValueType
            });
        }

        // 根据部门名称添加过滤条件
        if (!string.IsNullOrEmpty(departmentName))
        {
            filter.Add(new PropertyFilterParameters
            {
                ReferenceTypeField = "Department",
                ValueTypeField = "Name",
                MatchValue = departmentName,
                FilterAction = FilterAction.Equal,
                FilterLogic = FilterLogic.And,
                FilterFieldType = FilterFieldType.ClassType
            });
        }

        // 根据最小年龄添加过滤条件
        if (minAge.HasValue)
        {
            filter.Add(new PropertyFilterParameters
            {
                ValueTypeField = "Age",
                MatchValue = minAge.Value,
                FilterAction = FilterAction.GreaterThanOrEqual,
                FilterLogic = FilterLogic.And,
                FilterFieldType = FilterFieldType.ValueType
            });
        }

        // 根据活跃状态添加过滤条件
        if (isActive.HasValue)
        {
            filter.Add(new PropertyFilterParameters
            {
                ValueTypeField = "IsActive",
                MatchValue = isActive.Value,
                FilterAction = FilterAction.Equal,
                FilterLogic = FilterLogic.And,
                FilterFieldType = FilterFieldType.ValueType
            });
        }

        return filter;
    }
}

/// <summary>
/// 示例实体类
/// </summary>
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateTime { get; set; }
    public Department? Department { get; set; }
    public List<Role> Roles { get; set; } = [];
}

/// <summary>
/// 示例部门类
/// </summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 示例角色类
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
