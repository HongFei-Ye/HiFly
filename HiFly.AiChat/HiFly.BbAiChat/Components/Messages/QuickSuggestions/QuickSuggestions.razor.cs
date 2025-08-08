// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Messages.QuickSuggestions;

/// <summary>
/// 快捷建议组件
/// </summary>
public partial class QuickSuggestions : ComponentBase
{
    private List<CategoryData> _categories = GetDefaultCategories();

    /// <summary>
    /// 建议分类列表
    /// </summary>
    [Parameter]
    public List<CategoryData>? Categories
    {
        get => _categories;
        set => _categories = value ?? GetDefaultCategories();
    }

    /// <summary>
    /// 点击建议事件
    /// </summary>
    [Parameter]
    public EventCallback<string> OnSuggestionClick { get; set; }

    protected override void OnInitialized()
    {
        // 确保Categories永远不为null
        if (_categories == null)
        {
            _categories = GetDefaultCategories();
        }
        base.OnInitialized();
    }

    private async Task HandleSuggestionClick(string message)
    {
        if (OnSuggestionClick.HasDelegate)
        {
            await OnSuggestionClick.InvokeAsync(message);
        }
    }

    /// <summary>
    /// 获取默认分类数据
    /// </summary>
    private static List<CategoryData> GetDefaultCategories()
    {
        return new List<CategoryData>
        {
            new CategoryData
            {
                Title = "开始对话",
                IconClass = "fas fa-hand-wave",
                Suggestions = new List<SuggestionItem>
                {
                    new() { Text = "打个招呼", Message = "你好，请介绍一下自己" },
                    new() { Text = "了解功能", Message = "你能帮我做什么？" },
                    new() { Text = "随意聊聊", Message = "我想开始一个有趣的对话" }
                }
            },
            new CategoryData
            {
                Title = "学习助手",
                IconClass = "fas fa-brain",
                Suggestions = new List<SuggestionItem>
                {
                    new() { Text = "AI工作原理", Message = "解释一下人工智能的工作原理" },
                    new() { Text = "机器学习入门", Message = "什么是机器学习？" }
                }
            },
            new CategoryData
            {
                Title = "编程助手",
                IconClass = "fas fa-code",
                Suggestions = new List<SuggestionItem>
                {
                    new() { Text = "Python编程", Message = "帮我写一个Python函数" },
                    new() { Text = "代码解析", Message = "解释这段代码的作用" }
                }
            }
        };
    }
}

/// <summary>
/// 分类数据模型
/// </summary>
public class CategoryData
{
    public string Title { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public List<SuggestionItem> Suggestions { get; set; } = new();
}