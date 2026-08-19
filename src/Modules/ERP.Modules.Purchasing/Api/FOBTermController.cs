// <copyright file="FOBTermController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/fob-terms")]
public class FOBTermController : ControllerBase
{
    private readonly IRepository<FOBTerm> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public FOBTermController(IRepository<FOBTerm> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FOBTermDto>>>> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var terms = await _repository.GetAllAsync(cancellationToken);

        if (activeOnly == true)
            terms = terms.Where(t => t.IsActive).ToList();

        var dtos = terms.Select(t => new FOBTermDto
        {
            Id = t.Id,
            Code = t.Code,
            Description = t.Description,
            FreightResponsibility = t.FreightResponsibility,
            RiskTransferPoint = t.RiskTransferPoint,
            IsActive = t.IsActive,
        }).ToList();

        return Ok(ApiResponse<List<FOBTermDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FOBTermDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var term = await _repository.GetByIdAsync(id, cancellationToken);

        if (term == null)
            return NotFound(ApiResponse<FOBTermDto>.Failure(["FOB term not found."]));

        var dto = new FOBTermDto
        {
            Id = term.Id,
            Code = term.Code,
            Description = term.Description,
            FreightResponsibility = term.FreightResponsibility,
            RiskTransferPoint = term.RiskTransferPoint,
            IsActive = term.IsActive,
        };

        return Ok(ApiResponse<FOBTermDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FOBTermDto>>> Create(
        [FromBody] CreateFOBTermRequest request,
        CancellationToken cancellationToken)
    {
        var term = new FOBTerm(
            request.Code,
            request.Description,
            request.FreightResponsibility,
            request.RiskTransferPoint,
            request.IsActive);

        await _repository.AddAsync(term, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new FOBTermDto
        {
            Id = term.Id,
            Code = term.Code,
            Description = term.Description,
            FreightResponsibility = term.FreightResponsibility,
            RiskTransferPoint = term.RiskTransferPoint,
            IsActive = term.IsActive,
        };

        return CreatedAtAction(nameof(GetById), new { id = term.Id }, ApiResponse<FOBTermDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<FOBTermDto>>> Update(
        Guid id,
        [FromBody] UpdateFOBTermRequest request,
        CancellationToken cancellationToken)
    {
        var term = await _repository.GetByIdAsync(id, cancellationToken);

        if (term == null)
            return NotFound(ApiResponse<FOBTermDto>.Failure(["FOB term not found."]));

        term.UpdateDescription(request.Description);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                term.Activate();
            else
                term.Deactivate();
        }

        _repository.Update(term);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new FOBTermDto
        {
            Id = term.Id,
            Code = term.Code,
            Description = term.Description,
            FreightResponsibility = term.FreightResponsibility,
            RiskTransferPoint = term.RiskTransferPoint,
            IsActive = term.IsActive,
        };

        return Ok(ApiResponse<FOBTermDto>.Success(dto));
    }
}

public class FOBTermDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FreightResponsibility { get; set; } = string.Empty;
    public string RiskTransferPoint { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateFOBTermRequest
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FreightResponsibility { get; set; } = string.Empty;
    public string RiskTransferPoint { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateFOBTermRequest
{
    public string Description { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}
