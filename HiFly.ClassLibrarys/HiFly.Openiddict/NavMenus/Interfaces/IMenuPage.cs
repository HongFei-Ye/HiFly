// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

namespace HiFly.Openiddict.NavMenus.Interfaces;
public interface IMenuPage
{
    string Id { get; set; }

    string Text { get; set; }

    string Icon { get; set; }

    string Url { get; set; }


    List<string> RouterUserIds { get; set; }

    List<string> RouterRoleIds { get; set; }

    int RouterRoleHierarchy { get; set; }

    List<string> AddDataUserIds { get; set; }

    List<string> AddDataRoleIds { get; set; }

    int AddDataRoleHierarchy { get; set; }

    List<string> EditDataUserIds { get; set; }

    List<string> EditDataRoleIds { get; set; }

    int EditDataRoleHierarchy { get; set; }

    List<string> DeleteDataUserIds { get; set; }

    List<string> DeleteDataRoleIds { get; set; }

    int DeleteDataRoleHierarchy { get; set; }

    List<string> QueryDataUserIds { get; set; }

    List<string> QueryDataRoleIds { get; set; }

    int QueryDataRoleaHierarchy { get; set; }




    string? OtherAuthorizeJson { get; set; }

}
