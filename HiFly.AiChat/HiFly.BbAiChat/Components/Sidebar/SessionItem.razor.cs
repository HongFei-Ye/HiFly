// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Sidebar;

/// <summary>
/// 会话历史项组件
/// </summary>
public partial class SessionItem : ComponentBase
{
    /// <summary>
    /// 会话信息
    /// </summary>
    [Parameter, EditorRequired]
    public ChatSession Session { get; set; } = null!;

    /// <summary>
    /// 是否为当前活动会话
    /// </summary>
    [Parameter]
    public bool IsActive { get; set; }

    /// <summary>
    /// 点击会话事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSessionClick { get; set; }

    /// <summary>
    /// 编辑会话事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnEditSession { get; set; }

    /// <summary>
    /// 删除会话事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDeleteSession { get; set; }

    private async Task HandleSessionClick()
    {
        if (OnSessionClick.HasDelegate)
        {
            await OnSessionClick.InvokeAsync(Session.Id);
        }
    }

    private async Task HandleEditSession()
    {
        if (OnEditSession.HasDelegate)
        {
            await OnEditSession.InvokeAsync(Session.Id);
        }
    }

    private async Task HandleDeleteSession()
    {
        if (OnDeleteSession.HasDelegate)
        {
            await OnDeleteSession.InvokeAsync(Session.Id);
        }
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "刚刚";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}分钟前";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}小时前";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}天前";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)}周前";

        return dateTime.ToString("MM-dd");
    }
}