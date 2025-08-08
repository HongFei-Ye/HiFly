// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace HiFly.OpeniddictBbUI.IdentityManages;

[CascadingTypeParameter(nameof(TItem))]
public partial class UserTokenManage<TContext, TItem>
        where TContext : DbContext
        where TItem : class, new()
{









}


