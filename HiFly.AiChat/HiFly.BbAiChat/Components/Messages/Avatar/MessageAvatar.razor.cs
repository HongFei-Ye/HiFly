// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Messages.Avatar;

/// <summary>
/// 消息头像组件
/// </summary>
public partial class MessageAvatar : ComponentBase
{
    /// <summary>
    /// 是否为用户消息
    /// </summary>
    [Parameter]
    public bool IsUser { get; set; }

    /// <summary>
    /// 是否处于加载状态
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }
}