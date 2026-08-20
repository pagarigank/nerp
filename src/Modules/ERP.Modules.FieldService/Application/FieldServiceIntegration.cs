// <copyright file="FieldServiceIntegration.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Modules.FieldService.Application;

/// <summary>
/// Cross-module integration for Field Service: posts completed work orders to
/// Inventory (parts issue), Accounts Receivable (customer invoice) and Payroll
/// (technician time capture) by calling the other modules' HTTP endpoints,
/// forwarding the caller's bearer token. This keeps the modular monolith wired
/// to the phases that already exist (Inventory / AR / Payroll).
/// </summary>
public interface IFieldServiceIntegration
{
    Task IssuePartsAsync(Guid companyId, Guid itemId, Guid warehouseId, decimal quantity, string? reference, CancellationToken cancellationToken);

    Task<Guid?> BillWorkOrderToArAsync(
        Guid companyId, Guid customerId, string workOrderNumber, decimal billableTotal, string? description, CancellationToken cancellationToken);

    Task<Guid?> RecordTechnicianTimeAsync(
        Guid companyId, Guid employeeId, decimal laborHours, decimal laborRate, DateTime workDate, CancellationToken cancellationToken);
}

public class FieldServiceIntegration : IFieldServiceIntegration
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _baseUrl;

    public FieldServiceIntegration(
        HttpClient http,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _baseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:5000";
    }

    private void AttachBearer()
    {
        var auth = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(auth))
        {
            _http.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(auth);
        }
    }

    public async Task IssuePartsAsync(
        Guid companyId, Guid itemId, Guid warehouseId, decimal quantity, string? reference, CancellationToken cancellationToken)
    {
        AttachBearer();
        var payload = new
        {
            companyId,
            itemId,
            warehouseId,
            quantity,
            referenceNumber = reference,
            notes = "Field service work order part issue",
        };
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/v1/inventory/transactions/issue", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid?> BillWorkOrderToArAsync(
        Guid companyId, Guid customerId, string workOrderNumber, decimal billableTotal, string? description, CancellationToken cancellationToken)
    {
        AttachBearer();
        var invoiceNumber = $"FS-{workOrderNumber}";
        var revenueAccountId = await ResolveRevenueAccountIdAsync(companyId, cancellationToken);
        var payload = new
        {
            companyId,
            customerId,
            invoiceNumber,
            invoiceDate = DateTimeOffset.UtcNow,
            dueDate = DateTimeOffset.UtcNow.AddDays(30),
            description = description ?? $"Field service work order {workOrderNumber}",
            lines = new[]
            {
                new
                {
                    accountId = revenueAccountId,
                    description = $"Field service {workOrderNumber}",
                    quantity = 1m,
                    unitPrice = billableTotal,
                    taxAmount = 0m,
                    discountAmount = (decimal?)null,
                },
            },
        };
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/v1/ar/invoices/standalone", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("invoiceId", out var invId) &&
            invId.ValueKind == JsonValueKind.String)
        {
            var invoiceIdString = invId.GetString();
            return Guid.Parse(invoiceIdString!);
        }

        return null;
    }

    private async Task<Guid> ResolveRevenueAccountIdAsync(Guid companyId, CancellationToken cancellationToken)
    {
        try
        {
            AttachBearer();
            var url = $"{_baseUrl}/api/v1/platform/accounts?companyId={companyId}";
            var response = await _http.GetAsync(new Uri(url), cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("data", out var accounts) &&
                    accounts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var acc in accounts.EnumerateArray())
                    {
                        if (acc.TryGetProperty("AccountNumber", out var num) &&
                            num.GetString() == "4000" &&
                            acc.TryGetProperty("Id", out var id) &&
                            id.ValueKind == JsonValueKind.String)
                        {
                            var revenueId = id.GetString();
                            return Guid.Parse(revenueId!);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Fall back to empty; AR will reject and the work order stays unbilled.
        }

        return Guid.Empty;
    }

    public async Task<Guid?> RecordTechnicianTimeAsync(
        Guid companyId, Guid employeeId, decimal laborHours, decimal laborRate, DateTime workDate, CancellationToken cancellationToken)
    {
        AttachBearer();
        var weekEnding = workDate.Date.AddDays(7 - (int)workDate.DayOfWeek);
        var create = new
        {
            companyId,
            employeeId,
            weekEnding,
        };
        var createResp = await _http.PostAsJsonAsync($"{_baseUrl}/api/v1/payroll/timesheets", create, cancellationToken);
        if (!createResp.IsSuccessStatusCode)
        {
            return null;
        }

        var createContent = await createResp.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(createContent);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var timesheetIdString = data.GetString();
        var timesheetId = Guid.Parse(timesheetIdString!);
        var line = new
        {
            projectId = (Guid?)null,
            taskId = (Guid?)null,
            payCodeId = Guid.Empty,
            workDate,
            hours = laborHours,
            rate = laborRate,
            tradeClassification = (string?)null,
            isBillable = true,
            isOvertime = false,
        };
        var lineResp = await _http.PostAsJsonAsync($"{_baseUrl}/api/v1/payroll/timesheets/{timesheetId}/lines", line, cancellationToken);
        return lineResp.IsSuccessStatusCode ? timesheetId : null;
    }
}
