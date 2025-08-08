using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Settings;

/// <summary>
/// 设置区域容器组件
/// </summary>
public partial class SettingsSection : ComponentBase
{
    /// <summary>
    /// 区域标题
    /// </summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 图标CSS类
    /// </summary>
    [Parameter]
    public string IconClass { get; set; } = "fas fa-cog";

    /// <summary>
    /// 子内容
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// 是否折叠
    /// </summary>
    [Parameter]
    public bool IsCollapsed { get; set; } = false;

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
        var classes = new List<string> { "settings-section" };
        
        if (IsCollapsed)
            classes.Add("collapsed");
            
        if (!string.IsNullOrEmpty(CssClass))
            classes.Add(CssClass);
            
        return string.Join(" ", classes);
    }
}

