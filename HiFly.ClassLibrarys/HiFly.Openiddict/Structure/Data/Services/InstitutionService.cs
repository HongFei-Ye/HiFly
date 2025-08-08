// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.Openiddict.Structure.Data.Interfaces;
using HiFly.Openiddict.Structure.Data.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiFly.Openiddict.Structure.Data.Services;

public class InstitutionService<TContext, TItem>(IDbContextFactory<TContext> factory) : IInstitutionService
    where TContext : DbContext
    where TItem : class, IInstitution, new()
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

        var inst = context.Set<TItem>().FirstOrDefault(d => d.Id == id);

        return inst?.ShortName ?? "";
    }

    /// <summary>
    /// 根据ID获取全称
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public string GetFullNameById(string id)
    {
        using var context = _dbFactory.CreateDbContext();

        var inst = context.Set<TItem>().FirstOrDefault(d => d.Id == id);

        return inst?.FullName ?? "";
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<bool> IsEnableByIdAsync(string userId)
    {
        using var context = _dbFactory.CreateDbContext();

        var inst = await context.Set<TItem>().FirstOrDefaultAsync(d => d.Id == userId);

        return inst?.Enable ?? false;
    }




}
