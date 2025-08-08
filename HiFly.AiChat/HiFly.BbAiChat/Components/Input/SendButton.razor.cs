using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HiFly.BbAiChat.Components.Input;

/// <summary>
/// 发送按钮组件
/// </summary>
public partial class SendButton : ComponentBase
{
    /// <summary>
    /// 是否正在加载
    /// </summary>
    [Parameter]
    public bool IsLoading { get; set; }

    /// <summary>
    /// 是否禁用按钮
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 是否可以发送（有内容）
    /// </summary>
    [Parameter]
    public bool CanSend { get; set; }

    /// <summary>
    /// 按钮标题
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "发送消息 (Enter)";

    /// <summary>
    /// 点击事件
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// 右键菜单事件
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnContextMenu { get; set; }

    /// <summary>
    /// 获取按钮的CSS类名
    /// </summary>
    private string CssClass
    {
        get
        {
            var classes = new List<string>();
            
            if (IsLoading)
                classes.Add("loading");
            else if (CanSend && !IsDisabled)
                classes.Add("active");
            else
                classes.Add("disabled");
                
            return string.Join(" ", classes);
        }
    }

    /// <summary>
    /// 处理点击事件
    /// </summary>
    private async Task HandleClick(MouseEventArgs mouseArgs)
    {
        // 只处理左键点击
        if (mouseArgs.Button != 0) return;

        // 如果禁用或无法发送，不处理
        if (IsDisabled || !CanSend || IsLoading) return;

        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(mouseArgs);
        }
    }

    /// <summary>
    /// 处理右键菜单事件
    /// </summary>
    private async Task HandleContextMenu(MouseEventArgs mouseArgs)
    {
        if (OnContextMenu.HasDelegate)
        {
            await OnContextMenu.InvokeAsync(mouseArgs);
        }
    }
}
