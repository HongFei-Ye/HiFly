// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.Structure.Data.Services.Interfaces;

public interface IDepartmentService
{
    /// <summary>
    /// 根据ID获取简称
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetShortNameById(string id);

    /// <summary>
    /// 根据ID获取全称
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetFullNameById(string id);

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<bool> IsEnableByIdAsync(string userId);

}
