// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using System.ComponentModel;

namespace HiFly.Identity.Data.Interfaces;

public interface IRole
{
    [DisplayName("识别码")]
    Guid Id { get; }

    [DisplayName("角色名称")]
    string? Name { get; set; }

    [DisplayName("显示名称")]
    string? ShowName { get; set; }

    [DisplayName("权限等级")]
    int Hierarchy { get; set; }

    [DisplayName("是否启用")]
    bool Enable { get; set; }
}
