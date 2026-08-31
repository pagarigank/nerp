// <copyright file="CompanyController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/companies")]
[Authorize(Policy = "CompanyAdminOrSuper")]
public class CompanyController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUser;

    public CompanyController(IUnitOfWork unitOfWork, IAuditLogService auditLogService, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompanyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var companies = await _unitOfWork.Companies.GetAllAsync(cancellationToken);

        // A company admin only sees their own company; a super admin sees all.
        if (!_currentUser.IsSuperAdmin)
        {
            var allowed = _currentUser.CompanyIds;
            companies = companies.Where(c => allowed.Contains(c.Id)).ToList();
        }

        return Ok(companies.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Minimal, anonymous-safe list (Id + Name only) used by the public
    /// "Request Access" registration page so a prospective user can pick the
    /// company they want access to. No sensitive company data is exposed.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PublicCompanyDto>>> GetPublicList(CancellationToken cancellationToken)
    {
        var companies = await _unitOfWork.Companies.GetAllAsync(cancellationToken);
        return Ok(companies
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new PublicCompanyDto(c.Id, c.Name))
            .ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id, cancellationToken);
        if (company == null)
            return NotFound();

        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(company.Id))
            return Forbid();

        return Ok(MapToDto(company));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin)
            return Forbid();

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
            _currentUser.UserId ?? "system",
            newValues: new { request.Name, request.LegalName, request.BaseCurrency, request.ParentCompanyId },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = company.Id }, MapToDto(company));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(id))
            return Forbid();

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
            _currentUser.UserId ?? "system",
            oldValues: oldValues,
            newValues: new { request.Name, request.LegalName, request.BaseCurrency, request.ParentCompanyId },
            cancellationToken: cancellationToken);

        return Ok(MapToDto(company));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsSuperAdmin && !_currentUser.CompanyIds.Contains(id))
            return Forbid();

        var company = await _unitOfWork.Companies.GetByIdAsync(id, cancellationToken);
        if (company == null)
            return NotFound();

        company.MarkDeleted(_currentUser.UserId ?? "system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Deleted",
            nameof(Company),
            company.Id,
            _currentUser.UserId ?? "system",
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

public record PublicCompanyDto(Guid Id, string Name);
