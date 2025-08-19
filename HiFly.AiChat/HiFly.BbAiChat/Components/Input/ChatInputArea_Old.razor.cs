// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.BbAiChat.Components.Input;

/// <summary>
/// 聊天输入区域组件
/// </summary>
public partial class ChatInputArea_Old : ComponentBase
{
    [Inject]
    [NotNull]
    private IJSRuntime? JSRuntime { get; set; }

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
    /// 是否正在加载
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// 是否正在语音录制
    /// </summary>
    [Parameter]
    public bool IsVoiceRecording { get; set; }

    /// <summary>
    /// 自定义工具栏模板
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

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

    private ElementReference inputTextarea;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await FocusInput();

            // 初始化自动高度调整
            try
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.initAutoResize", inputTextarea);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to initialize auto resize: {ex.Message}");
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HandleSendMessage(MouseEventArgs? mouseArgs = null)
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage) || IsLoading)
            return;

        // 检查是否是左键点击（可选）
        if (mouseArgs != null && mouseArgs.Button != 0)
            return; // 只处理左键点击

        var message = CurrentMessage.Trim();

        // 首先清空本地状态
        CurrentMessage = string.Empty;

        // 然后通知父组件状态变化
        if (CurrentMessageChanged.HasDelegate)
        {
            await CurrentMessageChanged.InvokeAsync(CurrentMessage);
        }

        // 重置输入框高度
        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.resetTextareaHeight", inputTextarea);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Reset textarea height failed: {ex.Message}");
        }

        // 最后发送消息
        if (OnSendMessage.HasDelegate)
        {
            await OnSendMessage.InvokeAsync(message);
        }

        // 触发UI更新
        StateHasChanged();
        await FocusInput();
    }

    private async Task HandleSendMessageClick(MouseEventArgs mouseArgs)
    {
        await HandleSendMessage(mouseArgs);
    }

    /// <summary>
    /// 处理鼠标按下事件
    /// </summary>
    private async Task HandleMouseDown(MouseEventArgs mouseArgs)
    {
        // 可以根据按钮类型执行不同操作
        switch (mouseArgs.Button)
        {
            case 0: // 左键
                // 左键按下的处理逻辑
                break;
            case 1: // 中键
                // 中键按下的处理逻辑
                break;
            case 2: // 右键
                // 右键按下的处理逻辑
                break;
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理双击事件
    /// </summary>
    private async Task HandleDoubleClick(MouseEventArgs mouseArgs)
    {
        // 双击处理逻辑，比如快速操作
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理鼠标进入事件
    /// </summary>
    private async Task HandleMouseEnter(MouseEventArgs mouseArgs)
    {
        // 鼠标悬停效果
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理鼠标离开事件
    /// </summary>
    private async Task HandleMouseLeave(MouseEventArgs mouseArgs)
    {
        // 鼠标离开效果
        await Task.CompletedTask;
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            // 阻止默认的换行行为，并发送消息
            await HandleSendMessage();
        }
        else if (e.Key == "a" && e.CtrlKey)
        {
            // Ctrl+A 全选文本
            try
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.selectAllText", inputTextarea);
            }
            catch
            {
                // 让浏览器处理默认的Ctrl+A
            }
        }
        else if (e.Key == "d" && e.CtrlKey)
        {
            // Ctrl+D 选择当前词（类似VSCode）
            try
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.selectWordAtCursor", inputTextarea);
            }
            catch
            {
                // 忽略错误
            }
        }
        else if (e.Key == "z" && e.CtrlKey && !e.ShiftKey)
        {
            // Ctrl+Z 撤销（让浏览器处理）
            // 这里可以添加自定义撤销逻辑
        }
        else if (e.Key == "z" && e.CtrlKey && e.ShiftKey)
        {
            // Ctrl+Shift+Z 重做（让浏览器处理）
            // 这里可以添加自定义重做逻辑
        }
    }

    private bool ShouldPreventEnter(KeyboardEventArgs e)
    {
        // 只在单独按 Enter 键时阻止默认行为（不包括 Shift+Enter）
        return e.Key == "Enter" && !e.ShiftKey;
    }

    private async Task HandleCurrentMessageChanged(ChangeEventArgs e)
    {
        CurrentMessage = e.Value?.ToString() ?? string.Empty;
        if (CurrentMessageChanged.HasDelegate)
        {
            await CurrentMessageChanged.InvokeAsync(CurrentMessage);
        }
    }

    private async Task HandleAttachFile()
    {
        if (OnAttachFile.HasDelegate)
        {
            await OnAttachFile.InvokeAsync();
        }
    }

    private async Task HandleInsertTemplate()
    {
        if (OnInsertTemplate.HasDelegate)
        {
            await OnInsertTemplate.InvokeAsync();
        }
    }

    private async Task HandleToggleVoice()
    {
        if (OnToggleVoice.HasDelegate)
        {
            await OnToggleVoice.InvokeAsync();
        }
    }

    private async Task HandleDeepThink()
    {
        if (OnDeepThink.HasDelegate)
        {
            await OnDeepThink.InvokeAsync();
        }
    }

    private async Task HandleWebSearch()
    {
        if (OnWebSearch.HasDelegate)
        {
            await OnWebSearch.InvokeAsync();
        }
    }

    /// <summary>
    /// 处理输入框区域点击事件 - 重新激活输入框
    /// </summary>
    private async Task HandleInputAreaClick(MouseEventArgs mouseArgs)
    {
        // 只处理左键点击
        if (mouseArgs.Button != 0) return;

        // 如果正在加载或禁用状态，不处理点击
        if (IsLoading) return;

        // 检查是否在拖拽选择过程中，如果是则不处理
        // 这里通过检查鼠标位置变化来判断是否为拖拽
        var hasSelection = await JSRuntime.InvokeAsync<bool>("eval",
            "!!document.querySelector('textarea.chat-input-enhanced')?.selectionStart !== document.querySelector('textarea.chat-input-enhanced')?.selectionEnd");

        if (!hasSelection)
        {
            // 只有在没有选择文本时才重新聚焦
            await FocusInput();
        }
    }

    /// <summary>
    /// 处理输入框字段点击事件
    /// </summary>
    private async Task HandleInputFieldClick(MouseEventArgs mouseArgs)
    {
        // 只处理左键点击
        if (mouseArgs.Button != 0) return;

        // 如果正在加载或禁用状态，不处理点击
        if (IsLoading) return;

        // 对于input-field的点击，我们不需要额外处理
        // 让textarea自己处理焦点和光标位置
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理字符计数区域点击事件
    /// </summary>
    private async Task HandleStatsClick(MouseEventArgs mouseArgs)
    {
        // 只处理左键点击
        if (mouseArgs.Button != 0) return;

        // 如果正在加载或禁用状态，不处理点击
        if (IsLoading) return;

        // 点击字符计数时聚焦输入框但保留选择
        await FocusInput(preserveSelection: true);
    }

    private async Task FocusInput(bool preserveSelection = false)
    {
        try
        {
            if (preserveSelection)
            {
                // 保留选择状态的焦点设置
                await JSRuntime.InvokeVoidAsync("aiChatHelper.focusElementPreserveSelection", inputTextarea);
            }
            else
            {
                // 优先尝试使用增强版焦点函数
                var success = await JSRuntime.InvokeAsync<bool>("aiChatHelper.focusElement", inputTextarea);
                if (success) return;
            }
        }
        catch
        {
            // 如果失败，尝试智能焦点查找
            try
            {
                var success = await JSRuntime.InvokeAsync<bool>("aiChatHelper.focusInputElement");
                if (success) return;
            }
            catch
            {
                // 降级：尝试全局函数
                try
                {
                    await JSRuntime.InvokeVoidAsync("focusElement", inputTextarea);
                    return;
                }
                catch
                {
                    // 最后降级：直接DOM操作
                    try
                    {
                        if (preserveSelection)
                        {
                            await JSRuntime.InvokeVoidAsync("eval", @"
                                const textarea = document.querySelector('textarea.chat-input-enhanced') || 
                                               document.querySelector('textarea');
                                if (textarea && !textarea.matches(':focus')) {
                                    textarea.focus();
                                }
                            ");
                        }
                        else
                        {
                            await JSRuntime.InvokeVoidAsync("eval", @"
                                const textarea = document.querySelector('textarea.chat-input-enhanced') || 
                                               document.querySelector('textarea');
                                if (textarea) {
                                    textarea.focus();
                                    textarea.setSelectionRange(textarea.value.length, textarea.value.length);
                                }
                            ");
                        }
                    }
                    catch
                    {
                        // 完全忽略焦点设置失败
                        System.Console.WriteLine("所有焦点设置方法都失败了");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 处理表情符号点击事件
    /// </summary>
    private async Task HandleEmojiClick()
    {
        if (OnEmojiPicker.HasDelegate)
        {
            await OnEmojiPicker.InvokeAsync();
        }
    }

    /// <summary>
    /// 处理文本选择事件
    /// </summary>
    private async Task HandleTextSelectionStart(MouseEventArgs mouseArgs)
    {
        // 只处理左键
        if (mouseArgs.Button != 0) return;

        // 确保输入框有焦点，但保留选择状态
        await FocusInput(preserveSelection: true);
    }

    /// <summary>
    /// 处理文本选择完成事件
    /// </summary>
    private async Task HandleTextSelectionEnd(MouseEventArgs mouseArgs)
    {
        if (mouseArgs.Button != 0) return;

        // 不需要任何额外处理，让浏览器处理选择完成
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理双击选择单词事件
    /// </summary>
    private async Task HandleTextDoubleClick(MouseEventArgs mouseArgs)
    {
        if (mouseArgs.Button != 0) return;

        try
        {
            // 获取当前详细信息用于调试
            var debugInfo = await JSRuntime.InvokeAsync<object>("aiChatHelper.getDetailedSelectionInfo", inputTextarea);
            System.Console.WriteLine($"Double-click debug info: {debugInfo}");

            // 使用防抖版本，避免重复触发
            await JSRuntime.InvokeVoidAsync("aiChatHelper.selectWordAtCursorWithDebounce", inputTextarea);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Double-click selection failed: {ex.Message}");
            // 降级处理 - 让浏览器处理默认的双击选择
        }
    }

    /// <summary>
    /// 处理三击选择全部文本事件
    /// </summary>
    private async Task HandleTextTripleClick()
    {
        try
        {
            // 使用JavaScript来选择全部文本
            await JSRuntime.InvokeVoidAsync("aiChatHelper.selectAllText", inputTextarea);
        }
        catch
        {
            // 降级处理
        }
    }

    /// <summary>
    /// 处理文本输入事件 - 自动调整高度
    /// </summary>
    private async Task HandleTextInput(ChangeEventArgs e)
    {
        // 更新消息内容
        CurrentMessage = e.Value?.ToString() ?? string.Empty;

        // 通知父组件状态变化
        if (CurrentMessageChanged.HasDelegate)
        {
            await CurrentMessageChanged.InvokeAsync(CurrentMessage);
        }

        // 自动调整输入框高度
        await AutoResizeTextarea();
    }

    /// <summary>
    /// 自动调整输入框高度
    /// </summary>
    private async Task AutoResizeTextarea()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.autoResizeTextarea", inputTextarea);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Auto resize failed: {ex.Message}");
        }
    }
}
