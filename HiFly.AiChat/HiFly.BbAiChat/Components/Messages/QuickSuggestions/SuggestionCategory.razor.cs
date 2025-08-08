// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Messages.QuickSuggestions;

/// <summary>
/// 建议分类组件
/// </summary>
public partial class SuggestionCategory : ComponentBase
{
    /// <summary>
    /// 分类标题
    /// </summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 图标CSS类
    /// </summary>
    [Parameter, EditorRequired]
    public string IconClass { get; set; } = string.Empty;

    /// <summary>
    /// 建议列表
    /// </summary>
    [Parameter, EditorRequired]
    public List<SuggestionItem> Suggestions { get; set; } = new();

    /// <summary>
    /// 点击建议事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSuggestionClick { get; set; }

    private async Task HandleSuggestionClick(string message)
    {
        if (OnSuggestionClick.HasDelegate)
        {
            await OnSuggestionClick.InvokeAsync(message);
        }
    }
}

/// <summary>
/// 建议项数据模型
/// </summary>
public class SuggestionItem
{
    public string Text { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}