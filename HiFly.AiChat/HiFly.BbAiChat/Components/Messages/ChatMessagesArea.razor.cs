// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.BbAiChat.Components.Messages;

/// <summary>
/// 消息区域组件
/// </summary>
public partial class ChatMessagesArea : ComponentBase, IDisposable
{
    [Inject]
    [NotNull]
    private IJSRuntime? JSRuntime { get; set; }

    /// <summary>
    /// 消息列表
    /// </summary>
    [Parameter]
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// 是否正在加载
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// 复制消息事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnCopyMessage { get; set; }

    /// <summary>
    /// 重新生成响应事件
    /// </summary>
    [Parameter]
    public EventCallback<ChatMessage> OnRegenerateResponse { get; set; }

    /// <summary>
    /// 点赞消息事件
    /// </summary>
    [Parameter]
    public EventCallback<ChatMessage> OnLikeMessage { get; set; }

    /// <summary>
    /// 设置快捷消息事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSetQuickMessage { get; set; }

    private ElementReference messagesContainer;
    private bool showScrollButton = false;
    private DotNetObjectReference<ChatMessagesArea>? dotNetRef;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // 只有在有消息时才滚动到底部
        if (Messages.Any())
        {
            await ScrollToBottom();
        }

        if (firstRender)
        {
            // 添加滚动事件监听器
            await SetupScrollListener();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    /// 设置滚动事件监听器
    /// </summary>
    private async Task SetupScrollListener()
    {
        try
        {
            dotNetRef = DotNetObjectReference.Create(this);
            
            await JSRuntime.InvokeVoidAsync("eval", @"
                const container = arguments[0];
                const dotnetRef = arguments[1];
                
                if (container && dotnetRef) {
                    // 添加滚动事件监听器
                    container.addEventListener('scroll', function() {
                        const isNearBottom = aiChatHelper.isNearBottom(container, 150);
                        dotnetRef.invokeMethodAsync('UpdateScrollButtonVisibility', !isNearBottom);
                    });
                    
                    // 添加键盘快捷键监听器 (Ctrl/Cmd + End 滚动到底部)
                    document.addEventListener('keydown', function(e) {
                        if ((e.ctrlKey || e.metaKey) && e.key === 'End') {
                            e.preventDefault();
                            aiChatHelper.scrollToBottom(container);
                            dotnetRef.invokeMethodAsync('UpdateScrollButtonVisibility', false);
                        }
                        // Home键滚动到顶部
                        else if ((e.ctrlKey || e.metaKey) && e.key === 'Home') {
                            e.preventDefault();
                            container.scrollTo({
                                top: 0,
                                behavior: 'smooth'
                            });
                        }
                    });
                }
            ", messagesContainer, dotNetRef);
        }
        catch
        {
            // 忽略JS调用失败
        }
    }

    /// <summary>
    /// 更新滚动按钮可见性
    /// </summary>
    [JSInvokable]
    public async Task UpdateScrollButtonVisibility(bool visible)
    {
        if (showScrollButton != visible)
        {
            showScrollButton = visible;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// 处理滚动到底部按钮点击
    /// </summary>
    private async Task HandleScrollToBottom()
    {
        await ScrollToBottom();
        showScrollButton = false;
        StateHasChanged();
    }

    /// <summary>
    /// 当消息列表参数发生变化时触发
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        // 当新消息添加时，自动滚动到底部
        await ScrollToBottomIfNeeded();
        await base.OnParametersSetAsync();
    }

    /// <summary>
    /// 智能滚动到底部 - 只有在接近底部时才自动滚动
    /// </summary>
    private async Task ScrollToBottomIfNeeded()
    {
        try
        {
            // 检查用户是否已经滚动到接近底部
            var isNearBottom = await JSRuntime.InvokeAsync<bool>("eval", @"
                (function() {
                    const container = arguments[0];
                    if (!container) return true;
                    
                    const scrollTop = container.scrollTop;
                    const scrollHeight = container.scrollHeight;
                    const clientHeight = container.clientHeight;
                    
                    // 如果距离底部小于100px，认为用户在底部附近
                    return (scrollHeight - scrollTop - clientHeight) < 100;
                })(arguments[0]);
            ", messagesContainer);

            // 只有在用户接近底部时才自动滚动
            if (isNearBottom)
            {
                await ScrollToBottom();
            }
        }
        catch
        {
            // 如果JavaScript调用失败，默认滚动到底部
            await ScrollToBottom();
        }
    }

    /// <summary>
    /// 强制滚动到底部
    /// </summary>
    public async Task ScrollToBottom()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("aiChatHelper.scrollToBottom", messagesContainer);
        }
        catch
        {
            // 降级方案：使用基础的scrollIntoView
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", @"
                    const container = arguments[0];
                    if (container) {
                        container.scrollTo({
                            top: container.scrollHeight,
                            behavior: 'smooth'
                        });
                    }
                ", messagesContainer);
            }
            catch
            {
                // 忽略所有JS调用失败
            }
        }
    }

    private async Task HandleCopyMessage(string content)
    {
        if (OnCopyMessage.HasDelegate)
        {
            await OnCopyMessage.InvokeAsync(content);
        }
    }

    private async Task HandleRegenerateResponse(ChatMessage message)
    {
        if (OnRegenerateResponse.HasDelegate)
        {
            await OnRegenerateResponse.InvokeAsync(message);
            // 重新生成后滚动到底部查看新内容
            await Task.Delay(100); // 等待重新渲染
            await ScrollToBottom();
        }
    }

    private async Task HandleLikeMessage(ChatMessage message)
    {
        if (OnLikeMessage.HasDelegate)
        {
            await OnLikeMessage.InvokeAsync(message);
        }
    }

    private async Task HandleSetQuickMessage(string message)
    {
        if (OnSetQuickMessage.HasDelegate)
        {
            await OnSetQuickMessage.InvokeAsync(message);
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

    /// <summary>
    /// 资源释放
    /// </summary>
    public void Dispose()
    {
        dotNetRef?.Dispose();
    }
}
