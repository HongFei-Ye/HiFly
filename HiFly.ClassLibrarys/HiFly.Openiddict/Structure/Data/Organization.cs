// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Structure.Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HiFly.Openiddict.Structure.Data;

public class Organization : IOrganization
{
    [Key]
    [DisplayName("识别码")]
    public virtual string Id { get; set; } = Guid.NewGuid().ToString();

    [DisplayName("创建时间(UTC)")]
    public virtual DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [DisplayName("简称")]
    [Required(ErrorMessage = "请输入{0}")]
    public string ShortName { get; set; } = default!;

    [DisplayName("全称")]
    [Required(ErrorMessage = "请输入{0}")]
    [ProtectedPersonalData]
    public string FullName { get; set; } = default!;

    [DisplayName("负责人")]
    public string? ManagerId { get; set; }

    [DisplayName("描述")]
    public string? Description { get; set; }

    [DisplayName("是否启用")]
    public bool Enable { get; set; } = true;
}
