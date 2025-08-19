// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Sidebar;
public partial class SidebarToolbar
{
    /// <summary>
    /// 是否折叠状态
    /// </summary>
    [Parameter]
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// 当前侧边栏宽度（用于动态文字切换）
    /// </summary>
    [Parameter]
    public string CurrentWidth { get; set; } = "320px";

    /// <summary>
    /// 新建对话事件
    /// </summary>
    [Parameter]
    public EventCallback OnNewChat { get; set; }

    /// <summary>
    /// 折叠切换事件
    /// </summary>
    [Parameter]
    public EventCallback OnToggleCollapse { get; set; }

    /// <summary>
    /// 子内容（用于插入宽度控制组件）
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// 新建对话按钮模板（可选，用于自定义按钮样式）
    /// </summary>
    [Parameter]
    public RenderFragment? NewChatButtonTemplate { get; set; }

    /// <summary>
    /// 是否显示切换按钮（极少情况下可能需要隐藏）
    /// </summary>
    [Parameter]
    public bool ShowToggleButton { get; set; } = true;

    /// <summary>
    /// 切换按钮的展开图标（可选自定义）
    /// </summary>
    [Parameter]
    public string ExpandedIcon { get; set; } = "fas fa-angle-left";

    /// <summary>
    /// 切换按钮的折叠图标（可选自定义）
    /// </summary>
    [Parameter]
    public string CollapsedIcon { get; set; } = "fas fa-angle-right";

    /// <summary>
    /// 是否启用动态文字切换（仅基于侧边栏宽度，不考虑浏览器宽度）
    /// </summary>
    [Parameter]
    public bool EnableDynamicText { get; set; } = true;

    private async Task HandleNewChat()
    {
        if (OnNewChat.HasDelegate)
        {
            await OnNewChat.InvokeAsync();
        }
    }

    private async Task HandleToggleCollapse()
    {
        if (OnToggleCollapse.HasDelegate)
        {
            await OnToggleCollapse.InvokeAsync();
        }
    }

    /// <summary>
    /// 获取当前应该显示的切换图标
    /// </summary>
    private string GetToggleIcon() => IsCollapsed ? CollapsedIcon : ExpandedIcon;

    /// <summary>
    /// 根据侧边栏宽度动态获取新建对话按钮文字（不考虑浏览器宽度）
    /// </summary>
    private string GetDynamicButtonText()
    {
        if (!EnableDynamicText)
            return "新建对话";

        // 仅根据侧边栏宽度决定，保持一致性
        var widthValue = ExtractWidthValue(CurrentWidth);
        return widthValue switch
        {
            <= 220 => "", // 极小宽度：仅图标
            <= 280 => "新建", // 紧凑宽度：简短文字
            _ => "新建对话" // 标准宽度：完整文字
        };
    }

    /// <summary>
    /// 判断是否应该显示按钮文字
    /// </summary>
    private bool ShouldShowButtonText()
    {
        if (!EnableDynamicText)
            return true;

        var widthValue = ExtractWidthValue(CurrentWidth);
        return widthValue > 220; // 侧边栏宽度大于220px时显示文字
    }

    /// <summary>
    /// 获取按钮的CSS类（包含动态类）
    /// </summary>
    private string GetButtonClass()
    {
        var baseClass = "btn-new-chat";
        if (!EnableDynamicText)
            return baseClass;

        var widthValue = ExtractWidthValue(CurrentWidth);
        return widthValue switch
        {
            <= 220 => $"{baseClass} icon-only dynamic-icon-only",
            <= 280 => $"{baseClass} compact dynamic-compact",
            _ => $"{baseClass} dynamic-full"
        };
    }

    /// <summary>
    /// 从宽度字符串中提取数值
    /// </summary>
    /// <param name="width">宽度字符串，如 "280px"</param>
    /// <returns>宽度数值</returns>
    private int ExtractWidthValue(string width)
    {
        if (string.IsNullOrEmpty(width))
            return 320; // 默认宽度

        // 移除非数字字符，提取数值
        var numericPart = System.Text.RegularExpressions.Regex.Replace(width, @"[^\d]", "");

        if (int.TryParse(numericPart, out var result))
            return result;

        return 320; // 解析失败时的默认值
    }

    /// <summary>
    /// 获取动态间距CSS变量（统一间距，不区分设备）
    /// </summary>
    private string GetDynamicSpacingStyle()
    {
        if (!EnableDynamicText)
            return "";

        var widthValue = ExtractWidthValue(CurrentWidth);
        var gap = widthValue switch
        {
            <= 220 => "0.25rem",
            <= 280 => "0.5rem",
            _ => "0.75rem"
        };

        return $"--dynamic-gap: {gap};";
    }

    /// <summary>
    /// 获取工具栏的动态CSS类（统一布局）
    /// </summary>
    private string GetToolbarClass()
    {
        var baseClass = "left-panel-toolbar";
        if (!EnableDynamicText)
            return baseClass;

        var widthValue = ExtractWidthValue(CurrentWidth);
        var dynamicClass = widthValue switch
        {
            <= 220 => "ultra-compact",
            <= 240 => "very-compact",
            <= 280 => "compact",
            <= 320 => "standard",
            _ => "wide"
        };

        return $"{baseClass} dynamic-layout {dynamicClass}";
    }
}
