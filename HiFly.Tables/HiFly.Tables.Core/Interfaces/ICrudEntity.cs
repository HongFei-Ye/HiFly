// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

namespace HiFly.Tables.Core.Interfaces;

/// <summary>
/// 标记接口，用于标识需要 CRUD 服务的实体
/// </summary>
/// <remarks>
/// 实现此接口的实体类将被自动识别为需要 CRUD 服务的实体。
/// 这是一个空的标记接口，不需要实现任何方法。
/// </remarks>
public interface ICrudEntity
{
    // 标记接口，无需实现任何成员
}
