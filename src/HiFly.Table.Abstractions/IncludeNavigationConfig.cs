// SPDX-License-Identifier: Apache-2.0

namespace HiFly.Table;

/// <summary>
/// 导航属性Include配置类，支持多级ThenInclude（链式调用）
/// </summary>
public class IncludeNavigationConfig
{
    /// <summary>
    /// 导航属性名称
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 子级导航属性配置（用于ThenInclude）
    /// </summary>
    public List<IncludeNavigationConfig>? ThenIncludes { get; set; }

    /// <summary>
    /// 创建简单Include配置
    /// </summary>
    /// <param name="propertyName">导航属性名称</param>
    /// <returns>配置实例</returns>
    public static IncludeNavigationConfig Create(string propertyName)
    {
        return new IncludeNavigationConfig { PropertyName = propertyName };
    }

    /// <summary>
    /// 创建带ThenInclude的配置
    /// </summary>
    /// <param name="propertyName">导航属性名称</param>
    /// <param name="thenIncludes">子级配置</param>
    /// <returns>配置实例</returns>
    public static IncludeNavigationConfig Create(string propertyName, params IncludeNavigationConfig[] thenIncludes)
    {
        return new IncludeNavigationConfig
        {
            PropertyName = propertyName,
            ThenIncludes = thenIncludes.ToList()
        };
    }

    /// <summary>
    /// 添加单个子级导航属性配置（返回子配置，支持链式调用）
    /// </summary>
    /// <param name="propertyName">子级导航属性名称</param>
    /// <returns>子级配置实例（用于继续链式调用）</returns>
    public IncludeNavigationConfig ThenInclude(string propertyName)
    {
        ThenIncludes ??= [];
        var config = new IncludeNavigationConfig { PropertyName = propertyName };
        ThenIncludes.Add(config);
        return config; // 返回子配置，支持继续 ThenInclude
    }

    /// <summary>
    /// 添加子级导航属性配置对象（类似 PropertyFilterParameters.Add）
    /// </summary>
    /// <param name="config">子级配置对象</param>
    /// <returns>当前配置实例（支持链式调用）</returns>
    public IncludeNavigationConfig Add(IncludeNavigationConfig? config)
    {
        if (config == null)
        {
            return this;
        }

        ThenIncludes ??= [];
        ThenIncludes.Add(config);

        return this; // 返回当前实例，支持链式添加
    }

    /// <summary>
    /// 批量添加多个子级导航属性名称（返回当前实例）
    /// </summary>
    /// <param name="propertyNames">子级导航属性名称数组</param>
    /// <returns>当前配置实例（支持链式调用）</returns>
    public IncludeNavigationConfig AddRange(params string[] propertyNames)
    {
        ThenIncludes ??= [];
        foreach (var name in propertyNames)
        {
            ThenIncludes.Add(new IncludeNavigationConfig { PropertyName = name });
        }
        return this; // 返回当前实例
    }

}
