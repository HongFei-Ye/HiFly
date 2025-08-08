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
public partial class ChatMessagesArea : ComponentBase
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Messages.Any())
        {
            await ScrollToBottom();
        }
        await base.OnAfterRenderAsync(firstRender);
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
}