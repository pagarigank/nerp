// <copyright file="POTemplateController.cs" company="ERP Project">
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
[Route("api/v1/purchasing/po-templates")]
public class POTemplateController : ControllerBase
{
    private readonly ERP.Modules.Purchasing.Infrastructure.IRepository<PurchaseOrderTemplate> _repository;
    private readonly PurchasingDbContext _context;
    private readonly ERP.Modules.Purchasing.Infrastructure.IUnitOfWork _unitOfWork;

    public POTemplateController(
        ERP.Modules.Purchasing.Infrastructure.IRepository<PurchaseOrderTemplate> repository,
        PurchasingDbContext context,
        ERP.Modules.Purchasing.Infrastructure.IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<POTemplateDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? vendorId,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrderTemplates.AsQueryable();

        if (companyId.HasValue)
            query = ERP.Modules.Platform.Infrastructure.CompanyScope.ApplyCompanyScope(query, HttpContext, t => t.CompanyId, companyId);

        if (vendorId.HasValue)
            query = query.Where(t => t.VendorId == vendorId.Value);

        if (activeOnly == true)
            query = query.Where(t => t.IsActive);

        var templates = await query.ToListAsync(cancellationToken);

        var dtos = templates.Select(t => new POTemplateDto
        {
            Id = t.Id,
            TemplateCode = t.TemplateCode,
            TemplateName = t.TemplateName,
            CompanyId = t.CompanyId,
            VendorId = t.VendorId,
            OrderType = t.OrderType.ToString(),
            BlanketAmount = t.BlanketAmount,
            AmountUsed = t.AmountUsed,
            RemainingAmount = t.GetRemainingAmount(),
            IsActive = t.IsActive,
            IsExpired = t.IsExpired(),
        }).ToList();

        return Ok(ApiResponse<List<POTemplateDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<POTemplateDetailDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var template = await _context.PurchaseOrderTemplates
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template == null)
            return NotFound(ApiResponse<POTemplateDetailDto>.Failure(["Template not found."]));

        var dto = new POTemplateDetailDto
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            CompanyId = template.CompanyId,
            VendorId = template.VendorId,
            OrderType = template.OrderType.ToString(),
            Description = template.Description,
            BlanketAmount = template.BlanketAmount,
            AmountUsed = template.AmountUsed,
            RemainingAmount = template.GetRemainingAmount(),
            EffectiveDate = template.EffectiveDate,
            ExpirationDate = template.ExpirationDate,
            IsActive = template.IsActive,
            IsExpired = template.IsExpired(),
            Lines = template.Lines.Select(l => new POTemplateLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ItemId = l.ItemId,
                Description = l.Description,
                DefaultQuantity = l.DefaultQuantity,
                UnitOfMeasure = l.UnitOfMeasure,
                UnitPrice = l.UnitPrice,
                AccountId = l.AccountId,
                ProjectId = l.ProjectId,
            }).ToList(),
        };

        return Ok(ApiResponse<POTemplateDetailDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<POTemplateDto>>> Create(
        [FromBody] CreatePOTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var template = new PurchaseOrderTemplate(
            request.TemplateCode,
            request.TemplateName,
            request.CompanyId,
            request.VendorId,
            request.OrderType,
            request.Description,
            request.BlanketAmount,
            request.EffectiveDate,
            request.ExpirationDate,
            request.IsActive);

        foreach (var lineRequest in request.Lines)
        {
            var line = new PurchaseOrderTemplateLine(
                template.Id,
                lineRequest.LineNumber,
                lineRequest.ItemId,
                lineRequest.Description,
                lineRequest.DefaultQuantity,
                lineRequest.UnitOfMeasure,
                lineRequest.UnitPrice,
                lineRequest.AccountId,
                lineRequest.ProjectId);

            template.AddLine(line);
        }

        await _repository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new POTemplateDto
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            TemplateName = template.TemplateName,
            CompanyId = template.CompanyId,
            VendorId = template.VendorId,
            OrderType = template.OrderType.ToString(),
            BlanketAmount = template.BlanketAmount,
            AmountUsed = template.AmountUsed,
            RemainingAmount = template.GetRemainingAmount(),
            IsActive = template.IsActive,
            IsExpired = template.IsExpired(),
        };

        return CreatedAtAction(nameof(GetById), new { id = template.Id }, ApiResponse<POTemplateDto>.Success(dto));
    }

    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<ApiResponse<POTemplateDto>>> RecordRelease(
        Guid id,
        [FromBody] ReleaseAmountRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(id, cancellationToken);

        if (template == null)
            return NotFound(ApiResponse<POTemplateDto>.Failure(["Template not found."]));

        try
        {
            template.RecordRelease(request.Amount);
            _repository.Update(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = new POTemplateDto
            {
                Id = template.Id,
                TemplateCode = template.TemplateCode,
                TemplateName = template.TemplateName,
                CompanyId = template.CompanyId,
                VendorId = template.VendorId,
                OrderType = template.OrderType.ToString(),
                BlanketAmount = template.BlanketAmount,
                AmountUsed = template.AmountUsed,
                RemainingAmount = template.GetRemainingAmount(),
                IsActive = template.IsActive,
                IsExpired = template.IsExpired(),
            };

            return Ok(ApiResponse<POTemplateDto>.Success(dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<POTemplateDto>.Failure([ex.Message], 400));
        }
    }
}

public class POTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public decimal? BlanketAmount { get; set; }
    public decimal AmountUsed { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
}

public class POTemplateDetailDto : POTemplateDto
{
    public string? Description { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public List<POTemplateLineDto> Lines { get; set; } = [];
}

public class POTemplateLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? DefaultQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? ProjectId { get; set; }
}

public class CreatePOTemplateRequest
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public PurchaseOrderType OrderType { get; set; }
    public string? Description { get; set; }
    public decimal? BlanketAmount { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreatePOTemplateLineRequest> Lines { get; set; } = [];
}

public class CreatePOTemplateLineRequest
{
    public int LineNumber { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? DefaultQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? ProjectId { get; set; }
}

public class ReleaseAmountRequest
{
    public decimal Amount { get; set; }
}
