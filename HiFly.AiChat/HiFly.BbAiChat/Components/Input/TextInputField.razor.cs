// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.BbAiChat.Components.Input;

/// <summary>
/// 文本输入字段组件
/// </summary>
public partial class TextInputField : ComponentBase, IDisposable
{
    [Inject]
    [NotNull]
    private IJSRuntime? JSRuntime { get; set; }

    /// <summary>
    /// 当前值
    /// </summary>
    [Parameter]
    public string CurrentValue { get; set; } = string.Empty;

    /// <summary>
    /// 当前值变更事件
    /// </summary>
    [Parameter]
    public EventCallback<string> CurrentValueChanged { get; set; }

    /// <summary>
    /// 占位符
    /// </summary>
    [Parameter]
    public string Placeholder { get; set; } = "请输入消息...";

    /// <summary>
    /// 最大字符长度
    /// </summary>
    [Parameter]
    public int MaxLength { get; set; } = 2000;

    /// <summary>
    /// 是否禁用
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

    /// <summary>
    /// 实时状态更新事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnRealTimeUpdate { get; set; }

    private ElementReference textareaRef;
    private Timer? _updateTimer;

    protected override async Task OnInitializedAsync()
    {
        // 确保CurrentValue为空字符串，清除任何可能的测试数据
        if (string.IsNullOrEmpty(CurrentValue) || CurrentValue == "CurrentMessage")
        {
            CurrentValue = string.Empty;
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // 首次渲染时强制清空任何残留值
                await ClearTextareaValue();
                await InitializeTextarea();

                // 首次渲染后，触发一次实时更新以同步当前状态
                if (OnRealTimeUpdate.HasDelegate)
                {
                    await OnRealTimeUpdate.InvokeAsync(CurrentValue);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响功能
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void OnParametersSet()
    {
        // 过滤测试数据
        if (CurrentValue == "CurrentMessage")
        {
            CurrentValue = string.Empty;
            if (CurrentValueChanged.HasDelegate)
            {
                // 使用Task.Run避免在预渲染期间的同步上下文问题
                _ = Task.Run(async () => await CurrentValueChanged.InvokeAsync(CurrentValue));
            }
        }

        base.OnParametersSet();
    }

    /// <summary>
    /// 启动实时更新监听
    /// </summary>
    private async Task StartRealTimeUpdates()
    {
        try
        {
            // 等待DOM完全加载
            await Task.Delay(200);

            await JSRuntime.InvokeVoidAsync("eval", @"
                (function() {
                    console.log('🔄 开始启动实时更新监听器...');
                    
                    // 检查DOM是否准备就绪
                    if (document.readyState !== 'complete') {
                        console.log('⚠️ DOM未完全加载，延迟启动');
                        setTimeout(arguments.callee, 100);
                        return;
                    }
                    
                    const textarea = document.querySelector('textarea.chat-input-enhanced');
                    if (!textarea) {
                        console.log('❌ 未找到textarea元素，1秒后重试');
                        setTimeout(arguments.callee, 1000);
                        return;
                    }
                    
                    console.log('✅ 找到textarea元素:', textarea);
                    
                    // 安全地移除现有监听器
                    try {
                        if (textarea._realtimeHandler) {
                            ['input', 'keyup', 'paste', 'cut'].forEach(eventType => {
                                textarea.removeEventListener(eventType, textarea._realtimeHandler);
                            });
                            console.log('🧹 已清理旧的监听器');
                        }
                    } catch (e) {
                        console.log('清理旧监听器时出现错误:', e);
                    }
                    
                    // 创建新的实时更新处理器
                    const realtimeHandler = function(e) {
                        try {
                            console.log('📝 输入事件触发:', e.type, 'value:', e.target.value);
                            
                            // 触发自定义事件
                            const updateEvent = new CustomEvent('blazorRealTimeUpdate', {
                                detail: { 
                                    value: e.target.value,
                                    length: e.target.value.length,
                                    eventType: e.type,
                                    timestamp: Date.now()
                                },
                                bubbles: true
                            });
                            
                            document.dispatchEvent(updateEvent);
                            console.log('✅ 实时更新事件已触发');
                            
                        } catch (error) {
                            console.error('❌ 处理输入事件时出错:', error);
                        }
                    };
                    
                    // 保存处理器引用
                    textarea._realtimeHandler = realtimeHandler;
                    
                    // 添加事件监听器
                    try {
                        ['input', 'keyup', 'paste', 'cut'].forEach(eventType => {
                            textarea.addEventListener(eventType, realtimeHandler, { passive: true });
                        });
                        
                        console.log('✅ 实时更新监听器启动成功');
                        
                        // 立即触发一次更新以同步当前状态
                        if (textarea.value) {
                            realtimeHandler({ target: textarea, type: 'init' });
                        }
                        
                    } catch (error) {
                        console.error('❌ 添加事件监听器失败:', error);
                    }
                    
                })();
            ");

            System.Console.WriteLine("✅ 实时更新监听器初始化完成");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ 启动实时更新失败: {ex.Message}");
            System.Console.WriteLine($"详细错误: {ex.StackTrace}");

            // 提供降级方案
            await StartFallbackUpdate();
        }
    }

    /// <summary>
    /// 降级方案：使用定时器进行状态更新
    /// </summary>
    private async Task StartFallbackUpdate()
    {
        try
        {
            System.Console.WriteLine("🔄 启动降级更新方案...");

            // 每500ms检查一次输入框值的变化，延迟1秒启动以避免预渲染问题
            _updateTimer?.Dispose();
            _updateTimer = new Timer(async _ =>
            {
                try
                {
                    await InvokeAsync(async () =>
                    {
                        try
                        {
                            // 检查是否可以安全调用JavaScript
                            var currentValue = await JSRuntime.InvokeAsync<string>("eval",
                                "document.querySelector('textarea.chat-input-enhanced')?.value || ''");

                            if (currentValue != CurrentValue)
                            {
                                CurrentValue = currentValue;
                                if (OnRealTimeUpdate.HasDelegate)
                                {
                                    await OnRealTimeUpdate.InvokeAsync(CurrentValue);
                                }
                                if (CurrentValueChanged.HasDelegate)
                                {
                                    await CurrentValueChanged.InvokeAsync(CurrentValue);
                                }
                                StateHasChanged();
                            }
                        }
                        catch (JSException)
                        {
                            // JavaScript调用失败，可能是因为预渲染，忽略
                        }
                        catch (InvalidOperationException)
                        {
                            // 预渲染期间的无效操作，忽略
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"降级更新失败: {ex.Message}");
                }
            }, null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(500));

            System.Console.WriteLine("✅ 降级更新方案启动成功");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ 降级更新方案启动失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 启动防抖更新
    /// </summary>
    private void StartDebounceUpdate()
    {
        // 取消之前的定时器
        _updateTimer?.Dispose();

        // 启动新的定时器（100ms防抖）
        _updateTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                if (OnRealTimeUpdate.HasDelegate)
                {
                    await OnRealTimeUpdate.InvokeAsync(CurrentValue);
                }
                StateHasChanged();
            });
        }, null, 100, Timeout.Infinite);
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
            // 记录错误但不影响功能
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
            // 记录错误但不影响功能
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
            // 记录错误但不影响功能
        }
    }

    /// <summary>
    /// 清空内容
    /// </summary>
    public async Task ClearContent()
    {
        // 1. 清空当前值
        CurrentValue = string.Empty;

        // 2. 立即强制清空DOM中的textarea
        try
        {
            await JSRuntime.InvokeVoidAsync("eval", @"
                const textarea = document.querySelector('textarea.chat-input-enhanced');
                if (textarea) {
                    textarea.value = '';
                    textarea.defaultValue = '';
                    
                    // 立即触发input事件确保所有监听器知道值已清空
                    textarea.dispatchEvent(new Event('input', { bubbles: true }));
                    textarea.dispatchEvent(new Event('change', { bubbles: true }));
                }
            ");
        }
        catch (Exception ex)
        {
            // 记录错误但不影响功能
        }

        // 3. 重置高度
        await ResetHeight();

        // 4. 通知父组件
        if (CurrentValueChanged.HasDelegate)
        {
            await CurrentValueChanged.InvokeAsync(CurrentValue);
        }

        // 5. 强制立即触发实时更新事件
        if (OnRealTimeUpdate.HasDelegate)
        {
            await OnRealTimeUpdate.InvokeAsync(CurrentValue);
        }

        // 6. 强制UI更新
        StateHasChanged();
    }

    /// <summary>
    /// 强制清空textarea的值
    /// </summary>
    private async Task ClearTextareaValue()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("eval", @"
                const textarea = document.querySelector('textarea.chat-input-enhanced');
                if (textarea && (textarea.value === 'CurrentMessage' || textarea.defaultValue === 'CurrentMessage')) {
                    textarea.value = '';
                    textarea.defaultValue = '';
                }
            ");
        }
        catch (Exception ex)
        {
            // 记录错误但不影响功能
        }
    }

    /// <summary>
    /// 处理键盘按下
    /// </summary>
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            // Enter键发送消息
            if (!string.IsNullOrWhiteSpace(CurrentValue) && OnSendMessage.HasDelegate)
            {
                await OnSendMessage.InvokeAsync(CurrentValue.Trim());
            }
        }

        if (OnKeyDown.HasDelegate)
        {
            await OnKeyDown.InvokeAsync(e);
        }
    }

    /// <summary>
    /// 处理文本选择开始
    /// </summary>
    private async Task HandleTextSelectionStart(MouseEventArgs mouseArgs)
    {
        if (mouseArgs.Button != 0) return;
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理文本选择结束
    /// </summary>
    private async Task HandleTextSelectionEnd(MouseEventArgs mouseArgs)
    {
        if (mouseArgs.Button != 0) return;
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理双击选择单词
    /// </summary>
    private async Task HandleTextDoubleClick(MouseEventArgs mouseArgs)
    {
        if (mouseArgs.Button != 0) return;

        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.selectWordAtCursor", textareaRef);
        }
        catch (Exception ex)
        {
            // 记录错误但不影响功能
        }
    }

    /// <summary>
    /// 处理输入事件 - 实时更新触发器
    /// </summary>
    private async Task HandleInput(ChangeEventArgs e)
    {
        var newValue = e.Value?.ToString() ?? string.Empty;

        // 防止无意义的更新
        if (CurrentValue == newValue)
        {
            return;
        }

        // 过滤测试数据
        if (newValue == "CurrentMessage")
        {
            newValue = string.Empty;
        }

        // 更新当前值
        CurrentValue = newValue;

        // 立即通知父组件值变更
        if (CurrentValueChanged.HasDelegate)
        {
            await CurrentValueChanged.InvokeAsync(CurrentValue);
        }

        // 立即触发实时更新
        if (OnRealTimeUpdate.HasDelegate)
        {
            await OnRealTimeUpdate.InvokeAsync(CurrentValue);
        }

        // 强制UI更新
        StateHasChanged();
    }

    public void Dispose()
    {
        _updateTimer?.Dispose();
    }
}
