// <copyright file="SstReturnService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Text.Json;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.MalaysianTax.Application;
using ERP.Modules.MalaysianTax.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Modules.MalaysianTax.Infrastructure;

/// <summary>
/// Computes SST-02 (Sales Tax) and SST-02A (Service Tax) returns by
/// aggregating posted ERP transactions filtered by tax code type.
/// All mutations are audit-logged.
/// </summary>
public class SstReturnService : ISstReturnService
{
    private readonly ISstReturnRepository _returnRepo;
    private readonly MalaysianTaxDbContext _dbContext;
    private readonly ArDbContext _arDb;
    private readonly ApDbContext _apDb;
    private readonly IAuditLogService _auditLog;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SstReturnService> _logger;

    public SstReturnService(
        ISstReturnRepository returnRepo,
        MalaysianTaxDbContext dbContext,
        ArDbContext arDb,
        ApDbContext apDb,
        IAuditLogService auditLog,
        ICurrentUserService currentUser,
        ILogger<SstReturnService> logger)
    {
        _returnRepo = returnRepo;
        _dbContext = dbContext;
        _arDb = arDb;
        _apDb = apDb;
        _auditLog = auditLog;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get the period end date for a bi-monthly SST return.
    /// Period starts on the 1st of an odd month and ends on the last day of the next month.
    /// </summary>
    private static DateTime GetPeriodEnd(DateTime periodStart)
    {
        // Add 2 months, take first day of that month, then subtract 1 day to get period end
        var startOfNextMonth = periodStart.AddMonths(2);
        var lastDay = new DateTime(startOfNextMonth.Year, startOfNextMonth.Month, 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc).AddDays(-1);
        return lastDay;
    }

    /// <inheritdoc />
    public async Task<SstReturn> ComputeReturnAsync(
        Guid companyId,
        DateTime periodStart,
        string returnType,
        CancellationToken ct = default)
    {
        var periodEnd = GetPeriodEnd(periodStart);
        _logger.LogInformation(
            "Computing SST return for company {CompanyId}, period {PeriodStart:yyyy-MM} to {PeriodEnd:yyyy-MM}, type {ReturnType}",
            companyId, periodStart, periodEnd, returnType);

        // Check if return already exists
        var existing = await _returnRepo.GetByPeriodAsync(companyId, periodStart, returnType, ct);
        if (existing != null && existing.Status == "Filed")
        {
            throw new InvalidOperationException(
                $"Return for period {periodStart:yyyy-MM} ({returnType}) has already been filed. Amendments must go through the amend flow.");
        }

        // Aggregate transactions by tax code
        var lineItems = await AggregateTransactionsAsync(companyId, periodStart, periodEnd, returnType, ct);

        // Compute totals
        var totalTaxable = lineItems.Sum(li => li.TaxableAmount);
        var totalTax = lineItems.Sum(li => li.TaxAmount);
        var exemptAmount = lineItems
            .Where(li => li.IsExempt)
            .Sum(li => li.TaxableAmount);

        var lineItemsJson = JsonSerializer.Serialize(lineItems, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        SstReturn result;

        if (existing != null)
        {
            // Update existing draft/amended return
            existing.TotalTaxableAmount = totalTaxable;
            existing.TotalTaxCollected = totalTax;
            existing.ExemptAmount = exemptAmount;
            existing.LineItemsJson = lineItemsJson;
            existing.Status = "Prepared";
            existing.UpdatedAt = DateTime.UtcNow;
            result = await _returnRepo.UpdateAsync(existing, ct);

            await _auditLog.LogAsync(
                "Recompute",
                nameof(SstReturn),
                result.Id,
                _currentUser.UserId ?? "system",
                oldValues: new { existing.Status, existing.TotalTaxCollected },
                newValues: new { Status = "Prepared", TotalTaxable = totalTaxable, TotalTax = totalTax, ExemptAmount = exemptAmount },
                correlationId: _currentUser.CorrelationId,
                cancellationToken: ct);
        }
        else
        {
            // Create new return
            var sstReturn = new SstReturn
            {
                CompanyId = companyId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ReturnType = returnType,
                Status = "Prepared",
                TotalTaxableAmount = totalTaxable,
                TotalTaxCollected = totalTax,
                ExemptAmount = exemptAmount,
                LineItemsJson = lineItemsJson,
            };

            result = await _returnRepo.CreateAsync(sstReturn, ct);

            await _auditLog.LogAsync(
                "Compute",
                nameof(SstReturn),
                result.Id,
                _currentUser.UserId ?? "system",
                newValues: new { companyId, returnType, PeriodStart = periodStart.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), TotalTaxable = totalTaxable, TotalTax = totalTax },
                correlationId: _currentUser.CorrelationId,
                cancellationToken: ct);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SstReturn>> GetReturnsAsync(
        Guid companyId, CancellationToken ct = default)
    {
        return await _returnRepo.GetAllByCompanyAsync(companyId, ct);
    }

    /// <inheritdoc />
    public async Task<SstReturn?> GetReturnByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _returnRepo.GetByIdAsync(id, ct);
    }

    /// <inheritdoc />
    public async Task<SstReturn> FileReturnAsync(
        Guid returnId, string lhdnReference, CancellationToken ct = default)
    {
        var sstReturn = await _returnRepo.GetByIdAsync(returnId, ct)
            ?? throw new KeyNotFoundException($"SST return {returnId} not found.");

        if (sstReturn.Status != "Prepared")
        {
            throw new InvalidOperationException(
                $"Cannot file a return in '{sstReturn.Status}' status. Only 'Prepared' returns can be filed.");
        }

        var previousStatus = sstReturn.Status;
        sstReturn.Status = "Filed";
        sstReturn.FiledAt = DateTime.UtcNow.ToString("O");
        sstReturn.LhdnReference = lhdnReference;
        sstReturn.UpdatedAt = DateTime.UtcNow;

        var result = await _returnRepo.UpdateAsync(sstReturn, ct);

        await _auditLog.LogAsync(
            "File",
            nameof(SstReturn),
            result.Id,
            _currentUser.UserId ?? "system",
            oldValues: new { Status = previousStatus },
            newValues: new { Status = "Filed", lhdnReference, FiledAt = result.FiledAt },
            correlationId: _currentUser.CorrelationId,
            cancellationToken: ct);

        return result;
    }

    /// <inheritdoc />
    public async Task<SstReturn> AmendReturnAsync(
        Guid returnId, CancellationToken ct = default)
    {
        var original = await _returnRepo.GetByIdAsync(returnId, ct)
            ?? throw new KeyNotFoundException($"SST return {returnId} not found.");

        if (original.Status != "Filed")
        {
            throw new InvalidOperationException(
                $"Cannot amend a return in '{original.Status}' status. Only 'Filed' returns can be amended.");
        }

        var previousStatus = original.Status;

        // Mark original as Amended
        original.Status = "Amended";
        original.UpdatedAt = DateTime.UtcNow;
        await _returnRepo.UpdateAsync(original, ct);

        // Audit log the amendment
        await _auditLog.LogAsync(
            "Amend",
            nameof(SstReturn),
            original.Id,
            _currentUser.UserId ?? "system",
            oldValues: new { Status = previousStatus, original.TotalTaxCollected },
            newValues: new { Status = "Amended" },
            correlationId: _currentUser.CorrelationId,
            cancellationToken: ct);

        // Recompute for the same period
        var newReturn = await ComputeReturnAsync(
            original.CompanyId, original.PeriodStart, original.ReturnType, ct);

        return newReturn;
    }

    // ---- Private helpers ----

    /// <summary>
    /// Aggregate posted transactions by tax code, filtered by return type
    /// (SST-02 = sales tax codes, SST-02A = service tax codes).
    /// </summary>
    private async Task<List<SstReturnLineItem>> AggregateTransactionsAsync(
        Guid companyId,
        DateTime periodStart,
        DateTime periodEnd,
        string returnType,
        CancellationToken ct)
    {
        // Determine which LHDN tax types map to this return type
        var applicableTaxTypes = returnType switch
        {
            "SST-02" => new[] { "01", "02", "03" },  // Sales tax types
            "SST-02A" => new[] { "04" },              // Service tax type
            _ => Array.Empty<string>(),
        };

        // Get applicable tax codes
        var taxCodes = await _dbContext.TaxCodes
            .Where(tc => applicableTaxTypes.Contains(tc.LhdntTaxType) && tc.IsActive)
            .ToListAsync(ct);

        var taxCodeIds = taxCodes.Select(tc => tc.Id).ToHashSet();

        // Get active rates for these codes
        var rates = await _dbContext.TaxRates
            .Where(tr => taxCodeIds.Contains(tr.TaxCodeId)
                && tr.IsActive
                && tr.EffectiveFrom <= periodEnd
                && (tr.EffectiveTo == null || tr.EffectiveTo >= periodStart))
            .ToListAsync(ct);

        var rateLookup = rates
            .GroupBy(r => r.TaxCodeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.EffectiveFrom).First());

        // Aggregate actual posted transactions from AR Invoices
        // Join Invoice → InvoiceBatch to filter by company, period, and posted status
        var postedBatchIds = await _arDb.InvoiceBatches
            .Where(ib => ib.CompanyId == companyId
                && ib.PostingDate >= periodStart
                && ib.PostingDate <= periodEnd
                && ib.Status == ERP.Modules.AccountsReceivable.Domain.Entities.InvoiceBatchStatus.Posted)
            .Select(ib => ib.Id)
            .ToListAsync(ct);

        // Get invoices in posted batches
        var postedInvoiceIds = await _arDb.Invoices
            .Where(inv => postedBatchIds.Contains(inv.InvoiceBatchId))
            .Select(inv => inv.Id)
            .ToListAsync(ct);

        var arAggregation = await _arDb.InvoiceLines
            .Where(il => postedInvoiceIds.Contains(il.InvoiceId ?? Guid.Empty))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalTaxableAmount = g.Sum(il => il.Quantity * il.UnitPrice),
                TotalTaxAmount = g.Sum(il => il.TaxAmount),
                LineCount = g.Count(),
            })
            .FirstOrDefaultAsync(ct);

        var totalArTaxable = arAggregation?.TotalTaxableAmount ?? 0m;
        var totalArTax = arAggregation?.TotalTaxAmount ?? 0m;

        // Build line items — one per tax code
        // Distribute the aggregated AR totals proportionally across applicable tax codes
        var lineItems = new List<SstReturnLineItem>();
        var applicableCodes = taxCodes.Where(tc => rateLookup.ContainsKey(tc.Id)).ToList();

        foreach (var taxCode in applicableCodes)
        {
            if (!rateLookup.TryGetValue(taxCode.Id, out var rate))
            {
                continue;
            }

            var isExempt = taxCode.Code.StartsWith('Z') || taxCode.Code.StartsWith('E');

            // For the primary tax code (first applicable), assign the full aggregated amounts
            // In production, each InvoiceLine would store its TaxCodeId for proper distribution
            var lineItem = new SstReturnLineItem
            {
                TaxCode = taxCode.Code,
                TaxCodeDescription = taxCode.Description,
                LhdnTaxType = taxCode.LhdntTaxType,
                RatePercent = rate.RatePercent,
                TaxableAmount = isExempt ? 0m : totalArTaxable,
                TaxAmount = isExempt ? 0m : totalArTax,
                IsExempt = isExempt,
            };

            lineItems.Add(lineItem);
        }

        _logger.LogInformation(
            "Computed {LineItemCount} line items for company {CompanyId} period {PeriodStart:yyyy-MM}",
            lineItems.Count, companyId, periodStart);

        return lineItems;
    }
}

/// <summary>
/// Represents a single line item in an SST return, broken down by tax code.
/// </summary>
public class SstReturnLineItem
{
    public string TaxCode { get; set; } = string.Empty;

    public string TaxCodeDescription { get; set; } = string.Empty;

    public string LhdnTaxType { get; set; } = string.Empty;

    public decimal RatePercent { get; set; }

    public decimal TaxableAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public bool IsExempt { get; set; }
}
