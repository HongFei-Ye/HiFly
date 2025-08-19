// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Input;

/// <summary>
/// 快捷操作按钮组件
/// </summary>
public partial class QuickActionButtons : ComponentBase
{
    /// <summary>
    /// 是否禁用所有按钮
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 是否正在语音录制
    /// </summary>
    [Parameter]
    public bool IsVoiceRecording { get; set; }

    /// <summary>
    /// 切换语音事件
    /// </summary>
    [Parameter]
    public EventCallback OnToggleVoice { get; set; }

    /// <summary>
    /// 表情符号事件
    /// </summary>
    [Parameter]
    public EventCallback OnEmojiPicker { get; set; }

    /// <summary>
    /// 额外的操作按钮模板
    /// </summary>
    [Parameter]
    public RenderFragment? AdditionalActions { get; set; }
}
