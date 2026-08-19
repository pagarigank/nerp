// <copyright file="TaxExemptionCertificatesController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/tax-exemptions")]
public class TaxExemptionCertificatesController : ControllerBase
{
    private readonly OmDbContext _context;

    public TaxExemptionCertificatesController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TaxExemptionCertificateSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId, [FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        var q = _context.TaxExemptionCertificates.AsNoTracking();
        q = companyId is not null ? q.Where(x => x.CompanyId == companyId) : q;
        q = customerId is not null ? q.Where(x => x.CustomerId == customerId) : q;

        var list = await q.OrderBy(x => x.Jurisdiction).ThenBy(x => x.CertificateNumber)
            .Select(x => new TaxExemptionCertificateSummary(
                x.Id,
                x.CompanyId,
                x.CertificateNumber,
                x.CustomerId,
                x.Jurisdiction,
                x.ValidFrom,
                x.ValidTo,
                x.ExemptItemsDescription,
                x.Notes,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<TaxExemptionCertificateSummary>>.Success(list));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync([FromBody] CreateTaxExemptionCertificateRequest r, CancellationToken cancellationToken)
    {
        var e = new TaxExemptionCertificate(
            r.CompanyId,
            r.CertificateNumber,
            r.CustomerId,
            r.Jurisdiction,
            r.ValidFrom,
            r.ValidTo,
            r.ExemptItemsDescription,
            r.Notes);
        _context.TaxExemptionCertificates.Add(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(e.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateAsync(Guid id, [FromBody] UpdateTaxExemptionCertificateRequest r, CancellationToken cancellationToken)
    {
        var e = await _context.TaxExemptionCertificates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Tax exemption certificate {id} not found." }));
        }

        e.Update(r.CertificateNumber, r.Jurisdiction, r.ValidFrom, r.ValidTo, r.CustomerId, r.ExemptItemsDescription, r.Notes);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Updated"));
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<ActionResult<ApiResponse<string>>> RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.TaxExemptionCertificates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Tax exemption certificate {id} not found." }));
        }

        e.Revoke();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Revoked"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var e = await _context.TaxExemptionCertificates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (e is null)
        {
            return NotFound(ApiResponse<string>.Failure(new[] { $"Tax exemption certificate {id} not found." }));
        }

        _context.TaxExemptionCertificates.Remove(e);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<string>.Success("Deleted"));
    }
}

public record TaxExemptionCertificateSummary(
    Guid Id, Guid CompanyId, string CertificateNumber, Guid? CustomerId, string Jurisdiction,
    DateTime ValidFrom, DateTime ValidTo, string? ExemptItemsDescription, string? Notes, bool IsActive);

public record CreateTaxExemptionCertificateRequest(
    Guid CompanyId, string CertificateNumber, Guid? CustomerId, string Jurisdiction,
    DateTime ValidFrom, DateTime ValidTo, string? ExemptItemsDescription, string? Notes);

public record UpdateTaxExemptionCertificateRequest(
    string CertificateNumber, string Jurisdiction, DateTime ValidFrom, DateTime ValidTo,
    Guid? CustomerId, string? ExemptItemsDescription, string? Notes);
