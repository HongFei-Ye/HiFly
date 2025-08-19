// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Messages.MessageActions;

/// <summary>
/// 消息操作栏组件
/// </summary>
public partial class MessageActionBar : ComponentBase
{
    /// <summary>
    /// 消息内容
    /// </summary>
    [Parameter, EditorRequired]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消息对象
    /// </summary>
    [Parameter, EditorRequired]
    public ChatMessage Message { get; set; } = null!;

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

    private async Task HandleCopy()
    {
        if (OnCopyMessage.HasDelegate)
        {
            await OnCopyMessage.InvokeAsync(Content);
        }
    }

    private async Task HandleRegenerate()
    {
        if (OnRegenerateResponse.HasDelegate)
        {
            await OnRegenerateResponse.InvokeAsync(Message);
        }
    }

    private async Task HandleLike()
    {
        if (OnLikeMessage.HasDelegate)
        {
            await OnLikeMessage.InvokeAsync(Message);
        }
    }
}