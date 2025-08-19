// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using HiFly.Identity.Data.Interfaces;
using HiFly.Identity.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiFly.Identity.Services;

public class UserService<TContext, TItem>(IDbContextFactory<TContext> factory) : IUserService
    where TContext : DbContext
    where TItem : class, IUser, new()
{
    private readonly IDbContextFactory<TContext> _dbFactory = factory;


    /// <summary>
    /// 根据用户ID获取用户名称
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public string GetUserNameById(Guid userId)
    {
        using var context = _dbFactory.CreateDbContext();

        var user = context.Set<TItem>().FirstOrDefault(u => u.Id == userId);

        return user?.UserName ?? "";
    }

    /// <summary>
    /// 根据用户名称获取用户ID
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task<Guid?> GetUserIdByName(string userName)
    {
        using var context = _dbFactory.CreateDbContext();

        var user = await context.Set<TItem>().FirstOrDefaultAsync(u => u.UserName == userName);


        return user?.Id;
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<bool> IsEnableByIdAsync(Guid userId)
    {
        using var context = _dbFactory.CreateDbContext();

        var user = await context.Set<TItem>().FirstOrDefaultAsync(u => u.Id == userId);

        return user?.Enable ?? false;
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task<bool> IsEnableByNameAsync(string userName)
    {
        using var context = _dbFactory.CreateDbContext();

        var user = await context.Set<TItem>().FirstOrDefaultAsync(u => u.UserName == userName);

        return user?.Enable ?? false;
    }



    /// <summary>
    /// 设置用户登录首选角色
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="loginedRole"></param>
    /// <returns></returns>
    public async Task<bool> SetLoginedRoleAsync(Guid userId, string loginedRole)
    {
        using var context = _dbFactory.CreateDbContext();

        var user = await context.Set<TItem>().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return false;
        }

        if (user.LoginedRole != loginedRole)
        {
            user.LoginedRole = loginedRole;

            var result = context.SaveChanges();

            return result > 0;
        }


        return true;
    }

    /// <summary>
    /// 获取登录首选角色ID
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<string> GetLoginedRoleByIdAsync(Guid userId)
    {
        using var context = _dbFactory.CreateDbContext();

        var user = await context.Set<TItem>().FirstOrDefaultAsync(u => u.Id == userId);

        return user?.LoginedRole ?? "";
    }



    /// <summary>
    /// 设置QQ号码
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="qQNumber"></param>
    /// <returns></returns>
    //public async Task<bool> SetQQNumberAsync(Guid userId, string qQNumber)
    //{
    //    using var context = _dbFactory.CreateDbContext();

    //    var user = await context.Set<TItem>().FirstOrDefaultAsync(u => u.Id == userId);

    //    if (user == null)
    //    {
    //        return false;
    //    }

    //    user.QQNumber = qQNumber;

    //    var result = await context.SaveChangesAsync();

    //    return result > 0;
    //}

    /// <summary>
    /// 设置微信号码
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="weiXinNumber"></param>
    /// <returns></returns>
    //public async Task<bool> SetWeChatNumberAsync(Guid userId, string weChatNumber)
    //{
    //    using var context = _dbFactory.CreateDbContext();

    //    var user = context.Set<TItem>().FirstOrDefault(u => u.Id == userId);

    //    if (user == null)
    //    {
    //        return false;
    //    }

    //    user.WeChatNumber = weChatNumber;

    //    var result = await context.SaveChangesAsync();

    //    return result > 0;
    //}





}
