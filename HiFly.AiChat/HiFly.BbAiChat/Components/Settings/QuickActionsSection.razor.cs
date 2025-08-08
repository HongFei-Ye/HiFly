using Microsoft.AspNetCore.Components;

namespace HiFly.BbAiChat.Components.Settings;

/// <summary>
/// 快捷操作区域组件
/// </summary>
public partial class QuickActionsSection : ComponentBase
{
    #region 参数

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

    /// <summary>
    /// 额外的操作按钮
    /// </summary>
    [Parameter]
    public RenderFragment? AdditionalActions { get; set; }

    /// <summary>
    /// 是否禁用所有操作
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; } = false;

    #endregion

    #region 私有字段

    /// <summary>
    /// 按钮状态字典
    /// </summary>
    private readonly Dictionary<string, ButtonState> _buttonStates = new()
    {
        { "export", ButtonState.Normal },
        { "share", ButtonState.Normal },
        { "theme", ButtonState.Normal },
        { "reset", ButtonState.Normal }
    };

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取按钮状态CSS类
    /// </summary>
    /// <param name="buttonKey">按钮键</param>
    /// <returns>CSS类名</returns>
    private string GetButtonStateClass(string buttonKey)
    {
        if (_buttonStates.TryGetValue(buttonKey, out var state))
        {
            return state switch
            {
                ButtonState.Loading => "loading",
                ButtonState.Success => "success",
                ButtonState.Error => "error",
                _ => ""
            };
        }
        return "";
    }

    /// <summary>
    /// 设置按钮状态
    /// </summary>
    /// <param name="buttonKey">按钮键</param>
    /// <param name="state">状态</param>
    private void SetButtonState(string buttonKey, ButtonState state)
    {
        if (_buttonStates.ContainsKey(buttonKey))
        {
            _buttonStates[buttonKey] = state;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 重置按钮状态
    /// </summary>
    /// <param name="buttonKey">按钮键</param>
    /// <param name="delay">延迟毫秒</param>
    private async Task ResetButtonStateAsync(string buttonKey, int delay = 2000)
    {
        await Task.Delay(delay);
        SetButtonState(buttonKey, ButtonState.Normal);
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理导出点击
    /// </summary>
    private async Task HandleExportClick()
    {
        if (IsDisabled || _buttonStates["export"] == ButtonState.Loading) return;

        try
        {
            SetButtonState("export", ButtonState.Loading);
            
            if (OnExportChat.HasDelegate)
            {
                await OnExportChat.InvokeAsync();
            }
            
            SetButtonState("export", ButtonState.Success);
            _ = ResetButtonStateAsync("export");
        }
        catch (Exception)
        {
            SetButtonState("export", ButtonState.Error);
            _ = ResetButtonStateAsync("export");
        }
    }

    /// <summary>
    /// 处理分享点击
    /// </summary>
    private async Task HandleShareClick()
    {
        if (IsDisabled || _buttonStates["share"] == ButtonState.Loading) return;

        try
        {
            SetButtonState("share", ButtonState.Loading);
            
            if (OnShareChat.HasDelegate)
            {
                await OnShareChat.InvokeAsync();
            }
            
            SetButtonState("share", ButtonState.Success);
            _ = ResetButtonStateAsync("share");
        }
        catch (Exception)
        {
            SetButtonState("share", ButtonState.Error);
            _ = ResetButtonStateAsync("share");
        }
    }

    /// <summary>
    /// 处理主题切换点击
    /// </summary>
    private async Task HandleThemeClick()
    {
        if (IsDisabled || _buttonStates["theme"] == ButtonState.Loading) return;

        try
        {
            SetButtonState("theme", ButtonState.Loading);
            
            if (OnToggleTheme.HasDelegate)
            {
                await OnToggleTheme.InvokeAsync();
            }
            
            SetButtonState("theme", ButtonState.Success);
            _ = ResetButtonStateAsync("theme", 1000); // 主题切换反馈更快
        }
        catch (Exception)
        {
            SetButtonState("theme", ButtonState.Error);
            _ = ResetButtonStateAsync("theme");
        }
    }

    /// <summary>
    /// 处理重置设置点击
    /// </summary>
    private async Task HandleResetClick()
    {
        if (IsDisabled || _buttonStates["reset"] == ButtonState.Loading) return;

        try
        {
            SetButtonState("reset", ButtonState.Loading);
            
            if (OnResetSettings.HasDelegate)
            {
                await OnResetSettings.InvokeAsync();
            }
            
            SetButtonState("reset", ButtonState.Success);
            _ = ResetButtonStateAsync("reset");
        }
        catch (Exception)
        {
            SetButtonState("reset", ButtonState.Error);
            _ = ResetButtonStateAsync("reset");
        }
    }

    #endregion
}

/// <summary>
/// 按钮状态枚举
/// </summary>
public enum ButtonState
{
    /// <summary>
    /// 正常状态
    /// </summary>
    Normal,
    
    /// <summary>
    /// 加载状态
    /// </summary>
    Loading,
    
    /// <summary>
    /// 成功状态
    /// </summary>
    Success,
    
    /// <summary>
    /// 错误状态
    /// </summary>
    Error
}
