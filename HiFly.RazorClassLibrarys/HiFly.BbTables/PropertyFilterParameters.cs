// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using System.Text.Json.Serialization;

namespace HiFly.BbTables;

/// <summary>
/// 属性过滤参数
/// </summary>
public class PropertyFilterParameters
{
    /// <summary>
    /// 引用类型字段名称
    /// </summary>
    public string? ReferenceTypeField { get; set; }

    /// <summary>
    /// 基础类型字段名称
    /// </summary>
    public string? ValueTypeField { get; set; }

    /// <summary>
    /// 匹配值
    /// </summary>
    public object? MatchValue { get; set; }

    /// <summary>
    /// 获得/设置 Filter 项与其他 Filter 逻辑关系
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FilterLogic FilterLogic { get; set; }

    /// <summary>
    /// 获得/设置 Filter 条件行为
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FilterAction FilterAction { get; set; }

    /// <summary>
    /// 过滤器字段类型
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FilterFieldType? FilterFieldType { get; set; } = null;


    /// <summary>
    /// 获得/设置 子过滤条件集合
    /// </summary>
    public List<PropertyFilterParameters>? Filters { get; set; }


    public PropertyFilterParameters Add(PropertyFilterParameters? propertyFilterParameters)
    {
        if (propertyFilterParameters == null)
        {
            return this;
        }

        //if (ReferenceTypeField == null)
        //{
        //    ReferenceTypeField = classPropertyFilterParameters.ReferenceTypeField;
        //    ValueTypeField = classPropertyFilterParameters.ValueTypeField;
        //    MatchValue = classPropertyFilterParameters.MatchValue;
        //    FilterLogic = classPropertyFilterParameters.FilterLogic;
        //    FilterAction = classPropertyFilterParameters.FilterAction;
        //    return this;
        //}

        Filters ??= [];

        Filters.Add(propertyFilterParameters);

        return this;
    }


}
