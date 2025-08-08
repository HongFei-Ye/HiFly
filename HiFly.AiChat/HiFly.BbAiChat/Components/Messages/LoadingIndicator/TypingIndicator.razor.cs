// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

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