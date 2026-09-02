// <copyright file="EdiMappingController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Integration.Domain.Entities;
using ERP.Modules.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Integration.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/integration/edi-mappings")]
public class EdiMappingController : ControllerBase
{
    private readonly IntegrationDbContext _context;

    public EdiMappingController(IntegrationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EdiMappingDto>>> GetAll(
        [FromQuery] Guid? companyId, [FromQuery] string? documentType,
        [FromQuery] string? direction, CancellationToken ct)
    {
        var query = _context.EdiDocumentMappings
            .AsNoTracking()
            .Where(x => !companyId.HasValue || x.CompanyId == companyId.Value);
        if (!string.IsNullOrEmpty(documentType))
            query = query.Where(x => x.DocumentType == documentType);
        if (!string.IsNullOrEmpty(direction))
            query = query.Where(x => x.Direction == direction);

        var mappings = await query.OrderBy(x => x.DocumentType).ToListAsync(ct);
        return Ok(mappings.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EdiMappingDto>> GetById(Guid id, CancellationToken ct)
    {
        var mapping = await _context.EdiDocumentMappings.FindAsync(new object[] { id }, ct);
        return mapping is null ? NotFound() : Ok(MapToDto(mapping));
    }

    [HttpPost]
    public async Task<ActionResult<EdiMappingDto>> Create(
        [FromBody] CreateEdiMappingRequest request, CancellationToken ct)
    {
        var mapping = new EdiDocumentMapping(
            request.CompanyId, request.TradingPartnerId, request.DocumentType,
            request.DocumentName, request.Direction, request.Version, request.MappingJson);
        _context.EdiDocumentMappings.Add(mapping);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = mapping.Id }, MapToDto(mapping));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EdiMappingDto>> Update(
        Guid id, [FromBody] UpdateEdiMappingRequest request, CancellationToken ct)
    {
        var mapping = await _context.EdiDocumentMappings.FindAsync(new object[] { id }, ct);
        if (mapping is null) return NotFound();
        mapping.UpdateMapping(request.MappingJson);
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) mapping.Activate(); else mapping.Deactivate();
        }

        await _context.SaveChangesAsync(ct);
        return Ok(MapToDto(mapping));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var mapping = await _context.EdiDocumentMappings.FindAsync(new object[] { id }, ct);
        if (mapping is null) return NotFound();
        _context.EdiDocumentMappings.Remove(mapping);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static EdiMappingDto MapToDto(EdiDocumentMapping m) => new(
        m.Id, m.CompanyId, m.TradingPartnerId, m.DocumentType, m.DocumentName,
        m.Direction, m.Version, m.MappingJson, m.IsActive);

    [HttpGet("document-types")]
    public ActionResult<IReadOnlyList<string>> GetDocumentTypes()
    {
        return Ok(new[] { "850", "810", "855", "856", "820", "997", "999", "846", "860", "862" });
    }
}

public record EdiMappingDto(
    Guid Id, Guid CompanyId, Guid? TradingPartnerId, string DocumentType,
    string DocumentName, string Direction, string Version, string MappingJson, bool IsActive);

public record CreateEdiMappingRequest(
    Guid CompanyId, Guid? TradingPartnerId, string DocumentType,
    string DocumentName, string Direction, string Version, string MappingJson);

public record UpdateEdiMappingRequest(string MappingJson, bool? IsActive);
