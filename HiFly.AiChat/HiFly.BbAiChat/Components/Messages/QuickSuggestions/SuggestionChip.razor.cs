// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Messages.QuickSuggestions;

/// <summary>
/// 建议项组件
/// </summary>
public partial class SuggestionChip : ComponentBase
{
    /// <summary>
    /// 显示文本
    /// </summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 建议消息内容
    /// </summary>
    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 点击事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnClick { get; set; }

    private async Task HandleClick()
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(Message);
        }
    }
}