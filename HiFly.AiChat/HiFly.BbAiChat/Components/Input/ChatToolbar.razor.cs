using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HiFly.BbAiChat.Components.Input;

/// <summary>
/// 聊天工具栏组件
/// </summary>
public partial class ChatToolbar : ComponentBase
{
    /// <summary>
    /// 自定义工具栏模板
    /// </summary>
    [Parameter]
    public RenderFragment? ToolbarTemplate { get; set; }

    /// <summary>
    /// 是否禁用所有按钮
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 输入提示文本
    /// </summary>
    [Parameter]
    public string InputHint { get; set; } = "使用 Enter 发送消息，Shift+Enter 换行";

    /// <summary>
    /// 附加文件事件
    /// </summary>
    [Parameter]
    public EventCallback OnAttachFile { get; set; }

    /// <summary>
    /// 插入模板事件
    /// </summary>
    [Parameter]
    public EventCallback OnInsertTemplate { get; set; }

    /// <summary>
    /// 深度思考事件
    /// </summary>
    [Parameter]
    public EventCallback OnDeepThink { get; set; }

    /// <summary>
    /// 联网搜索事件
    /// </summary>
    [Parameter]
    public EventCallback OnWebSearch { get; set; }
}
