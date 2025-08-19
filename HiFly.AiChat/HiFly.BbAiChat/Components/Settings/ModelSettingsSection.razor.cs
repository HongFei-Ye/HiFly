// Copyright (c) HiFly. All rights reserved.
// 官方网站: www.hongfei8.net
// 联系方式: hongfei8@outlook.com

using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Settings;

/// <summary>
/// 模型设置区域组件
/// </summary>
public partial class ModelSettingsSection : ComponentBase
{
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

    /// <summary>
    /// 处理模型变更
    /// </summary>
    private async Task HandleModelChanged(ChangeEventArgs e)
    {
        var modelId = e.Value?.ToString() ?? string.Empty;
        SelectedModel = modelId;

        if (SelectedModelChanged.HasDelegate)
        {
            await SelectedModelChanged.InvokeAsync(modelId);
        }
    }

    /// <summary>
    /// 处理温度输入（实时更新显示值）
    /// </summary>
    private async Task HandleTemperatureInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out double temperature))
        {
            Temperature = Math.Round(temperature, 1); // 保留一位小数
            StateHasChanged(); // 更新显示的数值
        }
    }

    /// <summary>
    /// 处理温度变更（最终确认）
    /// </summary>
    private async Task HandleTemperatureChanged(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out double temperature))
        {
            Temperature = Math.Round(temperature, 1);

            if (TemperatureChanged.HasDelegate)
            {
                await TemperatureChanged.InvokeAsync(Temperature);
            }
        }
    }

    /// <summary>
    /// 处理最大令牌数变更
    /// </summary>
    private async Task HandleMaxTokensChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int tokens))
        {
            // 确保值在有效范围内
            tokens = Math.Max(100, Math.Min(4000, tokens));
            MaxTokens = tokens;

            if (MaxTokensChanged.HasDelegate)
            {
                await MaxTokensChanged.InvokeAsync(tokens);
            }
        }
    }
}
