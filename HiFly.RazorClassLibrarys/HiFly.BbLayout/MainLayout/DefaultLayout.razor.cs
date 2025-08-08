// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace HiFly.BbLayout.MainLayout;

public partial class DefaultLayout
{
    private bool IsOpen { get; set; }

    /// <summary>
    /// OnInitialized 方法
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

    }

    [Inject]
    [NotNull]
    private IJSRuntime? JSRuntime { get; set; }

    [NotNull]
    private JSModule? Module { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            Module = await JSRuntime.LoadUtility();
        }
    }

    private async Task GoToReceptionHome()
    {
        await Module.OpenUrl("/", "_self");
    }



    private Task OnSideChanged(bool v)
    {
        LayoutSetService.IsFullSide = v;
        StateHasChanged();
        return Task.CompletedTask;
    }





}

