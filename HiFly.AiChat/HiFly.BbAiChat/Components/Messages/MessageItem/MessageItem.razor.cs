// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Messages.MessageItem;

/// <summary>
/// 单条消息组件
/// </summary>
public partial class MessageItem : ComponentBase
{
    /// <summary>
    /// 消息对象
    /// </summary>
    [Parameter, EditorRequired]
    public ChatMessage Message { get; set; } = null!;

    /// <summary>
    /// 是否处于加载状态
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

    private string FormatAiMessage(string content)
    {
        // 简单的Markdown格式化
        content = content.Replace("\n", "<br/>");

        // 代码块处理
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"```(\w+)?\n?(.*?)```",
            "<pre><code>$2</code></pre>",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        // 行内代码处理
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"`([^`]+)`",
            "<code>$1</code>");

        // 粗体处理
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\*\*([^*]+)\*\*",
            "<strong>$1</strong>");

        // 斜体处理
        content = System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\*([^*]+)\*",
            "<em>$1</em>");

        return content;
    }
}