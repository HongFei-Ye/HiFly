// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using BootstrapBlazor.Components;

namespace HiFly.BbLayout.Services.Interfaces;

/// <summary>
/// 布局设置服务接口，用于配置页面布局组件的行为和样式
/// </summary>
public interface ILayoutSetService
{
    /// <summary>
    /// 是否显示返回顶端按钮 默认为 true
    /// </summary>
    bool ShowGotoTop { get; set; }

    /// <summary>
    /// 是否显示收缩展开 Bar 默认 true
    /// </summary>
    bool ShowCollapseBar { get; set; }

    /// <summary>
    /// 是否显示 Footer 模板 默认为 true
    /// </summary>
    bool ShowFooter { get; set; }

    /// <summary>
    /// 侧边栏宽度，支持百分比，设置 0 时关闭宽度功能 默认值 300
    /// </summary>
    string? SideWidth { get; set; }

    /// <summary>
    /// 获得/设置 是否为整页面布局 默认为 true
    /// </summary>
    /// <remarks>为真时增加 is-page 样式</remarks>
    bool IsPage { get; set; }

    /// <summary>
    /// 侧边栏是否占满整个左侧 默认为 false
    /// </summary>
    bool IsFullSide { get; set; }

    /// <summary>
    /// 是否固定 Header 组件 默认为 true
    /// </summary>
    bool IsFixedHeader { get; set; }

    /// <summary>
    /// 是否固定 Footer 组件 默认为 true
    /// </summary>
    bool IsFixedFooter { get; set; }

    /// <summary>
    /// 获得/设置 是否显示分割栏 默认 false 不显示
    /// 仅在 左右布局时有效
    /// </summary>
    bool ShowSplitebar { get; set; }

    /// <summary>
    /// 获得/设置 侧边栏最小宽度 默认 null 未设置
    /// </summary>
    int? SidebarMinWidth { get; set; }

    /// <summary>
    /// 获得/设置 侧边栏最大宽度 默认 null 未设置
    /// </summary>
    int? SidebarMaxWidth { get; set; }

    /// <summary>
    /// 获得/设置 Tab 样式
    /// </summary>
    TabStyle TabStyle { get; set; }

    /// <summary>
    /// 获得/设置 是否显示工具栏
    /// </summary>
    bool ShowToolbar { get; set; }

    /// <summary>
    /// 获得/设置 是否显示 Tab 上下文菜单
    /// </summary>
    bool ShowTabContextMenu { get; set; }

    /// <summary>
    /// 获得/设置 是否在 Header 中显示 Tab
    /// </summary>
    bool ShowTabInHeader { get; set; }

    /// <summary>
    /// 获得/设置 标签是否显示扩展按钮 默认 false
    /// </summary>
    bool ShowTabExtendButtons { get; set; }

    /// <summary>
    /// 默认标签页 关闭所有标签页时自动打开此地址 默认 "/Backstage/Home"
    /// </summary>
    string TabDefaultUrl { get; set; }

    /// <summary>
    /// 是否右侧使用 Tab 组件 默认为 true
    /// </summary>
    bool UseTabSet { get; set; }

    /// <summary>
    /// 获得/设置 是否固定多标签 Header 默认 false
    /// </summary>
    bool IsFixedTabHeader { get; set; }

    /// <summary>
    /// 侧边栏导航菜单集合
    /// </summary>
    List<MenuItem> Menus { get; set; }

    /// <summary>
    /// 主题Class
    /// </summary>
    string Theme { get; set; }
}
