// <copyright file="SegmentTypeController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/segment-types")]
public class SegmentTypeController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public SegmentTypeController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SegmentTypeDto>>> GetAll([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var types = await _unitOfWork.SegmentTypes.FindAsync(x => x.CompanyId == companyId, cancellationToken);
        return Ok(types.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SegmentTypeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var type = await _unitOfWork.SegmentTypes.GetByIdAsync(id, cancellationToken);
        if (type == null)
            return NotFound();

        return Ok(MapToDto(type));
    }

    [HttpPost]
    public async Task<ActionResult<SegmentTypeDto>> Create([FromBody] CreateSegmentTypeRequest request, CancellationToken cancellationToken)
    {
        var type = new SegmentType(
            request.CompanyId,
            request.Name,
            request.Code,
            request.DisplayOrder,
            request.IsRequired);

        await _unitOfWork.SegmentTypes.AddAsync(type, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(SegmentType),
            type.Id,
            "system",
            newValues: new { request.Name, request.Code },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = type.Id }, MapToDto(type));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SegmentTypeDto>> Update(Guid id, [FromBody] UpdateSegmentTypeRequest request, CancellationToken cancellationToken)
    {
        var type = await _unitOfWork.SegmentTypes.GetByIdAsync(id, cancellationToken);
        if (type == null)
            return NotFound();

        type.Update(request.Name, request.Code, request.DisplayOrder, request.IsRequired);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(type));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var type = await _unitOfWork.SegmentTypes.GetByIdAsync(id, cancellationToken);
        if (type == null)
            return NotFound();

        type.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static SegmentTypeDto MapToDto(SegmentType type)
    {
        return new SegmentTypeDto(
            type.Id,
            type.CompanyId,
            type.Name,
            type.Code,
            type.DisplayOrder,
            type.IsRequired,
            type.IsActive,
            type.CreatedOn,
            type.ModifiedOn);
    }
}
