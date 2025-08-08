// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.BbAiChat.Components;

/// <summary>
/// AI聊天主组件 - 重构版本
/// </summary>
public partial class AiChatComponentV2 : ComponentBase, IDisposable
{
    #region 依赖注入
    [Inject]
    [NotNull]
    private IJSRuntime? JSRuntime { get; set; }
    #endregion

    #region 基本参数
    /// <summary>
    /// 聊天标题
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "AI 智能对话";

    /// <summary>
    /// 输入框占位符
    /// </summary>
    [Parameter]
    public string InputPlaceholder { get; set; } = "请输入您的问题...";

    /// <summary>
    /// 是否显示左侧面板
    /// </summary>
    [Parameter]
    public bool ShowLeftPanel { get; set; } = true;

    /// <summary>
    /// 是否显示右侧面板 - 修改默认值为false，避免异常开启
    /// </summary>
    [Parameter]
    public bool ShowRightPanel { get; set; } = false;

    /// <summary>
    /// 左侧面板是否折叠
    /// </summary>
    [Parameter]
    public bool IsLeftPanelCollapsed { get; set; } = false;

    /// <summary>
    /// 左侧面板宽度
    /// </summary>
    [Parameter]
    public string LeftPanelWidth { get; set; } = "320px";

    /// <summary>
    /// 右侧面板宽度
    /// </summary>
    [Parameter]
    public string RightPanelWidth { get; set; } = "320px";
    #endregion

    #region 模板参数
    /// <summary>
    /// 顶部面板模板
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// 左侧面板模板
    /// </summary>
    [Parameter]
    public RenderFragment? SidebarTemplate { get; set; }

    /// <summary>
    /// 右侧面板模板
    /// </summary>
    [Parameter]
    public RenderFragment? SettingsTemplate { get; set; }

    /// <summary>
    /// 工具栏模板
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }
    #endregion

    #region 事件参数
    /// <summary>
    /// 发送消息回调
    /// </summary>
    [Parameter]
    public EventCallback<string> OnMessageSent { get; set; }

    /// <summary>
    /// 清空对话回调
    /// </summary>
    [Parameter]
    public EventCallback OnChatCleared { get; set; }

    /// <summary>
    /// 新建对话回调
    /// </summary>
    [Parameter]
    public EventCallback OnNewChatCreated { get; set; }

    /// <summary>
    /// 选择会话回调
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSessionSelected { get; set; }

    /// <summary>
    /// 模型变更回调
    /// </summary>
    [Parameter]
    public EventCallback<string> OnModelChanged { get; set; }

    /// <summary>
    /// 温度变更回调
    /// </summary>
    [Parameter]
    public EventCallback<double> OnTemperatureChanged { get; set; }

    /// <summary>
    /// 左侧面板折叠状态变更回调
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnLeftPanelCollapsedChanged { get; set; }

    /// <summary>
    /// 右侧面板显示状态变更回调
    /// </summary]
    [Parameter]
    public EventCallback<bool> OnRightPanelVisibilityChanged { get; set; }

    /// <summary>
    /// 删除会话回调
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSessionDeleted { get; set; }
    #endregion

    #region 私有字段和属性
    private string CurrentMessage { get; set; } = string.Empty;
    private bool IsLoading { get; set; } = false;
    private bool IsVoiceRecording { get; set; } = false;
    
    // 添加一个内部状态追踪，防止状态意外重置
    private bool _hasUserToggledPanel = false;
    private bool _initialPanelState = false;

    /// <summary>
    /// 确保CurrentMessage始终为空字符串（除非有实际内容）
    /// </summary>
    private void EnsureCurrentMessageIsEmpty()
    {
        if (CurrentMessage == "CurrentMessage" || string.IsNullOrWhiteSpace(CurrentMessage))
        {
            CurrentMessage = string.Empty;
        }
    }

    /// <summary>
    /// 当前会话ID
    /// </summary>
    public string CurrentSessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 消息列表
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// 聊天会话列表
    /// </summary>
    public List<ChatSession> ChatSessions { get; set; } = new();

    /// <summary>
    /// 可用模型列表
    /// </summary>
    public List<AiModel> AvailableModels { get; set; } = new()
    {
        new AiModel { Id = "gpt-3.5-turbo", Name = "GPT-3.5 Turbo" },
        new AiModel { Id = "gpt-4", Name = "GPT-4" },
        new AiModel { Id = "gpt-4-turbo", Name = "GPT-4 Turbo" }
    };

    /// <summary>
    /// 选中的模型
    /// </summary>
    public string SelectedModel { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// 温度设置
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// 最大令牌数
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// 是否启用上下文记忆
    /// </summary>
    public bool EnableMemory { get; set; } = true;

    /// <summary>
    /// 是否启用流式响应
    /// </summary>
    public bool EnableStreaming { get; set; } = true;
    #endregion

    #region 生命周期方法
    protected override async Task OnInitializedAsync()
    {
        // 记录初始面板状态（不调用JavaScript）
        _initialPanelState = ShowRightPanel;
        
        // 确保CurrentMessage为空
        EnsureCurrentMessageIsEmpty();

        // 初始化默认会话
        if (!ChatSessions.Any())
        {
            var defaultSession = new ChatSession
            {
                Id = CurrentSessionId,
                Title = "新对话",
                CreateTime = DateTime.Now
            };
            ChatSessions.Add(defaultSession);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 首次渲染后，尝试从localStorage恢复面板状态
            try
            {
                // 先恢复面板状态
                await RestorePanelStateFromStorage();
                
                // 然后初始化主题
                await JSRuntime.InvokeVoidAsync("aiChatHelper.initTheme");
            }
            catch (JSException)
            {
                // 提供降级方案
                try
                {
                    await JSRuntime.InvokeVoidAsync("eval", @"
                        const savedTheme = localStorage.getItem('theme');
                        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
                        
                        if (savedTheme === 'dark' || (!savedTheme && prefersDark)) {
                            document.body.classList.add('dark-theme');
                        } else {
                            document.body.classList.remove('dark-theme');
                        }
                    ");
                }
                catch (JSException)
                {
                    // 完全降级，不做任何操作
                }
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    /// 从localStorage恢复面板状态 - 增强版本，包含重试机制
    /// </summary>
    private async Task RestorePanelStateFromStorage()
    {
        const int maxRetries = 3;
        const int delayMs = 100;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var savedPanelState = await JSRuntime.InvokeAsync<bool>("aiChatHelper.storage.getRightPanelState");
                
                // 只有在用户未手动切换过面板状态且保存的状态与当前状态不同时才更新
                if (!_hasUserToggledPanel && savedPanelState != ShowRightPanel)
                {
                    ShowRightPanel = savedPanelState;
                    StateHasChanged(); // 触发重新渲染以更新UI
                }
                return; // 成功，退出重试循环
            }
            catch (JSException)
            {
                if (attempt < maxRetries)
                {
                    await Task.Delay(delayMs);
                }
                else
                {
                    // 最后一次尝试失败，使用降级方案
                    try
                    {
                        // 尝试使用基础的localStorage访问
                        var result = await JSRuntime.InvokeAsync<string>("eval", 
                            "localStorage.getItem('aiChat_rightPanelOpen') || 'false'");

                        if (bool.TryParse(result, out bool fallbackState) && 
                            !_hasUserToggledPanel && fallbackState != ShowRightPanel)
                        {
                            ShowRightPanel = fallbackState;
                            StateHasChanged();
                        }
                    }
                    catch (Exception)
                    {
                        // 完全降级，使用默认状态
                    }
                }
            }
        }
    }

    protected override void OnParametersSet()
    {
        // 增强参数设置逻辑，防止外部参数意外覆盖用户操作
        
        // 确保CurrentMessage为空（防止测试数据残留）
        EnsureCurrentMessageIsEmpty();
        
        // 只有在首次设置或者用户未手动切换过面板时，才允许外部参数影响面板状态
        if (!_hasUserToggledPanel)
        {
            // 如果这是首次参数设置，记录初始状态
            if (_initialPanelState == false && ShowRightPanel == true)
            {
                _initialPanelState = true;
            }
        }
        
        base.OnParametersSet();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 添加消息
    /// </summary>
    /// <param name="content">消息内容</param>
    /// <param name="isUser">是否为用户消息</param>
    public async Task AddMessage(string content, bool isUser = false)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = content,
            IsUser = isUser,
            Timestamp = DateTime.Now,
            SessionId = CurrentSessionId
        };

        Messages.Add(message);
        StateHasChanged();
        await Task.Delay(100);
    }

    /// <summary>
    /// 设置加载状态
    /// </summary>
    /// <param name="loading">是否加载中</param>
    public void SetLoading(bool loading)
    {
        IsLoading = loading;
        StateHasChanged();
    }

    /// <summary>
    /// 清空消息
    /// </summary>
    public async Task ClearMessages()
    {
        Messages.Clear();
        StateHasChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取当前消息列表
    /// </summary>
    /// <returns></returns>
    public List<ChatMessage> GetMessages() => Messages.ToList();

    /// <summary>
    /// 设置消息列表
    /// </summary>
    /// <param name="messages"></param>
    public async Task SetMessages(List<ChatMessage> messages)
    {
        Messages = messages ?? new List<ChatMessage>();
        StateHasChanged();
        await Task.Delay(100);
    }

    /// <summary>
    /// 调试模式：启用详细日志
    /// </summary>
    [Parameter]
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// 输出调试信息
    /// </summary>
    private void LogDebugInfo(string message)
    {
        if (EnableDebugLogging)
        {
            System.Console.WriteLine($"[AiChatPanel] {DateTime.Now:HH:mm:ss.fff} - {message}");
        }
    }

    /// <summary>
    /// 切换右侧面板显示状态 - 增强版本，包含状态持久化和错误处理
    /// </summary>
    public async Task ToggleRightPanel()
    {
        _hasUserToggledPanel = true; // 标记用户已手动切换
        ShowRightPanel = !ShowRightPanel;
        
        // 保存状态到localStorage（包含错误处理）
        await SavePanelStateToStorage();

        StateHasChanged();

        if (OnRightPanelVisibilityChanged.HasDelegate)
        {
            await OnRightPanelVisibilityChanged.InvokeAsync(ShowRightPanel);
        }
    }

    /// <summary>
    /// 强制设置右侧面板状态 - 增强版本
    /// </summary>
    /// <param name="show">是否显示</param>
    /// <param name="saveToStorage">是否保存到本地存储</param>
    public async Task SetRightPanelState(bool show, bool saveToStorage = true)
    {
        if (ShowRightPanel == show) return; // 状态相同，无需切换

        ShowRightPanel = show;
        
        if (saveToStorage)
        {
            await SavePanelStateToStorage();
        }

        StateHasChanged();

        if (OnRightPanelVisibilityChanged.HasDelegate)
        {
            await OnRightPanelVisibilityChanged.InvokeAsync(ShowRightPanel);
        }
    }

    /// <summary>
    /// 保存面板状态到localStorage - 包含错误处理和降级方案
    /// </summary>
    private async Task SavePanelStateToStorage()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.storage.setRightPanelState", ShowRightPanel);
        }
        catch (JSException)
        {
            // 尝试降级方案
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", 
                    $"localStorage.setItem('aiChat_rightPanelOpen', '{ShowRightPanel.ToString().ToLower()}')");
            }
            catch (Exception)
            {
                // 静默处理错误
            }
        }
    }

    /// <summary>
    /// 调试用：获取右侧面板状态信息
    /// </summary>
    public async Task<object> GetPanelDebugInfo()
    {
        try
        {
            var localStorageState = await JSRuntime.InvokeAsync<bool>("aiChatHelper.storage.getRightPanelState");
            
            return new
            {
                CurrentShowRightPanel = ShowRightPanel,
                LocalStorageState = localStorageState,
                HasUserToggled = _hasUserToggledPanel,
                InitialPanelState = _initialPanelState,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        catch (Exception ex)
        {
            return new
            {
                Error = ex.Message,
                CurrentShowRightPanel = ShowRightPanel,
                HasUserToggled = _hasUserToggledPanel,
                InitialPanelState = _initialPanelState,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }

    /// <summary>
    /// 调试用：重置面板状态
    /// </summary>
    public async Task ResetPanelState()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.storage.clearPanelStates");
            ShowRightPanel = false; // 重置为默认关闭状态
            _hasUserToggledPanel = false;
            _initialPanelState = false;
            StateHasChanged();
        }
        catch (Exception)
        {
            // 静默处理错误
        }
    }

    /// <summary>
    /// 清空输入框内容
    /// </summary>
    public async Task ClearCurrentMessage()
    {
        CurrentMessage = string.Empty;
        StateHasChanged();
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 重置组件状态（包括清空输入框）
    /// </summary>
    public async Task ResetComponentState()
    {
        CurrentMessage = string.Empty;
        IsLoading = false;
        IsVoiceRecording = false;
        StateHasChanged();
        await Task.CompletedTask;
    }
    #endregion

    #region 事件处理方法
    private async Task HandleSendMessage(string message)
    {
        SetLoading(true);
        await AddMessage(message, true);

        try
        {
            if (OnMessageSent.HasDelegate)
            {
                await OnMessageSent.InvokeAsync(message);
            }
        }
        catch (Exception ex)
        {
            await AddMessage($"发送消息时出现错误：{ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task HandleClearChat()
    {
        await ClearMessages();

        if (OnChatCleared.HasDelegate)
        {
            await OnChatCleared.InvokeAsync();
        }
    }

    private async Task HandleNewChat()
    {
        CurrentSessionId = Guid.NewGuid().ToString();
        var newSession = new ChatSession
        {
            Id = CurrentSessionId,
            Title = $"对话 {ChatSessions.Count + 1}",
            CreateTime = DateTime.Now
        };

        ChatSessions.Insert(0, newSession);
        await ClearMessages();

        if (OnNewChatCreated.HasDelegate)
        {
            await OnNewChatCreated.InvokeAsync();
        }
    }

    private async Task HandleSessionSelected(string sessionId)
    {
        CurrentSessionId = sessionId;

        if (OnSessionSelected.HasDelegate)
        {
            await OnSessionSelected.InvokeAsync(sessionId);
        }
    }

    private async Task HandleSessionDeleted(string sessionId)
    {
        // 如果删除的是当前会话，需要切换到其他会话或创建新会话
        if (sessionId == CurrentSessionId)
        {
            var remainingSessions = ChatSessions.Where(s => s.Id != sessionId).ToList();
            if (remainingSessions.Any())
            {
                await HandleSessionSelected(remainingSessions.First().Id);
            }
            else
            {
                await HandleNewChat();
            }
        }

        ChatSessions.RemoveAll(s => s.Id == sessionId);
        StateHasChanged();

        if (OnSessionDeleted.HasDelegate)
        {
            await OnSessionDeleted.InvokeAsync(sessionId);
        }
    }

    private async Task HandleCollapsedChanged(bool isCollapsed)
    {
        IsLeftPanelCollapsed = isCollapsed;
        StateHasChanged();

        if (OnLeftPanelCollapsedChanged.HasDelegate)
        {
            await OnLeftPanelCollapsedChanged.InvokeAsync(isCollapsed);
        }
    }

    private async Task HandleModelChanged(string modelId)
    {
        SelectedModel = modelId;
        if (OnModelChanged.HasDelegate)
        {
            await OnModelChanged.InvokeAsync(modelId);
        }
    }

    private async Task HandleTemperatureChanged(double temperature)
    {
        Temperature = temperature;
        if (OnTemperatureChanged.HasDelegate)
        {
            await OnTemperatureChanged.InvokeAsync(temperature);
        }
    }

    /// <summary>
    /// 处理设置面板切换
    /// </summary>
    private async Task HandleToggleSettings()
    {
        await ToggleRightPanel();
    }

    // 其他事件处理方法...
    private async Task HandleCopyMessage(string content)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("copyToClipboard", content);
        }
        catch
        {
            // 降级处理
        }
    }

    private async Task HandleRegenerateResponse(ChatMessage message)
    {
        if (Messages.Contains(message))
        {
            Messages.Remove(message);
            StateHasChanged();
        }
    }

    private async Task HandleLikeMessage(ChatMessage message)
    {
        // 实现点赞功能
        await Task.CompletedTask;
    }

    private async Task HandleSetQuickMessage(string message)
    {
        CurrentMessage = message;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task HandleAttachFile()
    {
        await Task.CompletedTask;
    }

    private async Task HandleInsertTemplate()
    {
        await Task.CompletedTask;
    }

    private async Task HandleToggleVoice()
    {
        IsVoiceRecording = !IsVoiceRecording;
        await Task.CompletedTask;
    }

    private async Task HandleDeepThink()
    {
        // 实现深度思考功能
        // TODO: 添加深度思考逻辑
        await Task.CompletedTask;
    }

    private async Task HandleWebSearch()
    {
        // 实现联网搜索功能
        // TODO: 添加联网搜索逻辑
        await Task.CompletedTask;
    }

    private async Task HandleExportChat()
    {
        try
        {
            var messagesData = Messages.Select(m => new
            {
                content = m.Content,
                isUser = m.IsUser,
                timestamp = m.Timestamp.ToString("O")
            }).ToArray();

            await JSRuntime.InvokeVoidAsync("exportChat", messagesData, Title);
        }
        catch (JSException)
        {
            // 静默处理错误
        }
    }

    private async Task HandleShareChat()
    {
        try
        {
            var messagesData = Messages.Select(m => new
            {
                content = m.Content,
                isUser = m.IsUser,
                timestamp = m.Timestamp.ToString("O")
            }).ToArray();

            await JSRuntime.InvokeAsync<bool>("shareChat", messagesData, Title);
        }
        catch (JSException)
        {
            // 静默处理错误
        }
    }

    private async Task HandleToggleTheme()
    {
        try
        {
            var newTheme = await JSRuntime.InvokeAsync<string>("aiChatHelper.toggleTheme");
            StateHasChanged();
        }
        catch (JSException)
        {
            // 提供降级方案
            try
            {
                // 尝试直接操作DOM
                await JSRuntime.InvokeVoidAsync("eval", @"
                    const body = document.body;
                    const isDark = body.classList.contains('dark-theme');
                    if (isDark) {
                        body.classList.remove('dark-theme');
                        localStorage.setItem('theme', 'light');
                    } else {
                        body.classList.add('dark-theme');
                        localStorage.setItem('theme', 'dark');
                    }
                ");
                StateHasChanged();
            }
            catch (JSException)
            {
                // 完全降级，不做任何操作
            }
        }
    }

    private async Task HandleResetSettings()
    {
        Temperature = 0.7;
        MaxTokens = 2048;
        SelectedModel = AvailableModels.FirstOrDefault()?.Id ?? "gpt-3.5-turbo";
        EnableMemory = true;
        EnableStreaming = true;

        StateHasChanged();
    }
    #endregion

    #region 资源释放
    public void Dispose()
    {
        // 清理资源
    }
    #endregion
}
