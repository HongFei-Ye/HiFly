using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HiFly.BbAiChat.Components.Input;

/// <summary>
/// 输入统计显示组件
/// </summary>
public partial class InputStatsDisplay : ComponentBase
{
    /// <summary>
    /// 当前字符长度
    /// </summary>
    [Parameter]
    public int CurrentLength { get; set; }

    /// <summary>
    /// 最大字符长度
    /// </summary>
    [Parameter]
    public int MaxLength { get; set; } = 2000;

    /// <summary>
    /// 是否显示进度条
    /// </summary>
    [Parameter]
    public bool ShowProgress { get; set; } = false;

    /// <summary>
    /// 点击事件
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// 获取进度百分比
    /// </summary>
    private double ProgressPercentage => MaxLength > 0 ? (double)CurrentLength / MaxLength * 100 : 0;

    /// <summary>
    /// 获取提示文本
    /// </summary>
    private string GetTooltipText()
    {
        var remaining = MaxLength - CurrentLength;
        if (remaining < 0)
        {
            return $"超出限制 {Math.Abs(remaining)} 个字符";
        }
        else if (remaining < 100)
        {
            return $"还可输入 {remaining} 个字符";
        }
        else
        {
            return $"已输入 {CurrentLength} 个字符，限制 {MaxLength} 个字符";
        }
    }

    /// <summary>
    /// 获取警告级别的CSS类
    /// </summary>
    private string GetWarningClass()
    {
        var percentage = ProgressPercentage;
        if (percentage >= 100)
            return "error";
        else if (percentage >= 90)
            return "warning";
        else if (percentage >= 75)
            return "caution";
        else
            return "normal";
    }
}
