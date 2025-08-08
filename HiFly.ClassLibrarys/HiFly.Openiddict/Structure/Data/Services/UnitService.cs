// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Structure.Data.Interfaces;
using HiFly.Openiddict.Structure.Data.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiFly.Openiddict.Structure.Data.Services;

public class UnitService<TContext, TUnit>(IDbContextFactory<TContext> factory) : IUnitService
    where TContext : DbContext
    where TUnit : class, IUnit, new()
{
    private readonly IDbContextFactory<TContext> _dbFactory = factory;

    /// <summary>
    /// 根据ID获取简称
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetShortNameById(string id)
    {
        using var context = _dbFactory.CreateDbContext();

        var org = context.Set<TUnit>().FirstOrDefault(o => o.Id == id);

        return org?.ShortName ?? "";
    }

    /// <summary>
    /// 根据ID获取全称
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetFullNameById(string id)
    {
        using var context = _dbFactory.CreateDbContext();

        var org = context.Set<TUnit>().FirstOrDefault(o => o.Id == id);

        return org?.FullName ?? "";
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<bool> IsEnableByIdAsync(string userId)
    {
        using var context = _dbFactory.CreateDbContext();

        var org = await context.Set<TUnit>().FirstOrDefaultAsync(o => o.Id == userId);

        return org?.Enable ?? false;
    }




}
