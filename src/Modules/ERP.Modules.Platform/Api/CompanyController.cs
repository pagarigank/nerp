// <copyright file="CompanyController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/companies")]
public class CompanyController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public CompanyController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompanyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var companies = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        return Ok(companies.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id, cancellationToken);
        if (company == null)
            return NotFound();

        return Ok(MapToDto(company));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = new Company(
            request.Name,
            request.LegalName,
            request.BaseCurrency,
            request.TaxId,
            request.Address,
            request.ParentCompanyId);

        await _unitOfWork.Companies.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(Company),
            company.Id,
            "system",
            newValues: new { request.Name, request.LegalName, request.BaseCurrency, request.ParentCompanyId },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = company.Id }, MapToDto(company));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id, cancellationToken);
        if (company == null)
            return NotFound();

        var oldValues = new { company.Name, company.LegalName, company.BaseCurrency, company.ParentCompanyId };

        company.Update(request.Name, request.LegalName, request.BaseCurrency, request.TaxId, request.Address);
        if (request.ParentCompanyId.HasValue)
        {
            company.SetParentCompany(request.ParentCompanyId.Value);
        }
        else if (request.ParentCompanyId == null && company.ParentCompanyId.HasValue)
        {
            company.SetParentCompany(null);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Updated",
            nameof(Company),
            company.Id,
            "system",
            oldValues: oldValues,
            newValues: new { request.Name, request.LegalName, request.BaseCurrency, request.ParentCompanyId },
            cancellationToken: cancellationToken);

        return Ok(MapToDto(company));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id, cancellationToken);
        if (company == null)
            return NotFound();

        company.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Deleted",
            nameof(Company),
            company.Id,
            "system",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    private static CompanyDto MapToDto(Company company)
    {
        return new CompanyDto(
            company.Id,
            company.Name,
            company.LegalName,
            company.BaseCurrency,
            company.TaxId,
            company.Address,
            company.ParentCompanyId,
            company.IsActive,
            company.CreatedOn,
            company.ModifiedOn);
    }
}
