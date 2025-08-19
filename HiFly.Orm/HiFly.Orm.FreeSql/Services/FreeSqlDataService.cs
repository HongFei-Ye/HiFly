// Copyright (c) HiFly. All rights reserved.
// �ٷ���վ: www.hongfei8.net
// ��ϵ��ʽ: hongfei8@outlook.com

using BootstrapBlazor.Components;
using FreeSql;
using HiFly.Tables.Core.Interfaces;
using HiFly.Tables.Core.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace HiFly.Orm.FreeSql.Services;

/// <summary>
/// FreeSql ���ݷ���ʵ��
/// </summary>
/// <typeparam name="TItem">ʵ������</typeparam>
public class FreeSqlDataService<TItem> : IHiFlyDataService<TItem>
    where TItem : class, new()
{
    /// <summary>
    /// FreeSql ʵ��
    /// </summary>
    protected readonly IFreeSql _freeSql;

    /// <summary>
    /// ��־��¼��
    /// </summary>
    protected readonly ILogger<FreeSqlDataService<TItem>> _logger;

    /// <summary>
    /// ���캯��
    /// </summary>
    /// <param name="freeSql">FreeSql ʵ��</param>
    /// <param name="logger">��־��¼��</param>
    public FreeSqlDataService(
        IFreeSql freeSql,
        ILogger<FreeSqlDataService<TItem>> logger)
    {
        _freeSql = freeSql ?? throw new ArgumentNullException(nameof(freeSql));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// ��ѯ����
    /// </summary>
    public virtual async Task<QueryData<TItem>> OnQueryAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters = null,
        bool isTree = false)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            // ������֤
            if (options.PageItems <= 0)
            {
                _logger.LogWarning("ҳ���С�������0����ǰֵ: {PageItems}", options.PageItems);
                return new QueryData<TItem> { TotalCount = 0, Items = [] };
            }

            // ����Ƿ�Ϊ���α��ʹ�ò�ͬ�����ݼ��ز���
            if (isTree)
            {
                return await GetTreeQueryDataAsync(options, propertyFilterParameters);
            }
            else
            {
                return await GetStandardQueryDataAsync(options, propertyFilterParameters);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "��ѯ����ʱ��������ѡ��: {@Options}", options);

            // ���ؿս���������׳��쳣
            return new QueryData<TItem>
            {
                TotalCount = 0,
                Items = [],
                IsSorted = false,
                IsFiltered = false,
                IsAdvanceSearch = false,
                IsSearch = false
            };
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    public virtual async Task<bool> OnSaveAsync(TItem item, ItemChangedType changedType)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            if (changedType == ItemChangedType.Add)
            {
                return await HandleAddAsync(item);
            }
            else // ItemChangedType.Update
            {
                return await HandleUpdateAsync(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "��������ʱ��������ʵ��: {EntityType}, �������: {ChangeType}",
                typeof(TItem).Name, changedType);
            return false;
        }
    }

    /// <summary>
    /// ɾ������
    /// </summary>
    public virtual async Task<bool> OnDeleteAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemsList = items.ToList();
        if (itemsList.Count == 0)
        {
            return true; // �ռ�����Ϊ�ɹ�
        }

        try
        {
            // ����Ƿ�Ϊ���νṹ
            var isTreeStructure = HasTreeStructure();

            if (isTreeStructure)
            {
                return await HandleTreeDeleteAsync(itemsList);
            }
            else
            {
                return await HandleStandardDeleteAsync(itemsList);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ɾ������ʱ��������ʵ��: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// ��ȡ��׼����ѯ����
    /// </summary>
    private async Task<QueryData<TItem>> GetStandardQueryDataAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters)
    {
        // ������ѯ
        var query = _freeSql.Select<TItem>();

        // Ӧ�ù������� - ��ʵ��
        if (propertyFilterParameters != null)
        {
            query = ApplySimpleFilter(query, propertyFilterParameters);
        }

        // �Ȼ�ȡ����
        var totalCount = await query.CountAsync();
        if (totalCount == 0)
        {
            return CreateEmptyQueryData(options);
        }

        // Ӧ������
        if (!string.IsNullOrEmpty(options.SortName))
        {
            query = ApplySort(query, options.SortName, options.SortOrder);
        }
        else
        {
            // Ĭ������ȷ�����һ����
            query = ApplyDefaultSort(query);
        }

        // Ӧ�÷�ҳ
        var skipCount = (options.PageIndex - 1) * options.PageItems;
        var items = await query
            .Skip(skipCount)
            .Take(options.PageItems)
            .ToListAsync();

        return new QueryData<TItem>
        {
            TotalCount = (int)totalCount, // ת��Ϊint
            Items = items,
            IsSorted = options.SortOrder != SortOrder.Unset,
            IsFiltered = options.Filters.Count != 0,
            IsAdvanceSearch = options.AdvanceSearches.Count != 0,
            IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
        };
    }

    /// <summary>
    /// ��ȡ���α������
    /// </summary>
    private async Task<QueryData<TItem>> GetTreeQueryDataAsync(
        QueryPageOptions options,
        PropertyFilterParameters? propertyFilterParameters)
    {
        try
        {
            // ��֤���νṹ���������
            var (idProp, parentIdProp) = ValidateTreeProperties();

            // 1. �������ڵ��ѯ
            var rootQuery = _freeSql.Select<TItem>();

            // Ӧ�ø��ڵ�ɸѡ������ParentIdΪnull��
            rootQuery = ApplyRootNodeFilter(rootQuery, parentIdProp);

            // Ӧ������
            if (!string.IsNullOrEmpty(options.SortName))
            {
                rootQuery = ApplySort(rootQuery, options.SortName, options.SortOrder);
            }
            else
            {
                rootQuery = ApplyDefaultSort(rootQuery);
            }

            // ��ȡ���ڵ�����
            var totalCount = await rootQuery.CountAsync();
            if (totalCount == 0)
            {
                return CreateEmptyQueryData(options);
            }

            // ��ҳ��ѯ���ڵ�
            var skipCount = (options.PageIndex - 1) * options.PageItems;
            var pagedRoots = await rootQuery
                .Skip(skipCount)
                .Take(options.PageItems)
                .ToListAsync();

            // 2. Ϊÿ�����ڵ��������������
            var allItems = new List<TItem>();
            foreach (var root in pagedRoots)
            {
                allItems.Add(root);
                await LoadChildNodesAsync(root, allItems, idProp, parentIdProp);
            }

            return new QueryData<TItem>
            {
                TotalCount = (int)totalCount,  // ת��Ϊint
                Items = allItems,         // ���ذ��������ӽڵ�ļ���
                IsSorted = options.SortOrder != SortOrder.Unset,
                IsFiltered = options.Filters.Count != 0,
                IsAdvanceSearch = options.AdvanceSearches.Count != 0,
                IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "��ȡ���α������ʱ��������");

            // ��������׼��ѯ
            return await GetStandardQueryDataAsync(options, propertyFilterParameters);
        }
    }

    /// <summary>
    /// Ӧ�ü򵥹���
    /// </summary>
    private static ISelect<TItem> ApplySimpleFilter(ISelect<TItem> query, PropertyFilterParameters filters)
    {
        try
        {
            return ApplyFilterRecursive(query, filters);
        }
        catch (Exception)
        {
            // ����ʧ��ʱ����ԭ��ѯ
            return query;
        }
    }

    /// <summary>
    /// �ݹ�Ӧ�ù�������
    /// </summary>
    private static ISelect<TItem> ApplyFilterRecursive(ISelect<TItem> query, PropertyFilterParameters filter)
    {
        // Ӧ�õ�ǰ����Ĺ�������
        query = ApplySingleFilter(query, filter);

        // Ӧ���ӹ�������
        if (filter.Filters?.Any() == true)
        {
            foreach (var subFilter in filter.Filters)
            {
                if (filter.FilterLogic == FilterLogic.And)
                {
                    query = ApplyFilterRecursive(query, subFilter);
                }
                else // FilterLogic.Or
                {
                    // ����OR�߼�����Ҫ�����ӵĴ���
                    // �����ȼ򻯴���ʵ��Ӧ���п��Կ���ʹ��Expression.OrElse
                    query = ApplyFilterRecursive(query, subFilter);
                }
            }
        }

        return query;
    }

    /// <summary>
    /// Ӧ�õ�����������
    /// </summary>
    private static ISelect<TItem> ApplySingleFilter(ISelect<TItem> query, PropertyFilterParameters filter)
    {
        try
        {
            // ȷ���ֶ�����
            var fieldName = !string.IsNullOrEmpty(filter.ReferenceTypeField)
                ? filter.ReferenceTypeField
                : filter.ValueTypeField;

            if (string.IsNullOrEmpty(fieldName) || filter.MatchValue == null)
            {
                return query;
            }

            // ��֤�ֶ��Ƿ����
            var property = typeof(TItem).GetProperty(fieldName);
            if (property == null)
            {
                return query;
            }

            // ���ݹ��˶���Ӧ�ò�ͬ������
            return filter.FilterAction switch
            {
                FilterAction.Equal => ApplyEqualFilter(query, fieldName, filter.MatchValue),
                FilterAction.NotEqual => ApplyNotEqualFilter(query, fieldName, filter.MatchValue),
                FilterAction.GreaterThan => ApplyGreaterThanFilter(query, fieldName, filter.MatchValue),
                FilterAction.GreaterThanOrEqual => ApplyGreaterThanOrEqualFilter(query, fieldName, filter.MatchValue),
                FilterAction.LessThan => ApplyLessThanFilter(query, fieldName, filter.MatchValue),
                FilterAction.LessThanOrEqual => ApplyLessThanOrEqualFilter(query, fieldName, filter.MatchValue),
                FilterAction.Contains => ApplyContainsFilter(query, fieldName, filter.MatchValue),
                FilterAction.NotContains => ApplyNotContainsFilter(query, fieldName, filter.MatchValue),


                _ => query
            };
        }
        catch (Exception)
        {
            // ������������ʧ��ʱ������ԭ��ѯ
            return query;
        }
    }

    /// <summary>
    /// Ӧ�õ��ڹ���
    /// </summary>
    private static ISelect<TItem> ApplyEqualFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var equal = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(equal, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ�ò����ڹ���
    /// </summary>
    private static ISelect<TItem> ApplyNotEqualFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var notEqual = Expression.NotEqual(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(notEqual, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ�ô��ڹ���
    /// </summary>
    private static ISelect<TItem> ApplyGreaterThanFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var greaterThan = Expression.GreaterThan(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(greaterThan, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ�ô��ڵ��ڹ���
    /// </summary>
    private static ISelect<TItem> ApplyGreaterThanOrEqualFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(greaterThanOrEqual, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ��С�ڹ���
    /// </summary>
    private static ISelect<TItem> ApplyLessThanFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var lessThan = Expression.LessThan(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(lessThan, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ��С�ڵ��ڹ���
    /// </summary>
    private static ISelect<TItem> ApplyLessThanOrEqualFilter(ISelect<TItem> query, string fieldName, object value)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var constant = Expression.Constant(value, property.Type);
        var lessThanOrEqual = Expression.LessThanOrEqual(property, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(lessThanOrEqual, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ�ð������ˣ����������ַ�����
    /// </summary>
    private static ISelect<TItem> ApplyContainsFilter(ISelect<TItem> query, string fieldName, object value)
    {
        if (value is not string stringValue)
            return query;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var constant = Expression.Constant(stringValue);
        var contains = Expression.Call(property, containsMethod!, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(contains, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ�ò��������ˣ����������ַ�����
    /// </summary>
    private static ISelect<TItem> ApplyNotContainsFilter(ISelect<TItem> query, string fieldName, object value)
    {
        if (value is not string stringValue)
            return query;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var constant = Expression.Constant(stringValue);
        var contains = Expression.Call(property, containsMethod!, constant);
        var notContains = Expression.Not(contains);
        var lambda = Expression.Lambda<Func<TItem, bool>>(notContains, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ�ÿ�ʼ�ڹ��ˣ����������ַ�����
    /// </summary>
    private static ISelect<TItem> ApplyStartsWithFilter(ISelect<TItem> query, string fieldName, object value)
    {
        if (value is not string stringValue)
            return query;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        var constant = Expression.Constant(stringValue);
        var startsWith = Expression.Call(property, startsWithMethod!, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(startsWith, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ�ý����ڹ��ˣ����������ַ�����
    /// </summary>
    private static ISelect<TItem> ApplyEndsWithFilter(ISelect<TItem> query, string fieldName, object value)
    {
        if (value is not string stringValue)
            return query;

        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, fieldName);
        var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        var constant = Expression.Constant(stringValue);
        var endsWith = Expression.Call(property, endsWithMethod!, constant);
        var lambda = Expression.Lambda<Func<TItem, bool>>(endsWith, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Ӧ������
    /// </summary>
    private static ISelect<TItem> ApplySort(ISelect<TItem> query, string sortName, SortOrder sortOrder)
    {
        if (string.IsNullOrEmpty(sortName))
            return query;

        try
        {
            // FreeSql �� OrderBy ����֧���ַ������� OrderByDescending ��Ҫ���ʽ
            if (sortOrder == SortOrder.Asc)
            {
                return query.OrderBy(sortName);
            }
            else if (sortOrder == SortOrder.Desc)
            {
                // ���ڽ�������ʹ�� OrderBy + Desc �ķ�ʽ
                // �����������ٷ�ת�������������÷������������ʽ
                return ApplyDescendingSort(query, sortName);
            }
            else
            {
                return query;
            }
        }
        catch (Exception)
        {
            // ����ʧ��ʱ����ԭ��ѯ
            return query;
        }
    }

    /// <summary>
    /// Ӧ�ý�������
    /// </summary>
    private static ISelect<TItem> ApplyDescendingSort(ISelect<TItem> query, string sortName)
    {
        try
        {
            // ���� lambda ���ʽ: x => x.PropertyName
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, sortName);

            // ������ת��Ϊ object ������ƥ�� OrderByDescending<object> ǩ��
            var converted = Expression.Convert(property, typeof(object));
            var lambda = Expression.Lambda<Func<TItem, object>>(converted, parameter);

            return query.OrderByDescending(lambda);
        }
        catch (Exception)
        {
            // ����������ʽʧ�ܣ�ʹ��Ĭ������
            return query.OrderBy(sortName);
        }
    }

    /// <summary>
    /// Ӧ��Ĭ������
    /// </summary>
    protected virtual ISelect<TItem> ApplyDefaultSort(ISelect<TItem> query)
    {
        try
        {
            // ���԰������ֶ�����
            var createTimeProp = typeof(TItem).GetProperty("CreateTime");
            if (createTimeProp != null)
            {
                return query.OrderBy("CreateTime");
            }

            var idProp = typeof(TItem).GetProperty("Id");
            if (idProp != null)
            {
                return query.OrderBy("Id");
            }

            return query;
        }
        catch (Exception)
        {
            // �������ʧ�ܣ�����ԭ��ѯ
            return query;
        }
    }

    /// <summary>
    /// Ӧ�ø��ڵ��������
    /// </summary>
    private static ISelect<TItem> ApplyRootNodeFilter(ISelect<TItem> query, PropertyInfo parentIdProp)
    {
        var parameter = Expression.Parameter(typeof(TItem), "x");
        var property = Expression.Property(parameter, parentIdProp);
        var nullValue = Expression.Constant(null, parentIdProp.PropertyType);
        var equalExpression = Expression.Equal(property, nullValue);
        var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

        return query.Where(lambda);
    }

    /// <summary>
    /// ����Ƿ�������νṹ
    /// </summary>
    private static bool HasTreeStructure()
    {
        var idProp = typeof(TItem).GetProperty("Id");
        var parentIdProp = typeof(TItem).GetProperty("ParentId");
        return idProp != null && parentIdProp != null;
    }

    /// <summary>
    /// ��֤���νṹ���������
    /// </summary>
    private static (PropertyInfo idProp, PropertyInfo parentIdProp) ValidateTreeProperties()
    {
        var idProp = typeof(TItem).GetProperty("Id");
        var parentIdProp = typeof(TItem).GetProperty("ParentId");

        if (idProp == null)
        {
            throw new InvalidOperationException($"���α��Ҫ�� {typeof(TItem).Name} ���� Id ����");
        }

        if (parentIdProp == null)
        {
            throw new InvalidOperationException($"���α��Ҫ�� {typeof(TItem).Name} ���� ParentId ����");
        }

        return (idProp, parentIdProp);
    }

    /// <summary>
    /// �����յĲ�ѯ����
    /// </summary>
    private static QueryData<TItem> CreateEmptyQueryData(QueryPageOptions options)
    {
        return new QueryData<TItem>
        {
            TotalCount = 0,
            Items = [],
            IsSorted = options.SortOrder != SortOrder.Unset,
            IsFiltered = options.Filters.Count != 0,
            IsAdvanceSearch = options.AdvanceSearches.Count != 0,
            IsSearch = options.Searches.Count != 0 || options.CustomerSearches.Count != 0
        };
    }

    /// <summary>
    /// ������Ӳ���
    /// </summary>
    private async Task<bool> HandleAddAsync(TItem item)
    {
        try
        {
            var result = await _freeSql.Insert(item).ExecuteAffrowsAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "���ʵ��ʱ��������: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// ������²���
    /// </summary>
    private async Task<bool> HandleUpdateAsync(TItem item)
    {
        try
        {
            var result = await _freeSql.Update<TItem>().SetSource(item).ExecuteAffrowsAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "����ʵ��ʱ��������: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// �����׼ɾ��
    /// </summary>
    private async Task<bool> HandleStandardDeleteAsync(List<TItem> items)
    {
        try
        {
            var result = await _freeSql.Delete<TItem>().Where(items).ExecuteAffrowsAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ɾ��ʵ��ʱ��������: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// ��������ɾ��
    /// </summary>
    private async Task<bool> HandleTreeDeleteAsync(List<TItem> items)
    {
        try
        {
            var (idProp, parentIdProp) = ValidateTreeProperties();
            var allItemsToDelete = new List<TItem>();

            foreach (var item in items)
            {
                allItemsToDelete.Add(item);
                await CollectChildrenForDeletion(item, allItemsToDelete, idProp, parentIdProp);
            }

            var result = await _freeSql.Delete<TItem>().Where(allItemsToDelete).ExecuteAffrowsAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "����ɾ��ʱ��������: {EntityType}", typeof(TItem).Name);
            return false;
        }
    }

    /// <summary>
    /// �����ӽڵ�
    /// </summary>
    private async Task LoadChildNodesAsync(TItem parent, List<TItem> collector, PropertyInfo idProp, PropertyInfo parentIdProp)
    {
        try
        {
            var parentId = idProp.GetValue(parent);
            if (parentId == null) return;

            // ������ѯ���ʽ
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);

            // ��������ת��
            Expression valueExpression;
            if (parentIdProp.PropertyType == idProp.PropertyType)
            {
                valueExpression = Expression.Constant(parentId, parentIdProp.PropertyType);
            }
            else if (Nullable.GetUnderlyingType(parentIdProp.PropertyType) == idProp.PropertyType)
            {
                // ParentId �ǿɿ����ͣ�Id ����
                valueExpression = Expression.Convert(Expression.Constant(parentId, idProp.PropertyType), parentIdProp.PropertyType);
            }
            else
            {
                // ��������ת��
                valueExpression = Expression.Convert(Expression.Constant(parentId, idProp.PropertyType), parentIdProp.PropertyType);
            }

            var equalExpression = Expression.Equal(property, valueExpression);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            var children = await _freeSql.Select<TItem>().Where(lambda).ToListAsync();

            foreach (var child in children)
            {
                collector.Add(child);
                await LoadChildNodesAsync(child, collector, idProp, parentIdProp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "�����ӽڵ�ʱ��������: {EntityType}", typeof(TItem).Name);
        }
    }

    /// <summary>
    /// �ռ�Ҫɾ�����ӽڵ�
    /// </summary>
    private async Task CollectChildrenForDeletion(TItem parent, List<TItem> collector, PropertyInfo idProp, PropertyInfo parentIdProp)
    {
        try
        {
            var parentId = idProp.GetValue(parent);
            if (parentId == null) return;

            // ������ѯ���ʽ
            var parameter = Expression.Parameter(typeof(TItem), "x");
            var property = Expression.Property(parameter, parentIdProp);

            // ��������ת��
            Expression valueExpression;
            if (parentIdProp.PropertyType == idProp.PropertyType)
            {
                valueExpression = Expression.Constant(parentId, idProp.PropertyType);
            }
            else if (Nullable.GetUnderlyingType(parentIdProp.PropertyType) == idProp.PropertyType)
            {
                // ParentId �ǿɿ����ͣ�Id ����
                valueExpression = Expression.Convert(Expression.Constant(parentId, idProp.PropertyType), parentIdProp.PropertyType);
            }
            else
            {
                // ��������ת��
                valueExpression = Expression.Convert(Expression.Constant(parentId, idProp.PropertyType), parentIdProp.PropertyType);
            }

            var equalExpression = Expression.Equal(property, valueExpression);
            var lambda = Expression.Lambda<Func<TItem, bool>>(equalExpression, parameter);

            var children = await _freeSql.Select<TItem>().Where(lambda).ToListAsync();

            foreach (var child in children)
            {
                collector.Add(child);
                await CollectChildrenForDeletion(child, collector, idProp, parentIdProp);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "�ռ�Ҫɾ�����ӽڵ�ʱ��������: {EntityType}", typeof(TItem).Name);
        }
    }

    /// <summary>
    /// ��Դ�ͷ�
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
