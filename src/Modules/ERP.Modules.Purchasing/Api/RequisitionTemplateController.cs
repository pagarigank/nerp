// <copyright file="RequisitionTemplateController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/requisition-templates")]
public class RequisitionTemplateController : ControllerBase
{
    private readonly ERP.Modules.Purchasing.Infrastructure.IRepository<RequisitionTemplate> _repository;
    private readonly PurchasingDbContext _context;
    private readonly ERP.Modules.Purchasing.Infrastructure.IUnitOfWork _unitOfWork;

    public RequisitionTemplateController(
        ERP.Modules.Purchasing.Infrastructure.IRepository<RequisitionTemplate> repository,
        PurchasingDbContext context,
        ERP.Modules.Purchasing.Infrastructure.IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RequisitionTemplateDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _context.RequisitionTemplates.AsQueryable();

        if (companyId.HasValue)
            query = ERP.Modules.Platform.Infrastructure.CompanyScope.ApplyCompanyScope(query, HttpContext, t => t.CompanyId, companyId);

        if (activeOnly == true)
            query = query.Where(t => t.IsActive);

        var templates = await query.ToListAsync(cancellationToken);

        var dtos = templates.Select(t => new RequisitionTemplateDto
        {
            Id = t.Id,
            TemplateCode = t.TemplateCode,
            TemplateName = t.TemplateName,
            CompanyId = t.CompanyId,
            Description = t.Description,
            IsActive = t.IsActive,
            LineCount = t.Lines.Count,
        }).ToList();

        return Ok(ApiResponse<List<RequisitionTemplateDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RequisitionTemplateDetailDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var template = await _context.RequisitionTemplates
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template == null)
            return NotFound(ApiResponse<RequisitionTemplateDetailDto>.Failure(["Template not found."]));

        var dto = new RequisitionTemplateDetailDto
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            CompanyId = template.CompanyId,
            Description = template.Description,
            IsActive = template.IsActive,
            Lines = template.Lines.Select(l => new RequisitionTemplateLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ItemId = l.ItemId,
                Description = l.Description,
                DefaultQuantity = l.DefaultQuantity,
                UnitOfMeasure = l.UnitOfMeasure,
                AccountId = l.AccountId,
                ProjectId = l.ProjectId,
            }).ToList(),
        };

        return Ok(ApiResponse<RequisitionTemplateDetailDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RequisitionTemplateDto>>> Create(
        [FromBody] CreateRequisitionTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = new RequisitionTemplate(
            request.TemplateCode,
            request.TemplateName,
            request.CompanyId,
            request.Description,
            request.IsActive);

        foreach (var lineRequest in request.Lines)
        {
            var line = new RequisitionTemplateLine(
                template.Id,
                lineRequest.LineNumber,
                lineRequest.ItemId,
                lineRequest.Description,
                lineRequest.DefaultQuantity,
                lineRequest.UnitOfMeasure,
                lineRequest.AccountId,
                lineRequest.ProjectId);

            template.AddLine(line);
        }

        await _repository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new RequisitionTemplateDto
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            CompanyId = template.CompanyId,
            Description = template.Description,
            IsActive = template.IsActive,
            LineCount = template.Lines.Count,
        };

        return CreatedAtAction(nameof(GetById), new { id = template.Id }, ApiResponse<RequisitionTemplateDto>.Success(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RequisitionTemplateDto>>> Update(
        Guid id,
        [FromBody] UpdateRequisitionTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(id, cancellationToken);

        if (template == null)
            return NotFound(ApiResponse<RequisitionTemplateDto>.Failure(["Template not found."]));

        template.UpdateDescription(request.Description);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                template.Activate();
            else
                template.Deactivate();
        }

        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new RequisitionTemplateDto
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            CompanyId = template.CompanyId,
            Description = template.Description,
            IsActive = template.IsActive,
            LineCount = template.Lines.Count,
        };

        return Ok(ApiResponse<RequisitionTemplateDto>.Success(dto));
    }
}

public class RequisitionTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int LineCount { get; set; }
}

public class RequisitionTemplateDetailDto : RequisitionTemplateDto
{
    public List<RequisitionTemplateLineDto> Lines { get; set; } = [];
}

public class RequisitionTemplateLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DefaultQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public Guid? AccountId { get; set; }
    public Guid? ProjectId { get; set; }
}

public class CreateRequisitionTemplateRequest
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateRequisitionTemplateLineRequest> Lines { get; set; } = [];
}

public class CreateRequisitionTemplateLineRequest
{
    public int LineNumber { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal DefaultQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public Guid? AccountId { get; set; }
    public Guid? ProjectId { get; set; }
}

public class UpdateRequisitionTemplateRequest
{
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
