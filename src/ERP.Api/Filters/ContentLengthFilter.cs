// <copyright file="ContentLengthFilter.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.Api.Filters;

public class ContentLengthFilter : IAsyncResultFilter
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            var json = JsonSerializer.Serialize(objectResult.Value, _options);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.HttpContext.Response.ContentLength = bytes.Length;
            context.HttpContext.Response.Headers.Remove("Transfer-Encoding");
            context.Result = new ContentResult { Content = json, ContentType = "application/json", StatusCode = objectResult.StatusCode ?? 200 };
        }

        await next();
    }
}
