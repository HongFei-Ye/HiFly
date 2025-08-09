// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.RippleSpa;
public class FullPicDataService
{

    /// <summary>
    /// 加载文本
    /// </summary>
    public string LoadingText { get; set; } = "正在加载中...";



    /// <summary>
    /// 背景图片
    /// </summary>
    public string BackGroundImage { get; set; } = "./_content/HiFly.RippleSpa/images/fullpic-bg.jpg";

    /// <summary>
    /// H1标题
    /// </summary>
    public string H1 { get; set; } = "帮联科技";

    /// <summary>
    /// H3标题 - 第一段
    /// </summary>
    public string H3_1 { get; set; } = "帮联";

    /// <summary>
    /// H3标题 - 第二段
    /// </summary>
    public string H3_2 { get; set; } = "数智";

    /// <summary>
    /// H3标题 - 第三段
    /// </summary>
    public string H3_3 { get; set; } = "解决方案";

    /// <summary>
    /// H3标题 - 第四段
    /// </summary>
    public string H3_4 { get; set; } = "致力于打造";

    /// <summary>
    /// H3标题 - 第五段
    /// </summary>
    public string H3_5 { get; set; } = "数字化 智能化";

    /// <summary>
    /// H3标题 - 第六段
    /// </summary>
    public string H3_6 { get; set; } = "信息化 现代化";

    /// <summary>
    /// H3标题 - 第七段
    /// </summary>
    public string H3_7 { get; set; } = "中国特色数智工厂";


    /// <summary>
    /// 后台路径
    /// </summary>
    public string BackstageUrl { get; set; } = $"/Backstage/Home";

    /// <summary>
    /// 注销路径
    /// </summary>
    public string LogoutUrl { get; set; } = "/Connect/Logout";

    /// <summary>
    /// 登录路径
    /// </summary>
    public string LoginUrl { get; set; } = "/Account/Login";

    /// <summary>
    /// 注册路径
    /// </summary>
    public string RegisterUrl { get; set; } = "/Account/Register";



    //public static void SetDefaultSeedData(ModelBuilder modelBuilder)
    //{
    //    List<SystemDictionary> SystemDictionarys =
    //    [
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-BackGroundImage",
    //            Value = "images/reception/hero-header-bg.jpg"
    //        },
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-H1",
    //            Value = "系统控制中心"
    //        },
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-H3_1",
    //            Value = "帮联"
    //        },
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-H3_2",
    //            Value = "数智"
    //        },
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-H3_3",
    //            Value = "解决方案"
    //        },
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-H3_4",
    //            Value = "致力于打造"
    //        },
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-H3_5",
    //            Value = "数字化 智能化"
    //        },
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-H3_6",
    //            Value = "现代化 标准化"
    //        },
    //        new SystemDictionary
    //        {
    //            DictCategoryType = DictCategoryType.SystemConfig,
    //            ValueType = DictValueType.Constant,
    //            ValueKey = "FullOne-H3_7",
    //            Value = "中国特色数智工厂"
    //        },

    //    ];


    //    int dataCount = 1;

    //    foreach (var systemDictionary in SystemDictionarys)
    //    {
    //        modelBuilder.Entity<SystemDictionary>().HasData
    //        (
    //            new SystemDictionary
    //            {
    //                Id = "System-Dictionary-FullOne-DefaultSeedData-" + dataCount.ToString(),
    //                DictCategoryType = systemDictionary.DictCategoryType,
    //                ValueType = systemDictionary.ValueType,
    //                ValueKey = systemDictionary.ValueKey,
    //                Value = systemDictionary.Value
    //            }
    //        );

    //        dataCount++;
    //    }



    //}



}
