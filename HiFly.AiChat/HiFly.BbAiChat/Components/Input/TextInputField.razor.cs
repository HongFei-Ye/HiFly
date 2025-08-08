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
                // 防止设置为测试数据
                if (value == "CurrentMessage")
                {
                    _currentValue = string.Empty;
                }
                else
                {
                    _currentValue = value ?? string.Empty;
                }
                
                // 通知父组件值变化
                _ = Task.Run(async () =>
                {
                    if (CurrentValueChanged.HasDelegate)
                    {
                        await CurrentValueChanged.InvokeAsync(_currentValue);
                    }
                    
                    // 自动调整高度
                    await AutoResizeTextarea();
                });
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
        // 强制确保CurrentValue为空字符串，清除任何可能的测试数据
        if (string.IsNullOrEmpty(_currentValue) || _currentValue == "CurrentMessage")
        {
            _currentValue = string.Empty;
        }
        
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 首次渲染时强制清空任何残留值
            await ClearTextareaValue();
            await InitializeTextarea();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnParametersSet()
    {
        // 确保CurrentValue不会是测试数据
        if (_currentValue == "CurrentMessage" || (string.IsNullOrWhiteSpace(_currentValue) && _currentValue != string.Empty))
        {
            _currentValue = string.Empty;
        }
        
        base.OnParametersSet();
    }

    /// <summary>
    /// 初始化输入框
    /// </summary>
    private async Task InitializeTextarea()
    {
        try
        {
            // 聚焦输入框
            await FocusTextarea();
            
            // 初始化自动高度调整
            await JSRuntime.InvokeVoidAsync("aiChatHelper.initAutoResize", textareaRef);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to initialize textarea: {ex.Message}");
        }
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
        CurrentValue = string.Empty;
        await ResetHeight();
        StateHasChanged();
    }

    /// <summary>
    /// 强制清空textarea的值
    /// </summary>
    private async Task ClearTextareaValue()
    {
        try
        {
            // 首先尝试使用新的清理函数
            await JSRuntime.InvokeVoidAsync("aiChatHelper.forceCleanAllTextareas");
            
            // 然后针对当前元素再次确保清空
            await JSRuntime.InvokeVoidAsync("eval", 
                @"var textarea = document.querySelector('textarea.chat-input-enhanced'); 
                  if(textarea) { 
                      textarea.value = ''; 
                      textarea.defaultValue = '';
                      textarea.dispatchEvent(new Event('input', { bubbles: true }));
                  }");
        }
        catch
        {
            // 忽略错误，使用降级方案
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", 
                    "var textareas = document.querySelectorAll('textarea'); textareas.forEach(t => { if(t.value === 'CurrentMessage') t.value = ''; });");
            }
            catch
            {
                // 完全降级，忽略
            }
        }
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
                await OnSendMessage.InvokeAsync(CurrentValue.Trim());
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
    /// 自动调整输入框高度
    /// </summary>
    private async Task AutoResizeTextarea()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.autoResizeTextarea", textareaRef);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Auto resize failed: {ex.Message}");
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
