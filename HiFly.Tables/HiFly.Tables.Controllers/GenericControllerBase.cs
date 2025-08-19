// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HiFly.Tables.Controllers;

/// <summary>
/// 泛型控制器基类，集成 CRUD 功能
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
[ApiController]
public abstract class GenericControllerBase<TEntity>(
    ILogger logger,
    IHiFlyDataService<TEntity> dataService) : ControllerBase
    where TEntity : class, new()
{
    protected readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IHiFlyDataService<TEntity> _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));

    /// <summary>
    /// 通用分页查询方法
    /// </summary>
    [HttpPost("query/simple")]
    public virtual async Task<ActionResult<QueryData<TEntity>>> QueryAsync(
        [FromBody] QueryPageOptions options)
    {
        try
        {
            var result = await _dataService.OnQueryAsync(options, null, false);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询 {EntityType} 时发生错误", typeof(TEntity).Name);
            return StatusCode(500, "内部服务器错误");
        }
    }

    /// <summary>
    /// 通用分页查询方法
    /// </summary>
    [HttpPost("query")]
    public virtual async Task<ActionResult<QueryData<TEntity>>> QueryAsync([FromBody] QueryRequest request)
    {
        try
        {
            var result = await _dataService.OnQueryAsync(request.Options, request.FilterParameters, request.IsTree);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询 {EntityType} 时发生错误", typeof(TEntity).Name);
            return StatusCode(500, "内部服务器错误");
        }
    }

    /// <summary>
    /// 通用新增方法
    /// </summary>
    [HttpPost]
    public virtual async Task<ActionResult<TEntity>> CreateAsync([FromBody] TEntity entity)
    {
        try
        {
            var result = await _dataService.OnSaveAsync(entity, ItemChangedType.Add);
            if (result)
            {
                return CreatedAtAction(nameof(GetByIdAsync), new { id = GetEntityId(entity) }, entity);
            }
            return BadRequest("创建失败");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "创建实体", entity);
        }
    }

    /// <summary>
    /// 通用更新方法
    /// </summary>
    [HttpPut("{id}")]
    public virtual async Task<ActionResult<TEntity>> UpdateAsync([FromRoute] object id, [FromBody] TEntity entity)
    {
        try
        {
            var result = await _dataService.OnSaveAsync(entity, ItemChangedType.Update);
            if (result)
            {
                return Ok(entity);
            }
            return NotFoundResponse(typeof(TEntity).Name, id);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "更新实体", new { id, entity });
        }
    }

    /// <summary>
    /// 通用删除方法
    /// </summary>
    [HttpDelete]
    public virtual async Task<ActionResult> DeleteAsync([FromBody] IEnumerable<TEntity> entities)
    {
        try
        {
            var result = await _dataService.OnDeleteAsync(entities);
            if (result)
            {
                return NoContent();
            }
            return BadRequest("删除失败");
        }
        catch (Exception ex)
        {
            return HandleException(ex, "删除实体", entities);
        }
    }

    /// <summary>
    /// 通用按ID查询方法
    /// </summary>
    [HttpGet("{id}")]
    public virtual async Task<ActionResult<TEntity>> GetByIdAsync(object id)
    {
        try
        {
            // 这里需要子类实现具体的查询逻辑
            return await GetEntityByIdAsync(id);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "查询实体", id);
        }
    }

    /// <summary>
    /// 获取实体ID的抽象方法，子类需要实现
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns>实体ID</returns>
    protected abstract object GetEntityId(TEntity entity);

    /// <summary>
    /// 根据ID获取实体的抽象方法，子类需要实现
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>实体对象</returns>
    protected abstract Task<ActionResult<TEntity>> GetEntityByIdAsync(object id);

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
