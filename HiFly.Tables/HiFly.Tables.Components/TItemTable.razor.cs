// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using AutoMapper;
using BootstrapBlazor.Components;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.Tables.Components;

/// <summary>
/// 泛型表格组件 - 重构后不依赖 EF Core
/// </summary>
/// <typeparam name="TItem">实体类型</typeparam>
[CascadingTypeParameter(nameof(TItem))]
public partial class TItemTable<TItem> : ComponentBase, ITableComponent<TItem>, IDisposable
    where TItem : class, new()
{
    #region 注入的服务

    [Inject]
    [NotNull]
    protected IHiFlyDataService<TItem> DataService { get; set; } = default!;

    [Inject]
    [NotNull]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    [NotNull]
    protected SwalService SwalService { get; set; } = default!;

    #endregion

    #region 参数属性

    /// <summary>
    /// 顶部 Div 的 CSS 类
    /// </summary>
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
    /// <param name="visible">是否显示</param>
    /// <returns></returns>
    public ValueTask ToggleLoading(bool visible) => TableRef.ToggleLoading(visible);

    /// <summary>
    /// 查询按钮调用此方法
    /// </summary>
    /// <returns></returns>
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
    /// 获得/设置 每页显示数据数量的外部数据源
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
    /// 获得/设置 是否显示按钮列(增加编辑删除) 默认为 true
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
    /// 获得/设置 是否显示高级搜索按钮 默认 true 显示
    /// </summary>
    [Parameter]
    public bool ShowAdvancedSearch { get; set; } = false;

    /// <summary>
    /// 获得/设置 固定表头 默认 false
    /// </summary>
    [Parameter]
    public bool IsFixedHeader { get; set; } = false;

    /// <summary>
    /// 获得/设置 扩展按钮是否在前面 默认 true 在前面
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
    /// 获得/设置 RowButtonTemplate 实例
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<TItem>? RowButtonTemplate { get; set; }

    /// <summary>
    /// 获得/设置 BeforeRowButtonTemplate 实例
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<TItem>? BeforeRowButtonTemplate { get; set; }

    /// <summary>
    /// 获得/设置 自定义搜索模型模板
    /// </summary>
    [NotNull]
    [Parameter]
    public RenderFragment<ITableSearchModel>? CustomerSearchTemplate { get; set; }

    /// <summary>
    /// 获得/设置 表格 Toolbar 按钮模板
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
    /// 获得/设置 是否显示列选择下拉框 默认为 true 显示
    /// </summary>
    [Parameter]
    public bool ShowColumnList { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否显示视图按钮 默认为 false
    /// </summary>
    [Parameter]
    public bool ShowCardView { get; set; } = true;

    /// <summary>
    /// 是否为多选模式 默认为 true
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
    /// 获得/设置 是否开启导航菜单验证 默认为 true
    /// </summary>
    [Parameter]
    public bool IsOpenNavigationVerification { get; set; } = true;

    /// <summary>
    /// 获得/设置 数据操作权限(增加,编辑,删除,查询)
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

    /// <summary>
    /// 获得/设置 属性过滤参数
    /// </summary>
    [NotNull]
    [Parameter]
    public PropertyFilterParameters? PropertyFilterParameters { get; set; }

    /// <summary>
    /// 获得/设置 被选中数据集合
    /// </summary>
    [Parameter]
    public List<TItem> SelectedRows { get; set; } = [];

    #endregion

    #region 回调方法参数

    [NotNull]
    [Parameter]
    public Func<QueryPageOptions, Task<QueryData<TItem>>>? OnQueryAsync { get; set; }

    [NotNull]
    [Parameter]
    public Func<TItem, ItemChangedType, Task<bool>>? OnSaveAsync { get; set; }

    [NotNull]
    [Parameter]
    public Func<IEnumerable<TItem>, Task<bool>>? OnDeleteAsync { get; set; }

    [NotNull]
    [Parameter]
    public Func<TItem, string>? SetRowClassFormatter { get; set; }

    [NotNull]
    [Parameter]
    public Func<List<ITableColumn>, Task>? OnColumnCreating { get; set; }

    #endregion

    #region 生命周期方法

    /// <summary>
    /// OnInitialized 方法
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        SetRowClassFormatter ??= DefaultSetRowClassFormatter;
        OnQueryAsync ??= DefaultOnQueryAsync;
        OnSaveAsync ??= DefaultOnSaveAsync;
        OnDeleteAsync ??= DefaultOnDeleteAsync;
        OnColumnCreating ??= DefaultOnColumnCreating;

        if (IsDialog)
        {
            PageItemsSource = [10, 15, 20, 50];
        }

        // 导航菜单权限验证
        if (IsOpenNavigationVerification)
        {
            if (string.IsNullOrEmpty(PageDirectory))
            {
                // 当前页面目录
                var pageDirectory = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).LocalPath;
                PageDirectory = pageDirectory;
            }

            // 是否显示 增加数据 按钮
            ShowAddButton = DataOperationVerification.IsCanAdd;

            // 是否显示 编辑数据 按钮
            ShowEditButton = DataOperationVerification.IsCanEdit;
            ShowExtendEditButton = DataOperationVerification.IsCanEdit;

            // 是否显示 删除数据 按钮
            ShowDeleteButton = DataOperationVerification.IsCanDelete;
            ShowExtendDeleteButton = DataOperationVerification.IsCanDelete;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // 可以在这里添加首次渲染后的逻辑
        }
    }

    #endregion

    #region 默认实现方法

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
    /// 默认查询方法
    /// </summary>
    public async Task<QueryData<TItem>> DefaultOnQueryAsync(QueryPageOptions options)
    {
        if (IsOpenNavigationVerification && !DataOperationVerification.IsCanQuery)
        {
            return new QueryData<TItem> { TotalCount = 0, Items = [] };
        }

        try
        {
            return await DataService.OnQueryAsync(options, PropertyFilterParameters, IsTree);
        }
        catch (Exception)
        {
            return new QueryData<TItem> { TotalCount = 0, Items = [] };
        }
    }

    /// <summary>
    /// 默认保存方法
    /// </summary>
    public async Task<bool> DefaultOnSaveAsync(TItem item, ItemChangedType changedType)
    {
        try
        {
            return await DataService.OnSaveAsync(item, changedType);
        }
        catch (Exception)
        {
            var op = new SwalOption()
            {
                Category = SwalCategory.Error,
                Title = "数据异常",
                Content = "保存数据时发生错误: 请检查数据合法性！",
            };
            await SwalService.Show(op);

            return false;
        }
    }

    /// <summary>
    /// 默认删除方法
    /// </summary>
    public async Task<bool> DefaultOnDeleteAsync(IEnumerable<TItem> items)
    {
        if (IsOpenNavigationVerification && !DataOperationVerification.IsCanDelete)
        {
            return false;
        }

        try
        {
            return await DataService.OnDeleteAsync(items);
        }
        catch (Exception)
        {
            var op = new SwalOption()
            {
                Category = SwalCategory.Error,
                Title = "数据失效",
                Content = "请选择 刷新页面 或者 刷新数据 后重新尝试",
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
                await TableRef.QueryAsync();
            }

            return false;
        }
    }

    /// <summary>
    /// 默认列创建时回调委托方法
    /// </summary>
    private async Task DefaultOnColumnCreating(List<ITableColumn> columns)
    {
        await Task.CompletedTask;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 清除被选中数据集合
    /// </summary>
    /// <returns></returns>
    public bool CleanSelectedRows()
    {
        SelectedRows.Clear();
        return SelectedRows.Count <= 0;
    }

    /// <summary>
    /// 修改被选中数据集合
    /// </summary>
    /// <param name="newSelectedRows"></param>
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

    #endregion

    #region 资源释放

    public void Dispose()
    {
        DataService?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}
