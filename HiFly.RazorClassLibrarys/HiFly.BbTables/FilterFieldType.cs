// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using System.ComponentModel;

namespace HiFly.BbTables;

/// <summary>
/// 过滤器字段类型
/// </summary>
public enum FilterFieldType
{
    [Description("基础类型")]
    ValueType,

    [Description("集合类型")]
    CollectionType,

    [Description("Class类型")]
    ClassType,


}
