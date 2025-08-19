// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.BbTables.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HiFly.BbTables.Controllers;

/// <summary>
/// 泛型控制器基类，集成 CRUD 功能
/// </summary>
/// <typeparam name="TContext">数据库上下文</typeparam>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TService">业务服务类型</typeparam>
[ApiController]
public abstract class GenericControllerBase<TContext, TEntity, TService>(
    ILogger logger,
    GenericCrudService<TContext, TEntity> crudService) : ControllerBase
    where TContext : DbContext
    where TEntity : class, new()
    where TService : class
{
    protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly GenericCrudService<TContext, TEntity> _crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));

    /// <summary>
    /// 通用分页查询方法
    /// </summary>
    [HttpPost("query")]
    public virtual async Task<ActionResult<QueryData<TEntity>>> QueryAsync(
        [FromBody] QueryPageOptions options,
        [FromQuery] bool isTree = false)
    {
        try
        {
            var result = await _crudService.OnQueryAsync(options, null, isTree);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询 {EntityType} 时发生错误", typeof(TEntity).Name);
            return StatusCode(500, "内部服务器错误");
        }
    }

    /// <summary>
    /// 通用错误处理方法
    /// </summary>
    protected ActionResult HandleException(Exception ex, string operation, object? context = null)
    {
        _logger.LogError(ex, "执行 {Operation} 时发生错误，上下文: {@Context}", operation, context);
        return StatusCode(500, "内部服务器错误");
    }

    /// <summary>
    /// 通用 NotFound 响应
    /// </summary>
    protected ActionResult NotFoundResponse(string entityName, object identifier)
    {
        return NotFound($"未找到{entityName}，标识符: {identifier}");
    }

}
