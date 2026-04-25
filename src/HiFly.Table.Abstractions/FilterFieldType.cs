// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;

namespace HiFly.Table;

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
