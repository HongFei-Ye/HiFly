// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;
using HiFly.BbLayout.Services.Interfaces;

namespace HiFly.BbLayout.Services;

public class LayoutSetService : ILayoutSetService
{
    /// <summary>
    /// 是否显示返回顶端按钮 默认为 true
    /// </summary>
    public bool ShowGotoTop { get; set; } = true;

    /// <summary>
    /// 是否显示收缩展开 Bar 默认 true
    /// </summary>
    public bool ShowCollapseBar { get; set; } = true;

    /// <summary>
    /// 是否显示 Footer 模板 默认为 true
    /// </summary>
    public bool ShowFooter { get; set; } = true;

    /// <summary>
    /// 侧边栏宽度，支持百分比，设置 0 时关闭宽度功能 默认值 300
    /// </summary>
    public string? SideWidth { get; set; } = "0";

    /// <summary>
    /// 获得/设置 是否为整页面布局 默认为 true
    /// </summary>
    /// <remarks>为真时增加 is-page 样式</remarks>
    public bool IsPage { get; set; } = true;

    /// <summary>
    /// 侧边栏是否占满整个左侧 默认为 false
    /// </summary>
    public bool IsFullSide { get; set; } = true;

    /// <summary>
    /// 是否固定 Header 组件 默认为 true
    /// </summary>
    public bool IsFixedHeader { get; set; } = true;

    /// <summary>
    /// 是否固定 Footer 组件 默认为 true
    /// </summary>
    public bool IsFixedFooter { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否显示分割栏 默认 false 不显示
    /// 仅在 左右布局时有效
    /// </summary>
    public bool ShowSplitebar { get; set; } = true;

    /// <summary>
    /// 获得/设置 侧边栏最小宽度 默认 null 未设置
    /// </summary>
    public int? SidebarMinWidth { get; set; } = 200;

    /// <summary>
    /// 获得/设置 侧边栏最大宽度 默认 null 未设置
    /// </summary>
    public int? SidebarMaxWidth { get; set; } = 350;


    public TabStyle TabStyle { get; set; } = TabStyle.Chrome;

    public bool ShowToolbar { get; set; } = true;

    public bool ShowTabContextMenu { get; set; } = true;

    public bool ShowTabInHeader { get; set; } = false;

    /// <summary>
    /// 获得/设置 标签是否显示扩展按钮 默认 false
    /// </summary>
    public bool ShowTabExtendButtons { get; set; } = false;


    /// <summary>
    /// 默认标签页 关闭所有标签页时自动打开此地址 默认 "/Backstage/Home"
    /// </summary>
    public string TabDefaultUrl { get; set; } = "/";

    /// <summary>
    /// 是否右侧使用 Tab 组件 默认为 true
    /// </summary>
    public bool UseTabSet { get; set; } = true;

    /// <summary>
    /// 获得/设置 是否固定多标签 Header 默认 false
    /// </summary>
    public bool IsFixedTabHeader { get; set; } = true;

    /// <summary>
    /// 侧边栏导航菜单集合
    /// </summary>
    public List<MenuItem> Menus { get; set; } = [];

    /// <summary>
    /// 主题Class
    /// </summary>
    public string Theme { get; set; } = "";





}
