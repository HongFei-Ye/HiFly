using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.BbAiChat.Components.Input;

/// <summary>
/// 智能文本输入框组件
/// </summary>
public partial class TextInputField : ComponentBase
{
    [Inject]
    [NotNull]
    private IJSRuntime? JSRuntime { get; set; }

    /// <summary>
    /// 当前值
    /// </summary>
    [Parameter]
    public string CurrentValue 
    { 
        get => _currentValue;
        set
        {
            if (_currentValue != value)
            {
                // 简化过滤逻辑，只过滤明确的测试数据
                _currentValue = (value == "CurrentMessage") ? string.Empty : (value ?? string.Empty);
                
                // 使用同步方式通知变化，避免异步竞态条件
                if (CurrentValueChanged.HasDelegate)
                {
                    _ = CurrentValueChanged.InvokeAsync(_currentValue);
                }
                
                // 强制触发状态更新，确保父组件能及时响应
                StateHasChanged();
            }
        }
    }
    
    private string _currentValue = string.Empty;

    /// <summary>
    /// 值变更事件
    /// </summary>
    [Parameter]
    public EventCallback<string> CurrentValueChanged { get; set; }

    /// <summary>
    /// 占位符文本
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "请输入您的消息...";

    /// <summary>
    /// 最大字符长度
    /// </summary>
    [Parameter]
    public int MaxLength { get; set; } = 2000;

    /// <summary>
    /// 是否禁用输入
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 键盘按下事件
    /// </summary>
    [Parameter]
    public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// 发送消息事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSendMessage { get; set; }

    /// <summary>
    /// 焦点获取事件
    /// </summary>
    [Parameter]
    public EventCallback OnFocusRequested { get; set; }

    private ElementReference textareaRef;

    protected override async Task OnInitializedAsync()
    {
        // 简化初始化，只确保字段为空
        _currentValue = string.Empty;
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 移除可能干扰输入的清空操作和聚焦操作
            // await ClearTextareaValue();
            // await FocusTextarea();
            
            // 只进行基本的自动高度调整初始化
            try
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.initAutoResize", textareaRef);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to initialize auto resize: {ex.Message}");
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnParametersSet()
    {
        
        base.OnParametersSet();
    }

    /// <summary>
    /// 聚焦到输入框
    /// </summary>
    public async Task FocusTextarea(bool preserveSelection = false)
    {
        try
        {
            if (preserveSelection)
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.focusElementPreserveSelection", textareaRef);
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.focusElement", textareaRef);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Focus failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 重置输入框高度
    /// </summary>
    public async Task ResetHeight()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.resetTextareaHeight", textareaRef);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Reset height failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 清空内容
    /// </summary>
    public async Task ClearContent()
    {
        _currentValue = string.Empty;
        if (CurrentValueChanged.HasDelegate)
        {
            await CurrentValueChanged.InvokeAsync(_currentValue);
        }
        await ResetHeight();
        StateHasChanged();
    }

    /// <summary>
    /// 处理键盘按下事件
    /// </summary>
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        // 发送消息的快捷键处理
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            if (!string.IsNullOrWhiteSpace(CurrentValue))
            {
                // 调用发送消息事件
                if (OnSendMessage.HasDelegate)
                {
                    await OnSendMessage.InvokeAsync(CurrentValue.Trim());
                }
            }
            return;
        }

        // 全选快捷键
        if (e.Key == "a" && e.CtrlKey)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.selectAllText", textareaRef);
            }
            catch { /* 让浏览器处理 */ }
            return;
        }

        // 选择当前词快捷键
        if (e.Key == "d" && e.CtrlKey)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("aiChatHelper.selectWordAtCursor", textareaRef);
            }
            catch { /* 忽略错误 */ }
            return;
        }

        // 通知父组件键盘事件
        if (OnKeyDown.HasDelegate)
        {
            await OnKeyDown.InvokeAsync(e);
        }
    }

    /// <summary>
    /// 处理文本选择开始事件
    /// </summary>
    private async Task HandleTextSelectionStart(MouseEventArgs mouseArgs)
    {
        if (mouseArgs.Button != 0) return;
        await FocusTextarea(preserveSelection: true);
    }

    /// <summary>
    /// 处理文本选择结束事件
    /// </summary>
    private async Task HandleTextSelectionEnd(MouseEventArgs mouseArgs)
    {
        if (mouseArgs.Button != 0) return;
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
            await JSRuntime.InvokeVoidAsync("aiChatHelper.selectWordAtCursorWithDebounce", textareaRef);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Double-click selection failed: {ex.Message}");
        }
    }
}
