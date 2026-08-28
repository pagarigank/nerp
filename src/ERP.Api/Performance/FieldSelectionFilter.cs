// <copyright file="FieldSelectionFilter.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

#pragma warning disable SA1200, SA1600, SA1309, SA1203, SA1009, SA1028, SA1101, SA1413
#pragma warning disable CA1052, CA1062, CA1031, CA1848, CA2007, CA1861, CA1305, CA1308, CA1310, CA1307, CA1823, S1118
#pragma warning disable SA1515, S3376, S1144

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.Api.Performance;

/// <summary>
/// Action filter that implements sparse fieldsets (field selection) via the ?fields query parameter.
/// When present, only the requested fields are included in the JSON response.
/// Example: GET /api/v1/ap/vendors?fields=id,name,vendorClass
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class FieldSelectionFilter : ActionFilterAttribute
{
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            var fieldsParam = context.HttpContext.Request.Query["fields"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(fieldsParam))
            {
                var requestedFields = fieldsParam
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(f => f.ToUpperInvariant())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var filteredValue = FilterFields(objectResult.Value, requestedFields);
                objectResult.Value = filteredValue;

                context.HttpContext.Response.Headers["X-Fields"] = fieldsParam;
            }
        }

        await next();
    }

    private static object FilterFields(object value, HashSet<string> requestedFields)
    {
        var valueType = value.GetType();

        // Handle collections
        if (value is IEnumerable<object> enumerable)
        {
            var list = enumerable.ToList();
            if (list.Count == 0)
                return list;

            var filtered = list.Select(item => FilterSingleObject(item, requestedFields)).ToList();
            return filtered;
        }

        // Handle ApiResponse<T> wrapper
        if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().Name.StartsWith("ApiResponse", StringComparison.Ordinal))
        {
            var dataProp = valueType.GetProperty("Data");
            if (dataProp != null)
            {
                var dataValue = dataProp.GetValue(value);
                if (dataValue != null)
                {
                    var filteredData = FilterFields(dataValue, requestedFields);
                    var successMethod = valueType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "Success" && m.GetParameters().Length == 2);
                    if (successMethod != null)
                    {
                        return successMethod.Invoke(null, [filteredData, null])!;
                    }
                }
            }
        }

        // Handle CursorPagedResult<T>
        if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().Name.Contains("CursorPagedResult", StringComparison.Ordinal))
        {
            var itemsProp = valueType.GetProperty("Items");
            if (itemsProp != null)
            {
                var itemsValue = itemsProp.GetValue(value);
                if (itemsValue is IEnumerable<object> items)
                {
                    var filteredItems = items.Select(i => FilterSingleObject(i, requestedFields)).ToList();
                    var instance = Activator.CreateInstance(valueType);
                    var itemsSetter = valueType.GetProperty("Items");
                    itemsSetter?.SetValue(instance, filteredItems);

                    foreach (var prop in valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (prop.Name != "Items" && prop.CanWrite)
                        {
                            prop.SetValue(instance, prop.GetValue(value));
                        }
                    }

                    return instance!;
                }
            }
        }

        return FilterSingleObject(value, requestedFields);
    }

    private static object FilterSingleObject(object obj, HashSet<string> requestedFields)
    {
        var type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && requestedFields.Contains(p.Name.ToUpperInvariant()))
            .ToList();

        if (properties.Count == 0)
            return obj;

        var dict = new Dictionary<string, object?>();
        foreach (var prop in properties)
        {
            dict[prop.Name] = prop.GetValue(obj);
        }

        return dict;
    }
}
