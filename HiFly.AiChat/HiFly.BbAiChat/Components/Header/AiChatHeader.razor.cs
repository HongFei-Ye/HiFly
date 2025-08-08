// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Header;

/// <summary>
/// AI聊天头部组件
/// </summary>
public partial class AiChatHeader : ComponentBase
{
    /// <summary>
    /// 标题
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "AI 智能对话";

    /// <summary>
    /// 子标题
    /// </summary>
    [Parameter]
    public string Subtitle { get; set; } = "智能对话助手";

    /// <summary>
    /// 自定义头部模板
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// 设置面板是否显示
    /// </summary>
    [Parameter]
    public bool ShowSettingsPanel { get; set; } = true;

    /// <summary>
    /// 切换设置事件
    /// </summary>
    [Parameter]
    public EventCallback OnToggleSettings { get; set; }

    /// <summary>
    /// 获取设置按钮的提示文本
    /// </summary>
    private string GetSettingsButtonTitle()
    {
        return ShowSettingsPanel ? "关闭设置面板" : "打开设置面板";
    }

    /// <summary>
    /// 获取设置按钮的图标
    /// </summary>
    private string GetSettingsButtonIcon()
    {
        return ShowSettingsPanel ? "fas fa-times" : "fas fa-cog";
    }

    /// <summary>
    /// 获取设置按钮的文本
    /// </summary>
    private string GetSettingsButtonText()
    {
        return ShowSettingsPanel ? "关闭" : "设置";
    }

    private async Task HandleToggleSettings()
    {
        if (OnToggleSettings.HasDelegate)
        {
            await OnToggleSettings.InvokeAsync();
        }
    }
}
