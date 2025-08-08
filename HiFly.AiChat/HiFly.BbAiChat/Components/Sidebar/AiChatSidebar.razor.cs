// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HiFly.BbAiChat.Components.Sidebar;

/// <summary>
/// 左侧边栏组件 - 重构版本
/// </summary>
public partial class AiChatSidebar : ComponentBase
{
    /// <summary>
    /// 是否折叠
    /// </summary>
    [Parameter]
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// 面板宽度（支持px、%、rem等单位）
    /// </summary>
    [Parameter]
    public string Width { get; set; } = "320px";

    /// <summary>
    /// 最小宽度
    /// </summary>
    [Parameter]
    public int MinWidth { get; set; } = 200;

    /// <summary>
    /// 最大宽度
    /// </summary>
    [Parameter]
    public int MaxWidth { get; set; } = 500;

    /// <summary>
    /// 折叠时的宽度
    /// </summary>
    [Parameter]
    public string CollapsedWidth { get; set; } = "60px";

    /// <summary>
    /// 是否启用宽度调整
    /// </summary>
    [Parameter]
    public bool EnableWidthResize { get; set; } = true;

    /// <summary>
    /// 是否显示宽度调整手柄
    /// </summary>
    [Parameter]
    public bool ShowResizeHandle { get; set; } = true;

    /// <summary>
    /// 预设宽度选项
    /// </summary>
    [Parameter]
    public List<SidebarWidthPreset> WidthPresets { get; set; } = new()
    {
        new("紧凑", "280px", "fas fa-compress"),
        new("标准", "320px", "fas fa-align-justify"),
        new("宽松", "380px", "fas fa-expand-arrows-alt"),
        new("超宽", "450px", "fas fa-arrows-alt-h")
    };

    /// <summary>
    /// 宽度变化事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnWidthChanged { get; set; }

    /// <summary>
    /// 会话列表
    /// </summary>
    [Parameter]
    public List<ChatSession> Sessions { get; set; } = new();

    /// <summary>
    /// 当前会话ID
    /// </summary>
    [Parameter]
    public string CurrentSessionId { get; set; } = string.Empty;

    /// <summary>
    /// 消息数量
    /// </summary>
    [Parameter]
    public int MessageCount { get; set; }

    /// <summary>
    /// 自定义左侧面板模板
    /// </summary>
    [Parameter]
    public RenderFragment? SidebarTemplate { get; set; }

    /// <summary>
    /// 新建对话事件
    /// </summary>
    [Parameter]
    public EventCallback OnNewChat { get; set; }

    /// <summary>
    /// 折叠状态变化事件
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnCollapsedChanged { get; set; }

    /// <summary>
    /// 选择会话事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSessionSelected { get; set; }

    /// <summary>
    /// 编辑会话事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnEditSession { get; set; }

    /// <summary>
    /// 删除会话事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDeleteSession { get; set; }

    /// <summary>
    /// JavaScript 运行时
    /// </summary>
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    private int _maxVisibleSessions = 15;
    private bool _isInitialized = false;
    private string _actualWidth = "320px";
    private ElementReference _sidebarElement;
    private SidebarResizeHandle? _resizeHandleComponent;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_isInitialized)
        {
            await InitializeMaxVisibleSessions();
            await InitializeWidthResize();
            _isInitialized = true;
        }

        // 每次渲染后更新折叠状态的计算
        if (_isInitialized && IsCollapsed)
        {
            await UpdateMaxVisibleSessions();
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _actualWidth = IsCollapsed ? CollapsedWidth : Width;
    }

    #region 子组件事件处理

    private async Task HandleNewChat()
    {
        if (OnNewChat.HasDelegate)
        {
            await OnNewChat.InvokeAsync();
        }
    }

    private async Task HandleToggleCollapse()
    {
        IsCollapsed = !IsCollapsed;
        _actualWidth = IsCollapsed ? CollapsedWidth : Width;

        // 切换状态时立即更新最大显示数量
        if (IsCollapsed)
        {
            try
            {
                await Task.Delay(50);
                await JSRuntime.InvokeVoidAsync("eval",
                    $"document.querySelector('.ai-chat-left-panel')?.setAttribute('data-session-count', '{Sessions.Count}')");
                _maxVisibleSessions = await JSRuntime.InvokeAsync<int>("aiChatHelper.getResponsiveMaxSessions");
            }
            catch (Exception)
            {
                _maxVisibleSessions = 8;
            }
        }
        else
        {
            _maxVisibleSessions = int.MaxValue;
        }

        if (OnCollapsedChanged.HasDelegate)
        {
            await OnCollapsedChanged.InvokeAsync(IsCollapsed);
        }

        StateHasChanged();

        if (_isInitialized)
        {
            await Task.Delay(150);
            await UpdateMaxVisibleSessions();
        }
    }

    private async Task HandleWidthPresetSelected(SidebarWidthPreset preset)
    {
        // 立即更新本地状态
        Width = preset.Width;
        _actualWidth = IsCollapsed ? CollapsedWidth : Width;

        // 通知父组件宽度变化
        if (OnWidthChanged.HasDelegate)
        {
            await OnWidthChanged.InvokeAsync(Width);
        }

        // 强制重新渲染
        StateHasChanged();
    }

    private async Task HandleSessionSelected(string sessionId)
    {
        if (OnSessionSelected.HasDelegate)
        {
            await OnSessionSelected.InvokeAsync(sessionId);
        }
    }

    private async Task HandleEditSession(string sessionId)
    {
        if (OnEditSession.HasDelegate)
        {
            await OnEditSession.InvokeAsync(sessionId);
        }
    }

    private async Task HandleDeleteSession(string sessionId)
    {
        if (OnDeleteSession.HasDelegate)
        {
            await OnDeleteSession.InvokeAsync(sessionId);
        }
    }

    private async Task HandleResizeWidthChanged(int newWidth)
    {
        var newWidthStr = $"{newWidth}px";
        if (Width != newWidthStr)
        {
            await SetWidth(newWidthStr);
        }
    }

    #endregion

    #region 宽度管理方法

    /// <summary>
    /// 设置预设宽度
    /// </summary>
    /// <param name="width">新宽度</param>
    public async Task SetWidth(string width)
    {
        Width = width;
        _actualWidth = IsCollapsed ? CollapsedWidth : Width;

        if (OnWidthChanged.HasDelegate)
        {
            await OnWidthChanged.InvokeAsync(Width);
        }

        StateHasChanged();
    }

    #endregion

    #region 初始化和响应式管理

    private async Task InitializeMaxVisibleSessions()
    {
        try
        {
            await Task.Delay(50);

            await JSRuntime.InvokeVoidAsync("eval",
                $"document.querySelector('.ai-chat-left-panel')?.setAttribute('data-session-count', '{Sessions.Count}')");

            if (IsCollapsed)
            {
                _maxVisibleSessions = await JSRuntime.InvokeAsync<int>("aiChatHelper.getResponsiveMaxSessions");
            }
            else
            {
                _maxVisibleSessions = int.MaxValue;
            }

            await JSRuntime.InvokeVoidAsync("aiChatHelper.onViewportChange",
                DotNetObjectReference.Create(this));

            StateHasChanged();
        }
        catch (Exception)
        {
            _maxVisibleSessions = IsCollapsed ? 8 : int.MaxValue;
        }
    }

    /// <summary>
    /// 初始化宽度调整功能
    /// </summary>
    private async Task InitializeWidthResize()
    {
        if (!EnableWidthResize || _resizeHandleComponent == null) return;

        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.initializeSidebarResize",
                _sidebarElement, MinWidth, MaxWidth, _resizeHandleComponent.GetDotNetReference());
        }
        catch (Exception)
        {
            // 静默处理错误
        }
    }

    /// <summary>
    /// JavaScript 回调方法：视口变化时更新最大可见会话数
    /// </summary>
    [JSInvokable]
    public async Task UpdateMaxVisibleSessions()
    {
        try
        {
            if (IsCollapsed)
            {
                var newMaxSessions = await JSRuntime.InvokeAsync<int>("aiChatHelper.getResponsiveMaxSessions");

                if (newMaxSessions != _maxVisibleSessions)
                {
                    _maxVisibleSessions = newMaxSessions;
                    await InvokeAsync(StateHasChanged);
                }
            }
            else
            {
                if (_maxVisibleSessions != int.MaxValue)
                {
                    _maxVisibleSessions = int.MaxValue;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch (Exception)
        {
            // 静默处理错误
        }
    }

    #endregion

    #region 辅助方法

    private string GetTokensUsed()
    {
        var estimatedTokens = MessageCount * 50;

        if (estimatedTokens < 1000) return estimatedTokens.ToString();
        if (estimatedTokens < 1000000) return $"{estimatedTokens / 1000.0:F1}K";
        return $"{estimatedTokens / 1000000.0:F1}M";
    }

    private string GetSessionDuration()
    {
        var currentSession = Sessions.FirstOrDefault(s => s.Id == CurrentSessionId);
        if (currentSession == null) return "0分钟";

        var duration = DateTime.Now - currentSession.CreateTime;
        if (duration.TotalMinutes < 1) return "刚刚";
        if (duration.TotalHours < 1) return $"{(int)duration.TotalMinutes}分钟";
        if (duration.TotalDays < 1) return $"{(int)duration.TotalHours}小时";
        return $"{(int)duration.TotalDays}天";
    }

    #endregion
}

/// <summary>
/// 侧边栏宽度预设选项
/// </summary>
/// <param name="Name">显示名称</param>
/// <param name="Width">宽度值</param>
/// <param name="Icon">图标CSS类</param>
public record SidebarWidthPreset(string Name, string Width, string Icon);