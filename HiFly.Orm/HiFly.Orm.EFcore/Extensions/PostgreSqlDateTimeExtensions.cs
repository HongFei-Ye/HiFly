// Copyright (c) 弘飞帮联科技有限公司. All rights reserved.
// 官方网站: www.hongfei8.cn
// 联系方式: felix@hongfei8.com 或 hongfei8@outlook.com

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace HiFly.Orm.EFcore.Extensions;

/// <summary>
/// PostgreSQL DateTime 兼容性扩展方法
/// </summary>
internal static class PostgreSqlDateTimeExtensions
{
    /// <summary>
    /// 确保 DateTime 属性兼容 PostgreSQL
    /// 在保存更改前调用此方法
    /// </summary>
    /// <param name="context">数据库上下文</param>
    internal static void EnsurePostgreSqlDateTimeCompatibility(this DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var entityType = entity.GetType();

            // 获取所有 DateTime 属性
            var dateTimeProperties = entityType.GetProperties()
                .Where(p => (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)) 
                           && p.CanRead && p.CanWrite);

            foreach (var property in dateTimeProperties)
            {
                var value = property.GetValue(entity);

                if (property.PropertyType == typeof(DateTime) && value is DateTime dateTime)
                {
                    var convertedValue = ConvertToUtc(dateTime);
                    if (convertedValue != dateTime)
                    {
                        property.SetValue(entity, convertedValue);
                    }
                }
                else if (property.PropertyType == typeof(DateTime?) && value is DateTime nullableDateTime)
                {
                    var convertedValue = ConvertToUtc(nullableDateTime);
                    if (convertedValue != nullableDateTime)
                    {
                        property.SetValue(entity, convertedValue);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 将 DateTime 转换为 UTC 格式
    /// </summary>
    /// <param name="dateTime">要转换的 DateTime</param>
    /// <returns>UTC 格式的 DateTime</returns>
    private static DateTime ConvertToUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime(),
            DateTimeKind.Utc => dateTime,
            _ => dateTime
        };
    }
}
