// <copyright file="EInvoiceConsolidationController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.EInvoice.Domain;
using ERP.Modules.EInvoice.Domain.Entities;
using ERP.Modules.EInvoice.Infrastructure;
using ERP.Modules.Platform.Api.Authorization;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Api.Controllers;

/// <summary>
/// API for viewing consolidated e-Invoices and eligible documents for consolidation.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/einvoice/consolidation")]
[ApiVersion("1.0")]
public class EInvoiceConsolidationController : ControllerBase
{
    private readonly EInvoiceDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Maximum amount per document for consolidation eligibility.</summary>
    private const decimal MaxConsolidationAmount = 10_000m;

    public EInvoiceConsolidationController(
        EInvoiceDbContext dbContext,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Gets all consolidated e-Invoice documents for the current company.
    /// </summary>
    [HttpGet("documents")]
    [RequirePermission("einvoice", "documents", "view")]
    [ProducesResponseType(typeof(List<ConsolidatedDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsolidatedDocuments(CancellationToken ct)
    {
        var companyId = GetCompanyId();
        if (companyId == Guid.Empty)
        {
            return BadRequest("Company context is required.");
        }

        var docs = await _dbContext.EInvoiceDocuments
            .Where(d => d.CompanyId == companyId
                && d.DocumentType == "ConsolidatedInvoice")
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        var result = docs.Select(d => new ConsolidatedDocumentDto
        {
            Id = d.Id,
            CompanyId = d.CompanyId,
            TotalAmount = d.TotalAmount,
            TaxAmount = d.TaxAmount,
            CurrencyCode = d.CurrencyCode,
            Status = d.Status.ToString(),
            MyInvoisUuid = d.MyInvoisUuid,
            ValidationLink = d.ValidationLink,
            CreatedAt = d.CreatedAt,
            SubmittedAt = d.SubmittedAt,
            ValidatedAt = d.ValidatedAt,
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Gets documents eligible for consolidation (valid invoices under RM10,000 not yet consolidated).
    /// </summary>
    [HttpGet("eligible")]
    [RequirePermission("einvoice", "documents", "view")]
    [ProducesResponseType(typeof(ConsolidationPreview), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEligibleDocuments(CancellationToken ct)
    {
        var companyId = GetCompanyId();
        if (companyId == Guid.Empty)
        {
            return BadRequest("Company context is required.");
        }

        var eligibleDocs = await _dbContext.EInvoiceDocuments
            .Where(d => d.CompanyId == companyId
                && d.Status == EInvoiceDocumentStatus.Valid
                && d.TotalAmount <= MaxConsolidationAmount
                && d.DocumentType == "Invoice"
                && d.ConsolidatedDocumentId == null)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);

        var preview = new ConsolidationPreview
        {
            CompanyId = companyId,
            TotalEligibleCount = eligibleDocs.Count,
            TotalEligibleAmount = eligibleDocs.Sum(d => d.TotalAmount),
            TotalEligibleTax = eligibleDocs.Sum(d => d.TaxAmount),
            MaxAmountPerLine = MaxConsolidationAmount,
            Batches = eligibleDocs
                .Select((d, idx) => new ConsolidationLineItem
                {
                    DocumentId = d.Id,
                    SourceDocumentId = d.SourceDocumentId,
                    TotalAmount = d.TotalAmount,
                    TaxAmount = d.TaxAmount,
                    CurrencyCode = d.CurrencyCode,
                    MyInvoisUuid = d.MyInvoisUuid,
                    CreatedAt = d.CreatedAt,
                    BatchNumber = (idx / 100) + 1,
                })
                .ToList(),
        };

        return Ok(preview);
    }

    /// <summary>
    /// Gets the source documents for a specific consolidated document.
    /// </summary>
    [HttpGet("documents/{id:guid}/sources")]
    [RequirePermission("einvoice", "documents", "view")]
    [ProducesResponseType(typeof(List<ConsolidationLineItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSourceDocuments(Guid id, CancellationToken ct)
    {
        var companyId = GetCompanyId();
        if (companyId == Guid.Empty)
        {
            return BadRequest("Company context is required.");
        }

        var sources = await _dbContext.EInvoiceDocuments
            .Where(d => d.CompanyId == companyId
                && d.ConsolidatedDocumentId == id)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);

        var result = sources.Select(d => new ConsolidationLineItem
        {
            DocumentId = d.Id,
            SourceDocumentId = d.SourceDocumentId,
            TotalAmount = d.TotalAmount,
            TaxAmount = d.TaxAmount,
            CurrencyCode = d.CurrencyCode,
            MyInvoisUuid = d.MyInvoisUuid,
            CreatedAt = d.CreatedAt,
        }).ToList();

        return Ok(result);
    }

    private Guid GetCompanyId()
    {
        if (_currentUser.CompanyIds.Count == 1)
        {
            return _currentUser.CompanyIds[0];
        }

        var claim = User?.FindFirst("CompanyId")?.Value;
        return Guid.TryParse(claim, out var companyId) ? companyId : Guid.Empty;
    }
}

/// <summary>
/// DTO for a consolidated e-Invoice document.
/// </summary>
public class ConsolidatedDocumentDto
{
    /// <summary>Gets or sets the document ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the company ID.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the tax amount.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = "MYR";

    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the MyInvois UUID.</summary>
    public string? MyInvoisUuid { get; set; }

    /// <summary>Gets or sets the validation link.</summary>
    public string? ValidationLink { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the submission timestamp.</summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>Gets or sets the validation timestamp.</summary>
    public DateTime? ValidatedAt { get; set; }
}

/// <summary>
/// Preview of documents eligible for consolidation.
/// </summary>
public class ConsolidationPreview
{
    /// <summary>Gets or sets the company ID.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gets or sets the total number of eligible documents.</summary>
    public int TotalEligibleCount { get; set; }

    /// <summary>Gets or sets the total eligible amount.</summary>
    public decimal TotalEligibleAmount { get; set; }

    /// <summary>Gets or sets the total eligible tax.</summary>
    public decimal TotalEligibleTax { get; set; }

    /// <summary>Gets or sets the maximum amount per line item.</summary>
    public decimal MaxAmountPerLine { get; set; }

    /// <summary>Gets or sets the list of eligible documents.</summary>
    #pragma warning disable CA1002, CA2227 // Do not expose generic lists; Collection properties should be read only
    public List<ConsolidationLineItem> Batches { get; set; } = new();
    #pragma warning restore CA1002, CA2227
}

/// <summary>
/// A single line item in a consolidation batch.
/// </summary>
public class ConsolidationLineItem
{
    /// <summary>Gets or sets the document ID.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Gets or sets the source document ID.</summary>
    public Guid SourceDocumentId { get; set; }

    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Gets or sets the tax amount.</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = "MYR";

    /// <summary>Gets or sets the MyInvois UUID.</summary>
    public string? MyInvoisUuid { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the batch number (for grouping).</summary>
    public int BatchNumber { get; set; }
}
