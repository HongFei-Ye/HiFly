// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.BbAiChat.Components.Input;

/// <summary>
/// 聊天输入区域容器组件 - 重构后的版本
/// </summary>
public partial class ChatInputArea : ComponentBase
{
    [Inject]
    [NotNull]
    private IJSRuntime? JSRuntime { get; set; }

    #region 参数定义

    /// <summary>
    /// 当前消息
    /// </summary>
    [Parameter]
    public string CurrentMessage { get; set; } = string.Empty;

    /// <summary>
    /// 当前消息变更事件
    /// </summary>
    [Parameter]
    public EventCallback<string> CurrentMessageChanged { get; set; }

    /// <summary>
    /// 输入框占位符
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "请输入您的消息...";

    /// <summary>
    /// 最大字符长度
    /// </summary>
    [Parameter]
    public int MaxLength { get; set; } = 2000;

    /// <summary>
    /// 输入提示文本
    /// </summary>
    [Parameter]
    public string InputHint { get; set; } = "使用 Enter 发送消息，Shift+Enter 换行";

    /// <summary>
    /// 是否显示字符进度条
    /// </summary>
    [Parameter]
    public bool ShowCharacterProgress { get; set; } = false;

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// 是否正在语音录制
    /// </summary>
    [Parameter]
    public bool IsVoiceRecording { get; set; }

    #endregion

    #region 模板参数

    /// <summary>
    /// 自定义工具栏模板
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

    /// <summary>
    /// 快捷操作模板
    /// </summary>
    [Parameter]
    public RenderFragment? QuickActionsTemplate { get; set; }

    #endregion

    #region 事件参数

    /// <summary>
    /// 发送消息事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSendMessage { get; set; }

    /// <summary>
    /// 附加文件事件
    /// </summary>
    [Parameter]
    public EventCallback OnAttachFile { get; set; }

    /// <summary>
    /// 插入模板事件
    /// </summary>
    [Parameter]
    public EventCallback OnInsertTemplate { get; set; }

    /// <summary>
    /// 切换语音事件
    /// </summary>
    [Parameter]
    public EventCallback OnToggleVoice { get; set; }

    /// <summary>
    /// 深度思考事件
    /// </summary>
    [Parameter]
    public EventCallback OnDeepThink { get; set; }

    /// <summary>
    /// 联网搜索事件
    /// </summary>
    [Parameter]
    public EventCallback OnWebSearch { get; set; }

    /// <summary>
    /// 表情符号事件
    /// </summary>
    [Parameter]
    public EventCallback OnEmojiPicker { get; set; }

    #endregion

    #region 私有字段和引用

    private TextInputField? textInputField;
    
    // 调试相关字段
    private bool _lastCanSendState = false;
    
    // 实时更新状态
    private int _currentMessageLength = 0;
    private bool _canSendMessage = false;

    #endregion

    #region 生命周期方法

    protected override void OnInitialized()
    {
        // 确保 CurrentMessage 在初始化时为空字符串
        if (CurrentMessage == "CurrentMessage" || string.IsNullOrWhiteSpace(CurrentMessage))
        {
            CurrentMessage = string.Empty;
        }
        
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // 确保 CurrentMessage 不包含测试数据
        if (CurrentMessage == "CurrentMessage")
        {
            CurrentMessage = string.Empty;
            
            // 通知父组件值已清理
            if (CurrentMessageChanged.HasDelegate)
            {
                _ = Task.Run(async () => await CurrentMessageChanged.InvokeAsync(CurrentMessage));
            }
        }
        
        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // 仅在首次渲染后触发一次状态同步
                _currentMessageLength = GetSafeCurrentLength();
                _canSendMessage = CanSendMessage();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                // 记录错误但不影响功能
            }
        }
        
        await base.OnAfterRenderAsync(firstRender);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 聚焦到输入框
    /// </summary>
    public async Task FocusInput()
    {
        if (textInputField != null)
        {
            await textInputField.FocusTextarea();
        }
    }

    /// <summary>
    /// 清空输入内容
    /// </summary>
    public async Task ClearInput()
    {
        // 1. 清空TextInputField组件
        if (textInputField != null)
        {
            await textInputField.ClearContent();
        }
        
        // 2. 清空当前消息状态
        CurrentMessage = string.Empty;
        
        // 3. 重置内部实时状态
        _currentMessageLength = 0;
        _canSendMessage = false;
        
        // 4. 立即强制UI更新，确保InputStatsDisplay显示0/2000
        StateHasChanged();
        
        // 5. 通知父组件状态变更
        if (CurrentMessageChanged.HasDelegate)
        {
            await CurrentMessageChanged.InvokeAsync(CurrentMessage);
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取安全的当前消息长度，过滤测试数据
    /// </summary>
    private int GetSafeCurrentLength()
    {
        // 只过滤特定的测试数据，不过滤正常的用户输入
        if (string.IsNullOrEmpty(CurrentMessage) || CurrentMessage == "CurrentMessage")
        {
            return 0;
        }
        return CurrentMessage.Length;
    }
    
    /// <summary>
    /// 检查当前消息是否可以发送（不是空内容且不是测试数据）
    /// </summary>
    private bool CanSendMessage()
    {
        var canSend = !string.IsNullOrWhiteSpace(CurrentMessage) && 
                      CurrentMessage != "CurrentMessage" && 
                      !IsLoading;
        
        return canSend;
    }

    /// <summary>
    /// 强制启用发送功能 - 调试用
    /// </summary>
    public void ForceEnableSending()
    {
        // 如果输入框为空，添加测试内容
        if (string.IsNullOrWhiteSpace(CurrentMessage))
        {
            CurrentMessage = "测试消息";
        }
        
        // 重置状态
        IsLoading = false;
        
        // 强制更新UI
        StateHasChanged();
    }

    /// <summary>
    /// 处理实时状态更新 - 简化版
    /// </summary>
    private async Task HandleRealTimeUpdate(string value)
    {
        try
        {
            // 更新内部状态
            var newLength = value?.Length ?? 0;
            var newCanSend = !string.IsNullOrWhiteSpace(value) && !IsLoading;
            
            // 只有在值真正变化时才更新状态
            if (_currentMessageLength != newLength || _canSendMessage != newCanSend)
            {
                _currentMessageLength = newLength;
                _canSendMessage = newCanSend;
                
                // 立即强制UI更新
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不影响功能
        }
    }

    /// <summary>
    /// 通过JavaScript更新UI状态
    /// </summary>
    private async Task UpdateUIStateViaJavaScript()
    {
        // 这个方法已被简化的实时更新机制替代，暂时保留用于调试
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理发送消息
    /// </summary>
    private async Task HandleSendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || IsLoading)
            return;

        // 清空输入内容
        await ClearInput();

        // 发送消息
        if (OnSendMessage.HasDelegate)
        {
            await OnSendMessage.InvokeAsync(message);
        }

        // 重新聚焦输入框
        await FocusInput();
    }

    /// <summary>
    /// 处理发送按钮点击
    /// </summary>
    private async Task HandleSendButtonClick(MouseEventArgs mouseArgs)
    {
        if (!string.IsNullOrWhiteSpace(CurrentMessage))
        {
            await HandleSendMessage(CurrentMessage);
        }
    }

    /// <summary>
    /// 处理输入区域点击事件
    /// </summary>
    private async Task HandleInputAreaClick(MouseEventArgs mouseArgs)
    {
        // 只处理左键点击
        if (mouseArgs.Button != 0 || IsLoading) return;

        // 检查是否有文本选择
        try
        {
            var hasSelection = await JSRuntime.InvokeAsync<bool>("eval", 
                "!!document.querySelector('textarea.chat-input-enhanced')?.selectionStart !== document.querySelector('textarea.chat-input-enhanced')?.selectionEnd");
            
            if (!hasSelection)
            {
                await FocusInput();
            }
        }
        catch
        {
            await FocusInput();
        }
    }

    /// <summary>
    /// 处理字符计数点击事件
    /// </summary>
    private async Task HandleStatsClick(MouseEventArgs mouseArgs)
    {
        if (mouseArgs.Button != 0 || IsLoading) return;

        if (textInputField != null)
        {
            await textInputField.FocusTextarea(preserveSelection: true);
        }
    }

    #endregion
}
