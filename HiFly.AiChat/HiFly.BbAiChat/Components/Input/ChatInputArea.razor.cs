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

    #endregion

    #region 生命周期方法

    protected override void OnInitialized()
    {
        // 简化初始化，确保 CurrentMessage 为空字符串
        CurrentMessage = string.Empty;
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // 确保 CurrentMessage 不包含测试数据
        if (CurrentMessage == "CurrentMessage")
        {
            CurrentMessage = string.Empty;
            
            // 移除Task.Run，使用同步方式通知父组件值已清理
            if (CurrentMessageChanged.HasDelegate)
            {
                _ = CurrentMessageChanged.InvokeAsync(CurrentMessage);
            }
        }
        
        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 启动UI状态实时同步，确保发送按钮和输入统计实时更新
            try
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.startUIStateSync");
                await JSRuntime.InvokeVoidAsync("aiChatHelper.enhanceBlazorSync");
                await JSRuntime.InvokeVoidAsync("aiChatHelper.enhanceEnterKeyHandling");
                
                // 添加自定义发送消息事件监听器
                await JSRuntime.InvokeVoidAsync("eval", @"
                    const textarea = document.querySelector('textarea.chat-input-enhanced');
                    if (textarea) {
                        textarea.addEventListener('sendMessage', (event) => {
                            console.log('📨 收到自定义发送消息事件:', event.detail.message);
                            // 这里可以触发Blazor的发送消息逻辑
                        });
                    }
                ");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to start UI state sync: {ex.Message}");
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
        if (textInputField != null)
        {
            await textInputField.ClearContent();
        }
        
        CurrentMessage = string.Empty;
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
        return !string.IsNullOrWhiteSpace(CurrentMessage) && CurrentMessage != "CurrentMessage";
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理消息变更
    /// </summary>
    private async Task OnCurrentMessageChanged(string message)
    {
        // 防止循环更新
        if (CurrentMessage == message) 
        {
            return;
        }
        
        // 过滤掉测试数据
        if (message == "CurrentMessage")
        {
            message = string.Empty;
        }
        
        CurrentMessage = message;
        
        // 立即强制重新渲染，确保UI状态同步
        StateHasChanged();
        
        // 通知父组件
        if (CurrentMessageChanged.HasDelegate)
        {
            await CurrentMessageChanged.InvokeAsync(CurrentMessage);
        }
        
        // 再次确保状态同步（防止异步延迟）
        await InvokeAsync(StateHasChanged);
    }

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
