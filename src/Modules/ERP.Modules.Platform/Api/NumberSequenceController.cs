// <copyright file="NumberSequenceController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/number-sequences")]
#pragma warning disable S6960
public class NumberSequenceController : ControllerBase
#pragma warning restore S6960
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly PlatformDbContext _context;

    public NumberSequenceController(IUnitOfWork unitOfWork, IAuditLogService auditLogService, PlatformDbContext context)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NumberSequenceDto>>> GetAll([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var sequences = await _context.NumberSequences
            .AsNoTracking()
            .ApplyCompanyScope(HttpContext, s => s.CompanyId, companyId)
            .ToListAsync(cancellationToken);
        return Ok(sequences.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NumberSequenceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sequence = await _unitOfWork.NumberSequences.GetByIdAsync(id, cancellationToken);
        if (sequence == null)
            return NotFound();

        return Ok(MapToDto(sequence));
    }

    [HttpPost]
    public async Task<ActionResult<NumberSequenceDto>> Create([FromBody] CreateNumberSequenceRequest request, CancellationToken cancellationToken)
    {
        var sequence = new NumberSequence(
            request.CompanyId,
            request.Name,
            request.Prefix,
            request.NextValue,
            request.Increment,
            request.MinValue,
            request.MaxValue);

        await _unitOfWork.NumberSequences.AddAsync(sequence, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(NumberSequence),
            sequence.Id,
            "system",
            newValues: new { request.Name, request.Prefix },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = sequence.Id }, MapToDto(sequence));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NumberSequenceDto>> Update(Guid id, [FromBody] UpdateNumberSequenceRequest request, CancellationToken cancellationToken)
    {
        var sequence = await _unitOfWork.NumberSequences.GetByIdAsync(id, cancellationToken);
        if (sequence == null)
            return NotFound();

        sequence.Update(request.Name, request.Prefix, request.Increment, request.MinValue, request.MaxValue);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(sequence));
    }

    [HttpPost("{id:guid}/next")]
    public async Task<ActionResult<string>> GetNextNumber(Guid id, CancellationToken cancellationToken)
    {
        var sequence = await _unitOfWork.NumberSequences.GetByIdAsync(id, cancellationToken);
        if (sequence == null)
            return NotFound();

        var nextNumber = sequence.GetNextNumber();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(nextNumber);
    }

    [HttpPost("{id:guid}/reset")]
    public async Task<IActionResult> Reset(Guid id, [FromQuery] int startingValue, CancellationToken cancellationToken)
    {
        var sequence = await _unitOfWork.NumberSequences.GetByIdAsync(id, cancellationToken);
        if (sequence == null)
            return NotFound();

        sequence.Reset(startingValue);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static NumberSequenceDto MapToDto(NumberSequence sequence)
    {
        return new NumberSequenceDto(
            sequence.Id,
            sequence.CompanyId,
            sequence.Name,
            sequence.Prefix,
            sequence.NextValue,
            sequence.Increment,
            sequence.MinValue,
            sequence.MaxValue,
            sequence.IsActive,
            sequence.CreatedOn,
            sequence.ModifiedOn);
    }
}
