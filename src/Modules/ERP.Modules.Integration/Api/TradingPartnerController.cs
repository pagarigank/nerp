// <copyright file="TradingPartnerController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Integration.Domain.Entities;
using ERP.Modules.Integration.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Integration.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration/trading-partners")]
public class TradingPartnerController : ControllerBase
{
    private readonly IntegrationDbContext _context;

    public TradingPartnerController(IntegrationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TradingPartnerDto>>> GetAll(
        [FromQuery] Guid? companyId, CancellationToken ct)
    {
        var partners = await _context.TradingPartners
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return Ok(partners.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TradingPartnerDto>> GetById(Guid id, CancellationToken ct)
    {
        var partner = await _context.TradingPartners.FindAsync(new object[] { id }, ct);
        return partner is null ? NotFound() : Ok(MapToDto(partner));
    }

    [HttpPost]
    public async Task<ActionResult<TradingPartnerDto>> Create(
        [FromBody] CreateTradingPartnerRequest request, CancellationToken ct)
    {
        var partner = new TradingPartner(
            request.CompanyId, request.PartnerCode, request.Name,
            request.IsaId, request.GsSenderId, request.GsReceiverId,
            request.Protocol, request.CertificateThumbprint, request.EdiVersion);
        _context.TradingPartners.Add(partner);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = partner.Id }, MapToDto(partner));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TradingPartnerDto>> Update(
        Guid id, [FromBody] UpdateTradingPartnerRequest request, CancellationToken ct)
    {
        var partner = await _context.TradingPartners.FindAsync(new object[] { id }, ct);
        if (partner is null) return NotFound();
        partner.UpdateProtocol(request.Protocol, request.CertificateThumbprint, request.CertificateExpiresOn);
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) partner.Activate(); else partner.Deactivate();
        }

        await _context.SaveChangesAsync(ct);
        return Ok(MapToDto(partner));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var partner = await _context.TradingPartners.FindAsync(new object[] { id }, ct);
        if (partner is null) return NotFound();
        partner.Activate();
        await _context.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var partner = await _context.TradingPartners.FindAsync(new object[] { id }, ct);
        if (partner is null) return NotFound();
        partner.Deactivate();
        await _context.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var partner = await _context.TradingPartners.FindAsync(new object[] { id }, ct);
        if (partner is null) return NotFound();
        _context.TradingPartners.Remove(partner);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static TradingPartnerDto MapToDto(TradingPartner p) => new(
        p.Id, p.CompanyId, p.PartnerCode, p.Name, p.IsaId,
        p.GsSenderId, p.GsReceiverId, p.Protocol, p.CertificateThumbprint,
        p.EdiVersion, p.IsActive, p.CertificateExpiresOn);
}

public record TradingPartnerDto(
    Guid Id, Guid CompanyId, string PartnerCode, string Name,
    string? IsaId, string? GsSenderId, string? GsReceiverId,
    string? Protocol, string? CertificateThumbprint, string EdiVersion,
    bool IsActive, DateTimeOffset? CertificateExpiresOn);

public record CreateTradingPartnerRequest(
    Guid CompanyId, string PartnerCode, string Name,
    string? IsaId, string? GsSenderId, string? GsReceiverId,
    string? Protocol, string? CertificateThumbprint, string EdiVersion);

public record UpdateTradingPartnerRequest(
    string? Protocol, string? CertificateThumbprint,
    DateTimeOffset? CertificateExpiresOn, bool? IsActive);
