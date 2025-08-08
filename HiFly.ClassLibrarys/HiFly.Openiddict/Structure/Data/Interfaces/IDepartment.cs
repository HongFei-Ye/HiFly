// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.Structure.Data.Interfaces;

public interface IDepartment
{
    string Id { get; set; }

    DateTime CreateTime { get; set; }

    string ShortName { get; set; }

    string FullName { get; set; }

    string? ManagerId { get; set; }

    string? Description { get; set; }

    bool Enable { get; set; }
}
