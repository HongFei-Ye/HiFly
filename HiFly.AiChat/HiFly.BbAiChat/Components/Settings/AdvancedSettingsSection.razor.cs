// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Settings;

/// <summary>
/// 高级设置区域组件
/// </summary>
public partial class AdvancedSettingsSection : ComponentBase
{
    /// <summary>
    /// 是否启用上下文记忆
    /// </summary>
    [Parameter]
    public bool EnableMemory { get; set; } = true;

    /// <summary>
    /// 上下文记忆变更事件
    /// </summary>
    [Parameter]
    public EventCallback<bool> EnableMemoryChanged { get; set; }

    /// <summary>
    /// 是否启用流式响应
    /// </summary>
    [Parameter]
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// 流式响应变更事件
    /// </summary>
    [Parameter]
    public EventCallback<bool> EnableStreamingChanged { get; set; }

    /// <summary>
    /// 额外的设置内容
    /// </summary>
    [Parameter]
    public RenderFragment? AdditionalSettings { get; set; }

    /// <summary>
    /// 处理记忆功能变更
    /// </summary>
    private async Task HandleMemoryChanged(ChangeEventArgs e)
    {
        var enabled = e.Value is bool boolValue ? boolValue : false;
        EnableMemory = enabled;

        if (EnableMemoryChanged.HasDelegate)
        {
            await EnableMemoryChanged.InvokeAsync(enabled);
        }
    }

    /// <summary>
    /// 处理流式响应变更
    /// </summary>
    private async Task HandleStreamingChanged(ChangeEventArgs e)
    {
        var enabled = e.Value is bool boolValue ? boolValue : false;
        EnableStreaming = enabled;

        if (EnableStreamingChanged.HasDelegate)
        {
            await EnableStreamingChanged.InvokeAsync(enabled);
        }
    }
}
