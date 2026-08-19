// <copyright file="SegmentValueController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/segment-values")]
public class SegmentValueController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public SegmentValueController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SegmentValueDto>>> GetAll([FromQuery] Guid segmentTypeId, CancellationToken cancellationToken)
    {
        var values = await _unitOfWork.SegmentValues.FindAsync(x => x.SegmentTypeId == segmentTypeId, cancellationToken);
        return Ok(values.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SegmentValueDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var value = await _unitOfWork.SegmentValues.GetByIdAsync(id, cancellationToken);
        if (value == null)
            return NotFound();

        return Ok(MapToDto(value));
    }

    [HttpPost]
    public async Task<ActionResult<SegmentValueDto>> Create([FromBody] CreateSegmentValueRequest request, CancellationToken cancellationToken)
    {
        var value = new SegmentValue(
            request.SegmentTypeId,
            request.CompanyId,
            request.Value,
            request.Description,
            request.DisplayOrder);

        await _unitOfWork.SegmentValues.AddAsync(value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(SegmentValue),
            value.Id,
            "system",
            newValues: new { request.Value, request.Description },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = value.Id }, MapToDto(value));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SegmentValueDto>> Update(Guid id, [FromBody] UpdateSegmentValueRequest request, CancellationToken cancellationToken)
    {
        var value = await _unitOfWork.SegmentValues.GetByIdAsync(id, cancellationToken);
        if (value == null)
            return NotFound();

        value.Update(request.Value, request.Description, request.DisplayOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(value));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var value = await _unitOfWork.SegmentValues.GetByIdAsync(id, cancellationToken);
        if (value == null)
            return NotFound();

        value.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static SegmentValueDto MapToDto(SegmentValue value)
    {
        return new SegmentValueDto(
            value.Id,
            value.SegmentTypeId,
            value.CompanyId,
            value.Value,
            value.Description,
            value.DisplayOrder,
            value.IsActive,
            value.CreatedOn,
            value.ModifiedOn);
    }
}
