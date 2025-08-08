// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

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
