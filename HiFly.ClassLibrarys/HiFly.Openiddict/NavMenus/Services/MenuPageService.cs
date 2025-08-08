// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.NavMenus.Interfaces;
using HiFly.Openiddict.NavMenus.Models;
using HiFly.Openiddict.NavMenus.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HiFly.Openiddict.NavMenus.Services;

/// <summary>
/// 导航页面服务
/// </summary>
/// <typeparam name="TContext"></typeparam>
/// <param name="factory"></param>
public class MenuPageService<TContext, TItem>(IDbContextFactory<TContext> factory) : IMenuPageService
    where TContext : DbContext
    where TItem : class, IMenuPage, new()
{
    private readonly IDbContextFactory<TContext> _dbFactory = factory;


    /// <summary>
    /// 获取导航页面名称
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    public string GetTextById(string id)
    {
        using var context = _dbFactory.CreateDbContext();

        var navigationPage = context.Set<TItem>().FirstOrDefault(np => np.Id == id);

        return navigationPage?.Text ?? "";
    }

    /// <summary>
    /// 获取导航页面图标
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    public string GetIconById(string id)
    {
        using var context = _dbFactory.CreateDbContext();

        var navigationPage = context.Set<TItem>().FirstOrDefault(np => np.Id == id);

        return navigationPage?.Icon ?? "";
    }

    /// <summary>
    /// 获取导航页面Url
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    public string GetUrlById(string id)
    {
        using var context = _dbFactory.CreateDbContext();

        var navigationPage = context.Set<TItem>().FirstOrDefault(np => np.Id == id);

        return navigationPage?.Url ?? "";
    }


    /// <summary>
    /// 增加或者更新其他验证Json
    /// </summary>
    /// <param name="navMenuId"></param>
    /// <param name="key"></param>
    /// <param name="authorizeJson"></param>
    /// <returns></returns>
    public async Task<bool> AddOrUpdateOtherAuthorizeJson(string id, string key, JsonAuthorize jsonAuthorize)
    {
        using var context = _dbFactory.CreateDbContext();

        // 获取目录信息
        var navigationPage = await context.Set<TItem>().FirstOrDefaultAsync(np => np.Id == id);
        if (navigationPage == null)
        {
            return false;
        }

        var jsonText = navigationPage.OtherAuthorizeJson;

        // 使用 System.Text.Json 进行反序列化
        var authorizeDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonAuthorize>>(jsonText ?? "") ?? []; // 默认空字典

        // 使用传入的键作为字典的键
        if (!string.IsNullOrEmpty(key))
        {
            if (authorizeDictionary.TryGetValue(key, out var value))
            {
                // 如果键存在，则修改对应的值
                value.UserIds = jsonAuthorize.UserIds;
                value.RoleIds = jsonAuthorize.RoleIds;
                value.RoleHierarchie = jsonAuthorize.RoleHierarchie;
            }
            else
            {
                // 如果键不存在，则添加新的键值对
                authorizeDictionary.Add(key, jsonAuthorize);
            }
        }

        // 将更新后的字典转换回JSON文本
        var updatedJsonText = JsonSerializer.Serialize(authorizeDictionary);

        // 更新数据库中的字段
        navigationPage.OtherAuthorizeJson = updatedJsonText;

        var result = await context.SaveChangesAsync();

        return result > 0;

    }

    /// <summary>
    /// 获取其他验证Json文本
    /// </summary>
    /// <param name="navMenuId"></param>
    /// <returns></returns>
    public async Task<string?> GetOtherAuthorizeJsonString(string id)
    {
        using var context = _dbFactory.CreateDbContext();

        // 获取目录信息
        var navigationPage = await context.Set<TItem>().FirstOrDefaultAsync(np => np.Id == id);
        if (navigationPage == null)
        {
            return null;
        }

        var jsonText = navigationPage.OtherAuthorizeJson;


        return jsonText;
    }

    /// <summary>
    /// 保存其他验证Json文本
    /// </summary>
    /// <param name="navMenuId"></param>
    /// <param name="AuthorizeJsonString"></param>
    /// <returns></returns>
    public async Task<bool> SaveAuthorizeJson(string id, string AuthorizeJsonString)
    {
        using var context = _dbFactory.CreateDbContext();

        // 获取目录信息
        var navigationPage = await context.Set<TItem>().FirstOrDefaultAsync(np => np.Id == id);
        if (navigationPage == null)
        {
            return false;
        }

        // 更新数据库中的字段
        navigationPage.OtherAuthorizeJson = AuthorizeJsonString;

        var result = await context.SaveChangesAsync();

        return result > 0;
    }







}

