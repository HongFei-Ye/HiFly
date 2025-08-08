// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Settings;

/// <summary>
/// AI设置面板容器组件 - 重构后的版本
/// </summary>
public partial class AiSettingsPanel : ComponentBase
{
    #region 基础参数

    /// <summary>
    /// 面板宽度
    /// </summary>
    [Parameter]
    public string Width { get; set; } = "320px";

    #endregion

    #region 模型设置参数

    /// <summary>
    /// 可用模型列表
    /// </summary>
    [Parameter]
    public List<AiModel> AvailableModels { get; set; } = new();

    /// <summary>
    /// 选中的模型
    /// </summary>
    [Parameter]
    public string SelectedModel { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// 模型变更事件
    /// </summary>
    [Parameter]
    public EventCallback<string> SelectedModelChanged { get; set; }

    /// <summary>
    /// 温度设置
    /// </summary>
    [Parameter]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// 温度变更事件
    /// </summary>
    [Parameter]
    public EventCallback<double> TemperatureChanged { get; set; }

    /// <summary>
    /// 最大令牌数
    /// </summary>
    [Parameter]
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// 最大令牌数变更事件
    /// </summary>
    [Parameter]
    public EventCallback<int> MaxTokensChanged { get; set; }

    #endregion

    #region 高级设置参数

    /// <summary>
    /// 是否启用上下文记忆
    /// </summary>
    [Parameter]
    public bool EnableMemory { get; set; } = true;

    /// <summary>
    /// 上下文记忆变更事件
    /// </summary>
    [Parameter]
    public EventCallback<bool> EnableMemoryChanged { get; set; }

    /// <summary>
    /// 是否启用流式响应
    /// </summary>
    [Parameter]
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// 流式响应变更事件
    /// </summary>
    [Parameter]
    public EventCallback<bool> EnableStreamingChanged { get; set; }

    #endregion

    #region 模板参数

    /// <summary>
    /// 自定义设置面板模板
    /// </summary>
    [Parameter]
    public RenderFragment? SettingsTemplate { get; set; }

    /// <summary>
    /// 高级设置模板
    /// </summary>
    [Parameter]
    public RenderFragment? AdvancedSettingsTemplate { get; set; }

    /// <summary>
    /// 快捷操作模板
    /// </summary>
    [Parameter]
    public RenderFragment? QuickActionsTemplate { get; set; }

    /// <summary>
    /// 额外的设置区域
    /// </summary>
    [Parameter]
    public RenderFragment? AdditionalSections { get; set; }

    #endregion

    #region 事件参数

    /// <summary>
    /// 导出对话事件
    /// </summary>
    [Parameter]
    public EventCallback OnExportChat { get; set; }

    /// <summary>
    /// 分享对话事件
    /// </summary>
    [Parameter]
    public EventCallback OnShareChat { get; set; }

    /// <summary>
    /// 切换主题事件
    /// </summary>
    [Parameter]
    public EventCallback OnToggleTheme { get; set; }

    /// <summary>
    /// 重置设置事件
    /// </summary>
    [Parameter]
    public EventCallback OnResetSettings { get; set; }

    #endregion
}
