// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Messages.LoadingIndicator;

/// <summary>
/// 打字指示器组件
/// </summary>
public partial class TypingIndicator : ComponentBase
{
    /// <summary>
    /// 提示文本
    /// </summary>
    [Parameter]
    public string Text { get; set; } = "AI正在思考中...";
}