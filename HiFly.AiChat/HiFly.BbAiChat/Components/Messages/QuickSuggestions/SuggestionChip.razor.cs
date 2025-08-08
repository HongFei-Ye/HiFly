// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

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