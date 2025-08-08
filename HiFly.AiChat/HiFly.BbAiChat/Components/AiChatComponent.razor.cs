// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.BbAiChat.Components;

public partial class AiChatComponent : ComponentBase, IDisposable
{
    #region 依赖注入
    [Inject]
    [NotNull]
    private IJSRuntime? JSRuntime { get; set; }
    #endregion

    #region 参数
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
    /// 是否显示右侧面板
    /// </summary>
    [Parameter]
    public bool ShowRightPanel { get; set; } = true;

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

    /// <summary>
    /// 顶部面板模板
    /// </summary>
    [Parameter]
    public RenderFragment? TopPanelTemplate { get; set; }

    /// <summary>
    /// 左侧面板模板
    /// </summary>
    [Parameter]
    public RenderFragment? LeftPanelTemplate { get; set; }

    /// <summary>
    /// 右侧面板模板
    /// </summary>
    [Parameter]
    public RenderFragment? RightPanelTemplate { get; set; }

    /// <summary>
    /// 工具栏模板
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

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
    /// 删除会话回调
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSessionDeleted { get; set; }
    #endregion

    #region 私有字段和属性
    private ElementReference messagesContainer;
    private ElementReference inputTextarea;

    private string CurrentMessage { get; set; } = string.Empty;
    private bool IsLoading { get; set; } = false;
    private bool IsVoiceRecording { get; set; } = false;

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
    #endregion

    #region 生命周期方法
    protected override async Task OnInitializedAsync()
    {
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
            try
            {
                await JSRuntime.InvokeVoidAsync("initTheme");
            }
            catch (JSException)
            {
                // 如果JavaScript函数不可用，忽略错误
                System.Console.WriteLine("initTheme function not available, using default theme");
            }

            await FocusInput();
        }

        // 滚动到底部
        if (Messages.Any())
        {
            await ScrollToBottom();
        }

        await base.OnAfterRenderAsync(firstRender);
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

        await Task.Delay(100); // 确保DOM更新
        await ScrollToBottom();
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
        await FocusInput();
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
        await ScrollToBottom();
    }
    #endregion

    #region 私有方法
    private async Task OnSendMessage()
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage) || IsLoading)
            return;

        var userMessage = CurrentMessage.Trim();
        CurrentMessage = string.Empty;

        await AddMessage(userMessage, true);

        SetLoading(true);

        try
        {
            if (OnMessageSent.HasDelegate)
            {
                await OnMessageSent.InvokeAsync(userMessage);
            }
        }
        catch (Exception ex)
        {
            await AddMessage($"发送消息时出现错误：{ex.Message}");
        }
        finally
        {
            SetLoading(false);
            await FocusInput();
        }
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await OnSendMessage();
        }
    }

    private bool ShouldPreventEnter(KeyboardEventArgs e)
    {
        return e.Key == "Enter" && !e.ShiftKey;
    }

    private async Task OnTemperatureSliderChanged(double value)
    {
        if (OnTemperatureChanged.HasDelegate)
        {
            await OnTemperatureChanged.InvokeAsync(value);
        }
    }

    private async Task OnClearChat()
    {
        await ClearMessages();

        if (OnChatCleared.HasDelegate)
        {
            await OnChatCleared.InvokeAsync();
        }
    }

    private async Task OnNewChat()
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

    private async Task OnSelectSession(string sessionId)
    {
        CurrentSessionId = sessionId;

        if (OnSessionSelected.HasDelegate)
        {
            await OnSessionSelected.InvokeAsync(sessionId);
        }
    }

    private async Task OnToggleSettings()
    {
        // 实现设置面板切换逻辑
        await Task.CompletedTask;
    }

    private async Task OnCopyMessage(string content)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", content);
            // 可以添加复制成功的提示
        }
        catch
        {
            // 降级处理，使用传统方式复制
        }
    }

    private async Task OnRegenerateResponse(ChatMessage message)
    {
        // 重新生成响应的逻辑
        if (Messages.Contains(message))
        {
            Messages.Remove(message);
            StateHasChanged();
        }
    }

    private async Task OnAttachFile()
    {
        // 实现文件附件功能
        await Task.CompletedTask;
    }

    private async Task OnInsertTemplate()
    {
        // 实现模板插入功能
        await Task.CompletedTask;
    }

    private async Task OnToggleVoice()
    {
        IsVoiceRecording = !IsVoiceRecording;
        // 实现语音输入功能
        await Task.CompletedTask;
    }

    private async Task OnToggleLeftPanel()
    {
        IsLeftPanelCollapsed = !IsLeftPanelCollapsed;
        StateHasChanged();

        if (OnLeftPanelCollapsedChanged.HasDelegate)
        {
            await OnLeftPanelCollapsedChanged.InvokeAsync(IsLeftPanelCollapsed);
        }
    }

    private async Task OnDeleteSession(string sessionId)
    {
        // 如果删除的是当前会话，需要切换到其他会话或创建新会话
        if (sessionId == CurrentSessionId)
        {
            var remainingSessions = ChatSessions.Where(s => s.Id != sessionId).ToList();
            if (remainingSessions.Any())
            {
                // 切换到最新的会话
                await OnSelectSession(remainingSessions.First().Id);
            }
            else
            {
                // 没有其他会话了，创建新会话
                await OnNewChat();
            }
        }

        // 移除会话
        ChatSessions.RemoveAll(s => s.Id == sessionId);
        StateHasChanged();

        if (OnSessionDeleted.HasDelegate)
        {
            await OnSessionDeleted.InvokeAsync(sessionId);
        }
    }

    private string FormatAiMessage(string content)
    {
        // 简单的Markdown格式处理
        content = content.Replace("\n", "<br/>");
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\*\*(.*?)\*\*",
            "<strong>$1</strong>");
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\*(.*?)\*",
            "<em>$1</em>");
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"`(.*?)`",
            "<code>$1</code>");

        return content;
    }

    private async Task ScrollToBottom()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("scrollToBottom", messagesContainer);
        }
        catch
        {
            // 忽略JS调用失败
        }
    }

    private async Task FocusInput()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("focusElement", inputTextarea);
        }
        catch
        {
            // 忽略焦点设置失败
        }
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "刚刚";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}分钟前";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}小时前";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}天前";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}周前";

        return dateTime.ToString("MM-dd");
    }

    private void SetQuickMessage(string message)
    {
        CurrentMessage = message;
        StateHasChanged();
    }

    private string GetSessionDuration()
    {
        if (!Messages.Any()) return "0分钟";

        var firstMessage = Messages.MinBy(m => m.Timestamp);
        var lastMessage = Messages.MaxBy(m => m.Timestamp);

        if (firstMessage == null || lastMessage == null) return "0分钟";

        var duration = lastMessage.Timestamp - firstMessage.Timestamp;
        if (duration.TotalMinutes < 1) return "1分钟";
        if (duration.TotalHours < 1) return $"{(int)duration.TotalMinutes}分钟";
        return $"{(int)duration.TotalHours}小时";
    }

    private string GetTokensUsed()
    {
        // 简单估算：平均每个字符约0.75个token
        var totalChars = Messages.Sum(m => m.Content.Length);
        var estimatedTokens = (int)(totalChars * 0.75);

        if (estimatedTokens < 1000) return estimatedTokens.ToString();
        if (estimatedTokens < 1000000) return $"{estimatedTokens / 1000:F1}K";
        return $"{estimatedTokens / 1000000:F1}M";
    }

    private async Task OnExportChat()
    {
        if (Messages.Any())
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
            catch (JSException ex)
            {
                System.Console.WriteLine($"Export failed: {ex.Message}");
                // 可以添加用户提示
            }
        }
    }

    private async Task OnShareChat()
    {
        if (Messages.Any())
        {
            try
            {
                var messagesData = Messages.Select(m => new
                {
                    content = m.Content,
                    isUser = m.IsUser,
                    timestamp = m.Timestamp.ToString("O")
                }).ToArray();

                var success = await JSRuntime.InvokeAsync<bool>("shareChat", messagesData, Title);
                if (!success)
                {
                    // 可以添加提示信息，告知用户分享失败或已取消
                    System.Console.WriteLine("分享失败或已取消");
                }
            }
            catch (JSException ex)
            {
                System.Console.WriteLine($"Share failed: {ex.Message}");
                // 降级到简单的复制功能
                await OnCopyMessage(string.Join("\n\n", Messages.Select(m =>
                    $"{(m.IsUser ? "用户" : "AI")}: {m.Content}")));
            }
        }
    }

    private async Task OnToggleTheme()
    {
        try
        {
            var newTheme = await JSRuntime.InvokeAsync<string>("toggleTheme");
            System.Console.WriteLine($"主题已切换到: {newTheme}");
            StateHasChanged();
        }
        catch (JSException ex)
        {
            System.Console.WriteLine($"Theme toggle failed: {ex.Message}");
        }
    }

    private async Task OnResetSettings()
    {
        try
        {
            // 重置所有设置到默认值
            Temperature = 0.7;
            MaxTokens = 2048;
            SelectedModel = AvailableModels.FirstOrDefault()?.Id ?? "gpt-3.5-turbo";
            EnableMemory = true;
            EnableStreaming = true;

            // 可以添加用户提示
            System.Console.WriteLine("设置已重置为默认值");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"重置设置失败: {ex.Message}");
        }
    }

    #endregion

    #region 模型数据类
    public class ChatMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }
        public string SessionId { get; set; } = string.Empty;
    }

    public class ChatSession
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime LastActiveTime { get; set; }
    }

    public class AiModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    #endregion

    #region 资源释放
    public void Dispose()
    {
        // 清理资源
    }
    #endregion

    #region 高级设置属性
    /// <summary>
    /// 是否启用上下文记忆
    /// </summary>
    [Parameter]
    public bool EnableMemory { get; set; } = true;

    /// <summary>
    /// 是否启用流式响应
    /// </summary>
    [Parameter]
    public bool EnableStreaming { get; set; } = true;
    #endregion
}
