using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Settings;

/// <summary>
/// 设置项组件
/// </summary>
public partial class SettingItem : ComponentBase
{
    /// <summary>
    /// 设置项标签
    /// </summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 图标CSS类
    /// </summary>
    [Parameter]
    public string IconClass { get; set; } = string.Empty;

    /// <summary>
    /// 徽章文本
    /// </summary>
    [Parameter]
    public string BadgeText { get; set; } = string.Empty;

    /// <summary>
    /// 值文本
    /// </summary>
    [Parameter]
    public string ValueText { get; set; } = string.Empty;

    /// <summary>
    /// 描述文本
    /// </summary>
    [Parameter]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 设置控件内容
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; } = false;

    /// <summary>
    /// 是否高亮
    /// </summary>
    [Parameter]
    public bool IsHighlighted { get; set; } = false;

    /// <summary>
    /// 额外的CSS类
    /// </summary>
    [Parameter]
    public string CssClass { get; set; } = string.Empty;

    /// <summary>
    /// 获取完整的CSS类
    /// </summary>
    private string GetCssClass()
    {
        var classes = new List<string>();
        
        if (IsDisabled)
            classes.Add("disabled");
            
        if (IsHighlighted)
            classes.Add("highlighted");
            
        if (!string.IsNullOrEmpty(CssClass))
            classes.Add(CssClass);
            
        return string.Join(" ", classes);
    }
}
