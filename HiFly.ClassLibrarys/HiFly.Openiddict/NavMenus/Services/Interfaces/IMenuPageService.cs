// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.NavMenus.Models;

namespace HiFly.Openiddict.NavMenus.Services.Interfaces;

public interface IMenuPageService
{
    /// <summary>
    /// 获取导航页面名称
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    string GetTextById(string id);

    /// <summary>
    /// 获取导航页面图标
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    string GetIconById(string id);

    /// <summary>
    /// 获取导航页面Url
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    string GetUrlById(string id);

    /// <summary>
    /// 增加或者更新其他验证Json
    /// </summary>
    /// <param name="navMenuId"></param>
    /// <param name="key"></param>
    /// <param name="authorizeJson"></param>
    /// <returns></returns>
    Task<bool> AddOrUpdateOtherAuthorizeJson(string id, string key, JsonAuthorize jsonAuthorize);

    /// <summary>
    /// 获取其他验证Json文本
    /// </summary>
    /// <param name="navMenuId"></param>
    /// <returns></returns>
    Task<string?> GetOtherAuthorizeJsonString(string id);

    /// <summary>
    /// 保存其他验证Json文本
    /// </summary>
    /// <param name="navMenuId"></param>
    /// <param name="AuthorizeJsonString"></param>
    /// <returns></returns>
    Task<bool> SaveAuthorizeJson(string id, string AuthorizeJsonString);




}
