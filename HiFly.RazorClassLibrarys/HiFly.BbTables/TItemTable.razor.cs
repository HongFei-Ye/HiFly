// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using AutoMapper;
using BootstrapBlazor.Components;
using HiFly.BbTables.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HiFly.BbTables;

[CascadingTypeParameter(nameof(TItem))]
public partial class TItemTable<TContext, TItem>
    where TContext : DbContext
    where TItem : class, new()
{
    [NotNull]
    [Parameter]
    public TContext? Context { get; set; }


    /// <summary>
    /// 重新设置数据上下文
    /// </summary>
    public void ReSetContext()
    {
        Context = DbFactory.CreateDbContext();
    }


    [Parameter]
    public string TopDivClass { get; set; } = "table-full";

    /// <summary>
    /// 是否为弹窗/对话框
    /// </summary>
    [Parameter]
    public bool IsDialog { get; set; } = false;

    [NotNull]
    private Table<TItem>? TableRef { get; set; }

    /// <summary>
    /// 显示/隐藏 Loading 遮罩
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public ValueTask ToggleLoading(bool v) => TableRef.ToggleLoading(v);

    /// <summary>
    /// 查询按钮调用此方法 参数 pageIndex 默认值 null 保持上次页码 第一页页码为 1
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task QueryAsync() => TableRef.QueryAsync();

    /// <summary>
    /// 获得/设置 组件编辑模式 默认为弹窗编辑行数据 PopupEditForm
    /// </summary>
    [Parameter]
    public EditMode EditMode { get; set; }

    /// <summary>
    /// 获得/设置 EditModel 实例
    /// </summary>
    [Parameter]
    public TItem? EditModel { get; set; }

    /// <summary>
    ///  获得/设置 每页显示数据数量的外部数据源
    /// </summary>
    [Parameter]
    public IEnumerable<int> PageItemsSource { get; set; } = [20, 30, 40, 50, 100, 300, 500, 1000];

    [NotNull]
    [Parameter]
    public RenderFragment<TItem>? TableColumns { get; set; }

    /// <summary>
    /// 获得/设置 是否自动生成列信息 默认为 true
    /// </summary>
    [Parameter]
    public bool AutoGenerateColumns { get; set; } = true;

    [Parameter]
    public bool IsExcel { get; set; } = false;

    [Parameter]
    public bool IsTree { get; set; } = false;

    [NotNull]
    [Parameter]
    public Func<TItem, Task<IEnumerable<TableTreeNode<TItem>>>>? OnTreeExpand { get; set; }

    [NotNull]
    [Parameter]
    public Func<IEnumerable<TItem>, Task<IEnumerable<TableTreeNode<TItem>>>>? TreeNodeConverter { get; set; }

    [Parameter]
    public string? SortString { get; set; }

    /// <summary>
    /// 获得/设置 是否显示搜索框 默认为 false 不显示搜索框
    /// </summary>
    [Parameter]
    public bool ShowSearch { get; set; } = false;

    /// <summary>
    /// 获得/设置 是否显示按钮列(增加编辑删除) 默认为 true,本属性设置为 true 新建编辑删除按钮设置为 false 可单独控制每个按钮是否显示
    /// </summary>
    [Parameter]
    public bool ShowDefaultButtons { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否显示新建按钮 默认为 true 显示
    /// </summary>
    [Parameter]
    public bool ShowAddButton { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否显示编辑按钮 默认为 true 显示
    /// </summary>
    [Parameter]
    public bool ShowEditButton { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否显示删除按钮 默认为 true 显示
    /// </summary>
    [Parameter]
    public bool ShowDeleteButton { get; set; } = true;


    /// <summary>
    /// 获得/设置 是否显示扩展按钮 默认为 true
    /// </summary>
    [Parameter]
    public bool ShowExtendButtons { get; set; } = true;

    [Parameter]
    public bool ShowExtendEditButton { get; set; } = true;

    [Parameter]
    public bool ShowExtendDeleteButton { get; set; } = true;


    /// <summary>
    /// 获得/设置 是否显示高级搜索按钮 默认 true 显示 BootstrapBlazor.Components.Table`1.ShowSearch
    /// </summary>
    [Parameter]
    public bool ShowAdvancedSearch { get; set; } = false;

    /// <summary>
    /// 获得/设置 固定表头 默认 false
    /// </summary>
    [Parameter]
    public bool IsFixedHeader { get; set; } = false;

    /// <summary>
    ///  获得/设置 扩展按钮是否在前面 默认 true 在前面 
    /// </summary>
    [Parameter]
    public bool IsExtendButtonsInRowHeader { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否显示工具栏 默认 true 显示
    /// </summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否显示刷新按钮 默认为 true
    /// </summary>
    [Parameter]
    public bool ShowRefresh { get; set; } = true;


    /// <summary>
    /// 获得/设置 EditTemplate 实例
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<TItem>? EditTemplate { get; set; }

    /// <summary>
    /// 获得/设置 编辑框是否可以拖拽 默认 false 不可以拖拽
    /// </summary>
    [Parameter]
    public bool EditDialogIsDraggable { get; set; } = true;

    /// <summary>
    /// 获得/设置 RowButtonTemplate 实例 此模板生成的按钮默认放置到按钮后面如需放置前面 请查看 <see cref="BeforeRowButtonTemplate" />
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<TItem>? RowButtonTemplate { get; set; }

    /// <summary>
    /// 获得/设置 BeforeRowButtonTemplate 实例 此模板生成的按钮默认放置到按钮前面如需放置前面 请查看 <see cref="RowButtonTemplate" />
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<TItem>? BeforeRowButtonTemplate { get; set; }

    /// <summary>
    /// 获得/设置 自定义搜索模型模板 BootstrapBlazor.Components.Table`1.CustomerSearchModel
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<ITableSearchModel>? CustomerSearchTemplate { get; set; }

    /// <summary>
    /// 获得/设置 表格 Toolbar 按钮模板,表格工具栏左侧按钮模板，模板中内容出现在默认按钮后面
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment? TableToolbarTemplate { get; set; }

    /// <summary>
    /// 获得/设置 导出按钮下拉菜单模板 默认 null
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<ITableExportContext<TItem>>? ExportButtonDropdownTemplate { get; set; }

    /// <summary>
    /// 获得/设置 导出按钮文本
    /// </summary>
    [NotNull]
    [Parameter]
    public string? ExportButtonText { get; set; } = "导入导出";

    /// <summary>
    /// 获得/设置 行模板
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<TableRowContext<TItem>>? RowTemplate { get; set; }

    /// <summary>
    /// 获得/设置 行内容模板
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<TableRowContext<TItem>>? RowContentTemplate { get; set; }

    /// <summary>
    /// 获得/设置 明细行模板 
    /// </summary>
    [Parameter]
    public RenderFragment<TItem>? DetailRowTemplate { get; set; }

    /// <summary>
    /// 获得/设置 是否显示导出按钮 默认为 false 不显示
    /// </summary>
    [Parameter]
    public bool ShowExportButton { get; set; } = false;


    /// <summary>
    /// 获得/设置 是否显示行号列 默认为 true
    /// </summary>
    [Parameter]
    public bool ShowLineNo { get; set; } = true;

    /// <summary>
    ///  获得/设置 是否显示列选择下拉框 默认为 true 显示 点击下拉框内列控制是否显示后触发OnColumnVisibleChanged回调方法
    /// </summary>
    [Parameter]
    public bool ShowColumnList { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否显示视图按钮 默认为 false IsExcel 模式下此设置无效
    /// </summary>
    [Parameter]
    public bool ShowCardView { get; set; } = true;

    /// <summary>
    /// 是否为多选模式 默认为 true，此参数在 BootstrapBlazor.Components.Table`1.IsExcel 模式下为 true
    /// </summary>
    [Parameter]
    public bool IsMultipleSelect { get; set; } = true;

    /// <summary>
    /// 点击行即选中本行 默认为 false
    /// </summary>
    [Parameter]
    public bool ClickToSelect { get; set; } = false;

    [Parameter]
    public bool HeaderTextWrap { get; set; } = false;

    /// <summary>
    /// 获得/设置 新建数据弹窗 Title
    /// </summary>
    [Parameter]
    public string? AddModalTitle { get; set; } = "新建数据";

    /// <summary>
    /// 获得/设置 编辑数据弹窗 Title
    /// </summary>
    [Parameter]
    public string? EditModalTitle { get; set; } = "编辑数据";

    /// <summary>
    /// 获得/设置 新建按钮文本
    /// </summary>
    [Parameter]
    public string? AddButtonText { get; set; } = "新建";

    /// <summary>
    /// 获得/设置 编辑按钮文本
    /// </summary>
    [Parameter]
    public string? EditButtonText { get; set; } = "编辑";


    /// <summary>
    /// 是否开启操作日志
    /// </summary>
    [Parameter]
    public bool IsOpenOperationLog { get; set; } = true;

    /// <summary>
    ///  获得/设置 是否开启导航菜单验证 默认为 true, 此参数在为true时可以传入PageDirectory指定验证目录，PageDirectory没有传入则自动获取当前页面目录
    /// </summary>
    [Parameter]
    public bool IsOpenNavigationVerification { get; set; } = true;

    /// <summary>
    /// 获得/设置 数据操作权限(增加,编辑,删除,查询),当IsOpenNavigationVerification等于true开启导航菜单验证时，验证结果将覆盖此参数
    /// </summary>
    [Parameter]
    [NotNull]
    public DataOperationVerification? DataOperationVerification { get; set; } = new()
    {
        IsCanAdd = true,
        IsCanEdit = true,
        IsCanDelete = true,
        IsCanQuery = true
    };

    /// <summary>
    /// 获得/设置 页面目录
    /// </summary>
    [Parameter]
    public string? PageDirectory { get; set; } = "";


    [NotNull]
    [Parameter]
    public Func<QueryPageOptions, Task<QueryData<TItem>>>? OnQueryAsync { get; set; }

    [NotNull]
    [Parameter]
    public Func<TItem, ItemChangedType, Task<bool>>? OnSaveAsync { get; set; }

    [NotNull]
    [Parameter]
    public Func<IEnumerable<TItem>, Task<bool>>? OnDeleteAsync { get; set; }


    private bool IsAlreadyInitialize { get; set; } = false;


    /// <summary>
    /// OnInitialized 方法
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        Context ??= DbFactory.CreateDbContext();
        await base.OnInitializedAsync();


        SetRowClassFormatter ??= DefaultSetRowClassFormatter;

        OnQueryAsync ??= DefaultOnQueryAsync;
        OnSaveAsync ??= DefaultOnSaveAsync;
        OnDeleteAsync ??= DefaultOnDeleteAsync;
        OnColumnCreating ??= DefaultOnColumnCreating;

        if (IsDialog == true)
        {
            PageItemsSource = [10, 15, 20, 50];
        }



        // 导航菜单权限验证
        if (IsOpenNavigationVerification == true)
        {
            if (PageDirectory == null || PageDirectory == "")
            {
                // 当前页面目录
                var pageDirectory = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).LocalPath;

                PageDirectory = pageDirectory;
            }


            // 获取登录用户数据操作权限
            //DataOperationVerification ??= await NavVerificationService.GetUserDataOperationVerification(PageDirectory);

            // 是否显示 增加数据 按钮
            ShowAddButton = DataOperationVerification.IsCanAdd;

            // 是否显示 编辑数据 按钮
            ShowEditButton = DataOperationVerification.IsCanEdit;
            ShowExtendEditButton = DataOperationVerification.IsCanEdit;

            // 是否显示 删除数据 按钮
            ShowDeleteButton = DataOperationVerification.IsCanDelete;
            ShowExtendDeleteButton = DataOperationVerification.IsCanDelete;



        }


        IsAlreadyInitialize = true;
    }


    // private AppClientInfo AppClientInfo { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // AppClientInfo = await AppClientInfoService.GetAppClientInfo();
        }
    }

    public void Dispose()
    {
        Context.Dispose();
    }

    [NotNull]
    [Parameter]
    public Func<TItem, string>? SetRowClassFormatter { get; set; }

    private string DefaultSetRowClassFormatter(TItem item)
    {
        if (SelectedRows.Contains(item))
        {
            return "row-highlight-mediumpurple";
        }
        else
        {
            return "";
        }
    }


    /// <summary>
    /// 获得/设置 属性过滤参数
    /// </summary>
    [NotNull]
    [Parameter]
    public PropertyFilterParameters? PropertyFilterParameters { get; set; }


    /// <summary>
    /// 默认查询方法，支持普通表格和树形表格
    /// </summary>
    /// <param name="options">查询选项</param>
    /// <returns>查询数据</returns>
    public async Task<QueryData<TItem>> DefaultOnQueryAsync(QueryPageOptions options)
    {
        if (IsOpenNavigationVerification == true)
        {
            // 没有权限查询数据检查代码
        }

        ReSetContext();

        // 处理过滤与搜索逻辑
        var searches = options.ToFilter();

        // 创建 AutoMapper 配置
        //var config = new MapperConfiguration(cfg => cfg.CreateMap<PropertyFilterParameters, PropertyFilterParameters>());
        //var mapper = new Mapper(config);

        // 使用 AutoMapper 进行深拷贝
        //var _propertyFilterParameters = mapper.Map<PropertyFilterParameters>(PropertyFilterParameters);

        // 将 FilterKeyValueAction 转换为 PropertyFilterParameters
        //if (searches != null)
        //{
        //    if (_propertyFilterParameters == null)
        //    {
        //        _propertyFilterParameters = searches.ToPropertyFilterParameters();
        //    }
        //    else
        //    {
        //        _propertyFilterParameters.Add(searches.ToPropertyFilterParameters());
        //    }
        //}


        // 处理过滤与搜索逻辑
        var finalFilterParameters = BuildFilterParameters(options, PropertyFilterParameters);


        // 检查是否为树形表格，使用不同的数据加载策略
        if (IsTree)
        {
            // 树形表格查询逻辑
            return await GetTreeQueryData(options, finalFilterParameters);
        }
        else
        {
            // 原有的普通表格查询逻辑
            var query = Context.Set<TItem>()
                .AutoFilter(finalFilterParameters)
                .Sort(options.SortName!, options.SortOrder, !string.IsNullOrEmpty(options.SortName))
                .Count(out var count)
                .Page((options.PageIndex - 1) * options.PageItems, options.PageItems);

            var ret = new QueryData<TItem>()
            {
                TotalCount = count,
                Items = query,
                IsSorted = options.SortOrder != SortOrder.Unset,
                IsFiltered = options.Filters.Count != 0,
                IsAdvanceSearch = options.AdvanceSearches.Count != 0,
                IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
            };

            return ret;
        }

    }

    /// <summary>
    /// 构建过滤参数
    /// </summary>
    private static PropertyFilterParameters? BuildFilterParameters(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters)
    {
        PropertyFilterParameters? finalFilterParameters = null;

        var searches = options.ToFilter();
        if (searches != null)
        {
            var searchParameters = searches.ToPropertyFilterParameters();
            if (propertyFilterParameters == null)
            {
                finalFilterParameters = searchParameters;
            }
            else
            {
                // 避免修改原始参数，创建新实例
                finalFilterParameters = new PropertyFilterParameters();
                finalFilterParameters.Add(propertyFilterParameters);
                finalFilterParameters.Add(searchParameters);
            }
        }
        else
        {
            finalFilterParameters = propertyFilterParameters;
        }

        return finalFilterParameters;
    }

    /// <summary>
    /// 获取树形表格数据
    /// </summary>
    private async Task<QueryData<TItem>> GetTreeQueryData(QueryPageOptions options, PropertyFilterParameters? filterParams)
    {
        try
        {
            // 尝试查找ParentId属性 - 要求TItem有ParentId属性
            var parentIdProp = typeof(TItem).GetProperty("ParentId");
            if (parentIdProp == null)
            {
                throw new InvalidOperationException($"树形表格要求{typeof(TItem).Name}类有ParentId属性");
            }

            // 1. 首先查询所有根节点（ParentId为null的节点）
            IQueryable<TItem> rootQuery = Context.Set<TItem>().AsNoTracking();

            // 应用过滤条件
            if (filterParams != null)
            {
                rootQuery = rootQuery.AutoFilter(filterParams);
            }

            // 构建表达式树来查询ParentId为null的节点
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);
            var nullValue = Expression.Constant(null, parentIdProp.PropertyType);
            var equalExpression = Expression.Equal(property, nullValue);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            // 应用根节点筛选条件（ParentId为null）
            rootQuery = rootQuery.Where(lambda);

            // 应用排序
            if (!string.IsNullOrEmpty(options.SortName))
            {
                rootQuery = rootQuery.Sort(options.SortName, options.SortOrder);
            }

            // 获取根节点总数
            var totalCount = await rootQuery.CountAsync();

            // 分页查询根节点
            var pagedRoots = await rootQuery
                .Skip((options.PageIndex - 1) * options.PageItems)
                .Take(options.PageItems)
                .ToListAsync();

            // 2. 为每个根节点加载完整的子树
            var allItems = new List<TItem>();
            foreach (var root in pagedRoots)
            {
                // 添加根节点
                allItems.Add(root);

                // 递归加载子节点
                await LoadChildNodesAsync(root, allItems);
            }

            return new QueryData<TItem>
            {
                TotalCount = totalCount,  // 总数是根节点的数量
                Items = allItems,         // 返回包含所有子节点的集合
                IsSorted = options.SortOrder != SortOrder.Unset,
                IsFiltered = options.Filters.Count != 0,
                IsAdvanceSearch = options.AdvanceSearches.Count != 0,
                IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
            };
        }
        catch
        {
            var query = Context.Set<TItem>()
                .AutoFilter(filterParams)
                .Sort(options.SortName!, options.SortOrder, !string.IsNullOrEmpty(options.SortName))
                .Count(out var count)
                .Page((options.PageIndex - 1) * options.PageItems, options.PageItems);

            return new QueryData<TItem>()
            {
                TotalCount = count,
                Items = query,
                IsSorted = options.SortOrder != SortOrder.Unset,
                IsFiltered = options.Filters.Count != 0,
                IsAdvanceSearch = options.AdvanceSearches.Count != 0,
                IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
            };
        }
    }

    /// <summary>
    /// 递归加载子节点
    /// </summary>
    private async Task LoadChildNodesAsync(TItem parent, List<TItem> collector)
    {
        // 获取父节点ID
        var idProp = typeof(TItem).GetProperty("Id");
        var parentIdProp = typeof(TItem).GetProperty("ParentId");

        if (idProp == null || parentIdProp == null)
            return;

        var parentId = idProp.GetValue(parent)?.ToString();
        if (string.IsNullOrEmpty(parentId))
            return;

        // 查询子节点
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, parentIdProp);
        var value = Expression.Constant(parentId, parentIdProp.PropertyType);
        var equalExpression = Expression.Equal(property, value);
        var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

        var children = await Context.Set<TItem>()
            .AsNoTracking()
            .Where(lambda)
            .ToListAsync();

        // 添加子节点并继续递归
        foreach (var child in children)
        {
            collector.Add(child);
            await LoadChildNodesAsync(child, collector);
        }
    }





    /// <summary>
    /// 默认异步查询方法
    /// </summary>
    /// <param name="item"></param>
    /// <param name="changedType"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<bool> DefaultOnSaveAsync(TItem item, ItemChangedType changedType)
    {
        ReSetContext();

        try
        {
            // 获取实体类型元数据
            var entityType = Context.Model.FindEntityType(typeof(TItem)) ?? throw new InvalidOperationException($"实体类型 {typeof(TItem).Name} 不在模型中。");

            // 获取主键属性
            var primaryKey = entityType.FindPrimaryKey() ?? throw new InvalidOperationException($"实体类型 {typeof(TItem).Name} 没有定义主键。");

            // 判断是否为复合主键
            bool isCompositeKey = primaryKey.Properties.Count > 1;

            if (changedType == ItemChangedType.Add)
            {
                // 新增操作前先检查实体是否已存在
                bool entityExists = false;

                // 提取主键值
                var keyValues = primaryKey.Properties
                    .Select(p => typeof(TItem).GetProperty(p.Name)?.GetValue(item))
                    .ToArray();

                // 检查主键值是否有null
                if (!keyValues.Any(v => v == null))
                {
                    // 对于简单主键，直接使用 FindAsync
                    if (!isCompositeKey)
                    {
                        var existingEntity = await Context.Set<TItem>().FindAsync(keyValues);
                        entityExists = existingEntity != null;
                    }
                    else
                    {
                        // 对于复合主键，使用专用方法查找
                        var existingEntity = await FindEntityWithComplexKey(item, primaryKey);
                        entityExists = existingEntity != null;
                    }
                }

                // 如果实体已存在，直接返回成功
                if (entityExists)
                {
                    var op = new SwalOption()
                    {
                        Category = SwalCategory.Error,
                        Title = "数据已存在",
                        Content = "当前数据已存在，禁止再次加入！",
                    };
                    await SwalService.Show(op);

                    return false;
                }

                // 实体不存在，添加新实体
                await Context.Set<TItem>().AddAsync(item);
                int result = await Context.SaveChangesAsync();
                return result > 0;
            }
            else // ItemChangedType.Update
            {
                TItem? existingEntity = null;

                // 提取主键值
                var keyValues = primaryKey.Properties
                    .Select(p => typeof(TItem).GetProperty(p.Name)?.GetValue(item))
                    .ToArray();

                // 检查主键值是否有null
                if (!keyValues.Any(v => v == null))
                {
                    // 对于简单主键，直接使用 FindAsync
                    if (!isCompositeKey)
                    {
                        existingEntity = await Context.Set<TItem>().FindAsync(keyValues);
                    }
                    else
                    {
                        // 对于复合主键，使用专用方法查找
                        existingEntity = await FindEntityWithComplexKey(item, primaryKey);
                    }
                }

                if (existingEntity != null)
                {
                    Context.Entry(existingEntity).CurrentValues.SetValues(item);
                }

                int result = await Context.SaveChangesAsync();
                return result > 0;
            }

        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            var op = new SwalOption()
            {
                Category = SwalCategory.Error,
                Title = "数据异常",
                Content = $"保存数据时发生错误: 请检查数据合法性！",
            };
            await SwalService.Show(op);

            return false;
        }
    }

    /// <summary>
    /// 使用动态查询查找具有复杂主键的实体
    /// </summary>
    /// <param name="item">要查找的实体</param>
    /// <param name="primaryKey">主键定义</param>
    /// <returns>找到的实体或null</returns>
    private async Task<TItem?> FindEntityWithComplexKey(TItem item, IKey primaryKey)
    {
        try
        {
            // 提取主键值
            var keyValues = new List<object?>();
            var keyProps = primaryKey.Properties.Select(p => p.Name).ToArray();

            // 创建可以用于匹配的属性值字典
            var keyDict = new Dictionary<string, object?>();
            foreach (var propName in keyProps)
            {
                var prop = typeof(TItem).GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(item);
                    if (value != null)
                    {
                        keyDict.Add(propName, value);
                        keyValues.Add(value);
                    }
                    else
                    {
                        // 如果任何主键值为null，无法找到实体
                        return null;
                    }
                }
            }

            // 1.尝试使用传统的Find方法（支持复合主键）
            try
            {
                var entity = await Context.Set<TItem>().FindAsync([.. keyValues]);
                if (entity != null)
                    return entity;
            }
            catch
            {
                // 如果Find方法失败，继续使用LINQ查询
            }

            // 2. 如果Find方法无法工作，构建LINQ查询
            IQueryable<TItem> query = Context.Set<TItem>();
            foreach (var key in keyDict)
            {
                var propName = key.Key;
                var propValue = key.Value;

                // 使用动态属性查询
                query = query.Where(e => EF.Property<object>(e, propName)!.Equals(propValue));
            }

            return await query.FirstOrDefaultAsync();
        }
        catch
        {
            return null;
        }
    }





    /// <summary>
    /// 默认删除方法
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public async Task<bool> DefaultOnDeleteAsync_Old(IEnumerable<TItem> items)
    {
        if (IsOpenNavigationVerification == true)
        {
            // 没有权限删除数据
            // if (DataOperationVerification.IsCanDelete == false)
            // {
            //     return false;
            // }
        }

        ReSetContext();

        try
        {
            Context.Set<TItem>().RemoveRange(items);
            int result = await Context.SaveChangesAsync();

            if (result > 0 && IsOpenOperationLog)
            {
                // SystemOperationLog sensitiveOperationLog = new(AppClientInfo)
                //     {
                //         UserId = await AuthenticationStateService.GetUserId(),
                //         RoleId = AuthenticationStateService.NowRolePlus?.Id ?? "",
                //         Operation = typeof(TItem).Name + "删除数据",
                //         Info = JsonConvert.SerializeObject(items)
                //     };

                // Context.SensitiveOperationLogs.Add(sensitiveOperationLog);
                // await Context.SaveChangesAsync();
            }

            return result > 0;
        }
        catch
        {
            var op = new SwalOption()
            {
                Category = SwalCategory.Error,
                Title = "数据失效",
                Content = "请选择 刷新页面 或者 刷新数据 后重新尝试",
                CancelButtonText = "刷新数据",
                //CancelButtonIcon = "fas fa-arrow-rotate-right",
                ConfirmButtonText = "刷新页面",
                ConfirmButtonIcon = "fas fa-rotate",
                //ConfirmButtonColor = "",
                //CancelButtonColor = "",
            };
            var ret = await SwalService.ShowModal(op);

            if (ret == true)
            {
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            else
            {
                Context = DbFactory.CreateDbContext();
                await TableRef.QueryAsync();
            }

            return false;
        }

    }

    /// <summary>
    /// 默认删除方法，支持普通表格和树形表格
    /// </summary>
    /// <param name="items">要删除的项目</param>
    /// <returns>删除是否成功</returns>
    public async Task<bool> DefaultOnDeleteAsync(IEnumerable<TItem> items)
    {
        if (IsOpenNavigationVerification == true)
        {
            // 没有权限删除数据
            // if (DataOperationVerification.IsCanDelete == false)
            // {
            //     return false;
            // }
        }

        ReSetContext();

        try
        {
            if (IsTree)
            {
                // 树形表格删除逻辑
                List<TItem> allItemsToDelete = new();

                // 首先收集选中的项目及其所有子项
                foreach (var item in items)
                {
                    // 避免重复添加
                    if (!allItemsToDelete.Contains(item))
                    {
                        allItemsToDelete.Add(item);
                    }

                    // 递归收集所有子项
                    await CollectChildNodesForDelete(item, allItemsToDelete);
                }

                // 执行批量删除
                Context.Set<TItem>().RemoveRange(allItemsToDelete);
                int result = await Context.SaveChangesAsync();

                // 日志记录
                if (result > 0 && IsOpenOperationLog)
                {
                    // SystemOperationLog sensitiveOperationLog = new(AppClientInfo)
                    // {
                    //     UserId = await AuthenticationStateService.GetUserId(),
                    //     RoleId = AuthenticationStateService.NowRolePlus?.Id ?? "",
                    //     Operation = typeof(TItem).Name + "删除数据(含子项)",
                    //     Info = JsonConvert.SerializeObject(allItemsToDelete)
                    // };
                    // Context.SensitiveOperationLogs.Add(sensitiveOperationLog);
                    // await Context.SaveChangesAsync();
                }

                return result > 0;
            }
            else
            {
                // 原有的普通表格删除逻辑
                Context.Set<TItem>().RemoveRange(items);
                int result = await Context.SaveChangesAsync();

                if (result > 0 && IsOpenOperationLog)
                {
                    // SystemOperationLog sensitiveOperationLog = new(AppClientInfo)
                    // {
                    //     UserId = await AuthenticationStateService.GetUserId(),
                    //     RoleId = AuthenticationStateService.NowRolePlus?.Id ?? "",
                    //     Operation = typeof(TItem).Name + "删除数据",
                    //     Info = JsonConvert.SerializeObject(items)
                    // };
                    // Context.SensitiveOperationLogs.Add(sensitiveOperationLog);
                    // await Context.SaveChangesAsync();
                }

                return result > 0;
            }
        }
        catch (Exception ex)
        {
            var op = new SwalOption()
            {
                Category = SwalCategory.Error,
                Title = "数据失效",
                Content = $"删除失败: {ex.Message}\n请选择 刷新页面 或者 刷新数据 后重新尝试",
                CancelButtonText = "刷新数据",
                ConfirmButtonText = "刷新页面",
                ConfirmButtonIcon = "fas fa-rotate",
            };
            var ret = await SwalService.ShowModal(op);

            if (ret == true)
            {
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            else
            {
                Context = DbFactory.CreateDbContext();
                await TableRef.QueryAsync();
            }

            return false;
        }
    }

    /// <summary>
    /// 递归收集要删除的子节点
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="collector">收集器集合</param>
    private async Task CollectChildNodesForDelete(TItem parent, List<TItem> collector)
    {
        try
        {
            // 获取父节点ID和ParentId属性
            var idProp = typeof(TItem).GetProperty("Id");
            var parentIdProp = typeof(TItem).GetProperty("ParentId");

            if (idProp == null || parentIdProp == null)
                return;

            var parentId = idProp.GetValue(parent)?.ToString();
            if (string.IsNullOrEmpty(parentId))
                return;

            // 构建查询表达式以查找所有子节点
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);
            var value = Expression.Constant(parentId, parentIdProp.PropertyType);
            var equalExpression = Expression.Equal(property, value);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            // 查询所有直接子节点
            var children = await Context.Set<TItem>()
                .AsNoTracking()
                .Where(lambda)
                .ToListAsync();

            // 添加子节点并递归其子节点
            foreach (var child in children)
            {
                // 避免重复添加
                if (!collector.Any(x => idProp.GetValue(x)?.ToString() == idProp.GetValue(child)?.ToString()))
                {
                    collector.Add(child);

                    // 递归处理子节点的子节点
                    await CollectChildNodesForDelete(child, collector);
                }
            }
        }
        catch (Exception ex)
        {
            // 记录错误但继续进行，避免中断整个删除过程
            //Console.WriteLine($"收集子节点时发生错误: {ex.Message}");
        }
    }




    /// <summary>
    /// 列创建时回调委托方法
    /// </summary>
    [NotNull]
    [Parameter]
    public Func<List<ITableColumn>, Task>? OnColumnCreating { get; set; }

    /// <summary>
    /// 默认列创建时回调委托方法
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    private async Task DefaultOnColumnCreating(List<ITableColumn> columns)
    {
        //var column = columns[2];
        ////获取绑定字段信息方法
        //var Test2 = column.GetFieldName();
        ////获取绑定字段显示名称方法
        //var Test4 = column.GetDisplayName();

        //if (IsOpenNavigationVerification == true)
        //{
        //    List<ITableColumn> removeColumns = [];

        //    foreach (var column in columns)
        //    {
        //        var columnName = column.GetFieldName();
        //        // var IsShow = await NavVerificationService.IsUserJsonPermission(PageDirectory ?? "", columnName);
        //        // if (IsShow == false)
        //        // {
        //        //     removeColumns.Add(column);
        //        // }
        //    }

        //    columns.RemoveAll(column => removeColumns.Contains(column));
        //}

        await Task.CompletedTask;
    }



    /// <summary>
    /// 获得/设置 被选中数据集合
    /// </summary>
    [Parameter]
    public List<TItem> SelectedRows { get; set; } = [];

    /// <summary>
    /// 清除被选中数据集合
    /// </summary>
    /// <returns></returns>
    public bool CleanSelectedRows()
    {
        SelectedRows.Clear();

        if (SelectedRows.Count <= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// 修改被选中数据集合
    /// </summary>
    /// <param name="SelectedRows"></param>
    /// <returns></returns>
    public void SetSelectedRows(List<TItem>? newSelectedRows = null)
    {
        if (newSelectedRows == null)
        {
            SelectedRows = [];
        }
        else
        {
            SelectedRows = newSelectedRows;
        }
    }












}
