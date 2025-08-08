// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.Identity.Services.Interfaces;

public interface IUserService
{

    /// <summary>
    /// 根据用户ID获取用户名称
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    string GetUserNameById(string userId);

    /// <summary>
    /// 根据用户名称获取用户ID
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    Task<string?> GetUserIdByName(string userName);

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<bool> IsEnableByIdAsync(string userId);

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    Task<bool> IsEnableByNameAsync(string userName);

    /// <summary>
    /// 设置用户登录首选角色
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="loginedRole"></param>
    /// <returns></returns>
    Task<bool> SetLoginedRoleAsync(string userId, string loginedRole);

    /// <summary>
    /// 获取登录首选角色ID
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<string> GetLoginedRoleByIdAsync(string userId);

    /// <summary>
    /// 设置QQ号码
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="qQNumber"></param>
    /// <returns></returns>
    Task<bool> SetQQNumberAsync(string userId, string qQNumber);

    /// <summary>
    /// 设置微信号码
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="weChatNumber"></param>
    /// <returns></returns>
    Task<bool> SetWeChatNumberAsync(string userId, string weChatNumber);

}
