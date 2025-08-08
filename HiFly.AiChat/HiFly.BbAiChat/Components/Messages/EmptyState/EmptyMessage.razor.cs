// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using HiFly.BbAiChat.Components.Messages.QuickSuggestions;
using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Messages.EmptyState;

/// <summary>
/// 空消息状态组件 - 现代化设计
/// </summary>
public partial class EmptyMessage : ComponentBase
{
    private List<CategoryData>? _suggestionCategories;

    /// <summary>
    /// 标题
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "开始新的对话";

    /// <summary>
    /// 描述文本
    /// </summary>
    [Parameter]
    public string Description { get; set; } = "我是您的AI助手，准备为您提供帮助。让我们开始一段智能对话吧！";

    /// <summary>
    /// 是否显示建议
    /// </summary>
    [Parameter]
    public bool ShowSuggestions { get; set; } = true;

    /// <summary>
    /// 自定义建议分类 - 如果为null，将使用默认分类
    /// </summary>
    [Parameter]
    public List<CategoryData>? SuggestionCategories
    {
        get => _suggestionCategories ?? GetEnhancedDefaultCategories();
        set => _suggestionCategories = value;
    }

    /// <summary>
    /// 点击建议事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSuggestionClick { get; set; }

    /// <summary>
    /// 是否启用动画效果
    /// </summary>
    [Parameter]
    public bool EnableAnimations { get; set; } = true;

    /// <summary>
    /// 自定义CSS类
    /// </summary>
    [Parameter]
    public string? CssClass { get; set; }

    /// <summary>
    /// 处理建议点击事件
    /// </summary>
    /// <param name="message">建议消息</param>
    private async Task HandleSuggestionClick(string message)
    {
        if (OnSuggestionClick.HasDelegate)
        {
            await OnSuggestionClick.InvokeAsync(message);
        }
    }

    /// <summary>
    /// 获取增强的默认分类数据
    /// </summary>
    private static List<CategoryData> GetEnhancedDefaultCategories()
    {
        return new List<CategoryData>
        {
            new CategoryData
            {
                Title = "开始对话",
                IconClass = "fas fa-comments",
                Suggestions = new List<SuggestionItem>
                {
                    new() { Text = "友好问候", Message = "你好！很高兴见到你，请介绍一下自己吧" },
                    new() { Text = "了解功能", Message = "你能帮我做什么？有哪些特色功能？" },
                    new() { Text = "随意聊聊", Message = "我想开始一个轻松有趣的对话" },
                    new() { Text = "获得帮助", Message = "我需要一些帮助，你能指导我吗？" }
                }
            },
            new CategoryData
            {
                Title = "学习助手",
                IconClass = "fas fa-graduation-cap",
                Suggestions = new List<SuggestionItem>
                {
                    new() { Text = "AI基础知识", Message = "请解释一下人工智能的基本工作原理" },
                    new() { Text = "机器学习入门", Message = "什么是机器学习？它是如何工作的？" },
                    new() { Text = "技术趋势", Message = "当前AI技术的发展趋势是什么？" },
                    new() { Text = "学习计划", Message = "帮我制定一个AI学习计划" }
                }
            },
            new CategoryData
            {
                Title = "编程助手",
                IconClass = "fas fa-code",
                Suggestions = new List<SuggestionItem>
                {
                    new() { Text = "代码编写", Message = "帮我写一个实用的代码示例" },
                    new() { Text = "代码解析", Message = "请解释这段代码的功能和逻辑" },
                    new() { Text = "最佳实践", Message = "分享一些编程的最佳实践和技巧" },
                    new() { Text = "调试帮助", Message = "帮我分析和解决代码中的问题" }
                }
            }
        };
    }

    /// <summary>
    /// 组件初始化
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 确保有默认的建议分类
        if (_suggestionCategories == null)
        {
            _suggestionCategories = GetEnhancedDefaultCategories();
        }
    }

    /// <summary>
    /// 组件参数设置完成后执行
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // 如果禁用了动画，添加相应的CSS类
        if (!EnableAnimations)
        {
            CssClass = $"{CssClass} no-animations".Trim();
        }
    }

}
