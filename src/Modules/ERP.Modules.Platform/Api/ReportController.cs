// <copyright file="ReportController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Api;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/reports")]
public class ReportController : ControllerBase
{
    private readonly PlatformDbContext _context;

    public ReportController(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet("company-setup")]
    public async Task<ActionResult<CompanySetupReportDto>> GetCompanySetupReport(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, cancellationToken);

        if (company == null)
            return NotFound();

        var fiscalYears = await _context.FiscalYears
            .Where(fy => fy.CompanyId == companyId && !fy.DeletedOn.HasValue)
            .OrderBy(fy => fy.Year)
            .ToListAsync(cancellationToken);

        var segmentTypes = await _context.SegmentTypes
            .Where(st => st.CompanyId == companyId && !st.DeletedOn.HasValue)
            .OrderBy(st => st.DisplayOrder)
            .ToListAsync(cancellationToken);

        var currencies = await _context.Currencies
            .Where(c => !c.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var numberSequences = await _context.NumberSequences
            .Where(ns => ns.CompanyId == companyId && !ns.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var users = await _context.Users
            .Where(u => !u.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var roles = await _context.Roles
            .Where(r => !r.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        return Ok(new CompanySetupReportDto(
            new CompanyDto(company.Id, company.Name, company.LegalName, company.BaseCurrency, company.TaxId, company.Address, company.ParentCompanyId, company.IsActive, company.CreatedOn, company.ModifiedOn),
            fiscalYears.Select(fy => new FiscalYearDto(fy.Id, fy.CompanyId, fy.Year, fy.Description, fy.StartDate, fy.EndDate, fy.IsClosed, fy.CalendarType, fy.YearEndType, fy.CreatedOn, fy.ModifiedOn)).ToList(),
            segmentTypes.Select(st => new SegmentTypeDto(st.Id, st.CompanyId, st.Name, st.Code, st.DisplayOrder, st.IsRequired, st.IsActive, st.CreatedOn, st.ModifiedOn)).ToList(),
            currencies.Select(c => new CurrencyDto(c.Id, c.Code, c.Name, c.Symbol, c.DecimalPlaces, c.IsActive, c.CreatedOn, c.ModifiedOn)).ToList(),
            numberSequences.Select(ns => new NumberSequenceDto(ns.Id, ns.CompanyId, ns.Name, ns.Prefix, ns.NextValue, ns.Increment, ns.MinValue, ns.MaxValue, ns.IsActive, ns.CreatedOn, ns.ModifiedOn)).ToList(),
            users.Select(u => new UserDto(u.Id, u.Username, u.Email, u.DisplayName, u.PhoneNumber, u.IsActive, u.LastLoginAt, u.CreatedOn, u.ModifiedOn, Array.Empty<UserRoleAssignmentDto>())).ToList(),
            roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsActive, r.CreatedOn, r.ModifiedOn)).ToList(),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("chart-of-accounts")]
    public async Task<ActionResult<ChartOfAccountsReportDto>> GetChartOfAccountsReport(
        [FromQuery] Guid companyId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, cancellationToken);

        if (company == null)
            return NotFound();

        var accounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId && (!activeOnly || a.IsActive) && !a.DeletedOn.HasValue)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync(cancellationToken);

        var segmentTypes = await _context.SegmentTypes
            .Where(st => st.CompanyId == companyId && !st.DeletedOn.HasValue)
            .OrderBy(st => st.DisplayOrder)
            .ToListAsync(cancellationToken);

        return Ok(new ChartOfAccountsReportDto(
            companyId,
            company.Name,
            accounts.Select(a => new AccountDto(a.Id, a.CompanyId, a.AccountNumber, a.Description, a.AccountType, a.NormalBalance, a.IsActive, a.CreatedOn, a.ModifiedOn)).ToList(),
            segmentTypes.Select(st => new SegmentTypeDto(st.Id, st.CompanyId, st.Name, st.Code, st.DisplayOrder, st.IsRequired, st.IsActive, st.CreatedOn, st.ModifiedOn)).ToList(),
            DateTimeOffset.UtcNow));
    }

    [HttpGet("fiscal-calendar")]
    public async Task<ActionResult<FiscalCalendarReportDto>> GetFiscalCalendarReport(
        [FromQuery] Guid companyId,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, cancellationToken);

        if (company == null)
            return NotFound();

        var query = _context.FiscalYears
            .Where(fy => fy.CompanyId == companyId && !fy.DeletedOn.HasValue);

        if (year.HasValue)
            query = query.Where(fy => fy.Year == year.Value);

        var fiscalYears = await query
            .OrderBy(fy => fy.Year)
            .ToListAsync(cancellationToken);

        var result = new List<FiscalYearWithPeriodsDto>();

        foreach (var fy in fiscalYears)
        {
            var periods = await _context.FiscalPeriods
                .Where(fp => fp.FiscalYearId == fy.Id && !fp.DeletedOn.HasValue)
                .OrderBy(fp => fp.PeriodNumber)
                .ToListAsync(cancellationToken);

            result.Add(new FiscalYearWithPeriodsDto(
                new FiscalYearDto(fy.Id, fy.CompanyId, fy.Year, fy.Description, fy.StartDate, fy.EndDate, fy.IsClosed, fy.CalendarType, fy.YearEndType, fy.CreatedOn, fy.ModifiedOn),
                periods.Select(p => new FiscalPeriodDto(p.Id, p.FiscalYearId, p.CompanyId, p.PeriodNumber, p.Description, p.StartDate, p.EndDate, p.Status, p.CreatedOn, p.ModifiedOn)).ToList()));
        }

        return Ok(new FiscalCalendarReportDto(companyId, company.Name, result, DateTimeOffset.UtcNow));
    }

    [HttpGet("security-matrix")]
    public async Task<ActionResult<SecurityMatrixReportDto>> GetSecurityMatrixReport(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, cancellationToken);

        if (company == null)
            return NotFound();

        var roles = await _context.Roles
            .Where(r => !r.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var users = await _context.Users
            .Where(u => !u.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        var permissions = await _context.Permissions
            .ToListAsync(cancellationToken);

        var rolePermissionsList = await _context.RolePermissions
            .ToListAsync(cancellationToken);

        var userRolesList = await _context.UserRoles
            .ToListAsync(cancellationToken);

        var rolePermissions = roles
            .Select(r => new RolePermissionDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsActive,
                permissions
                    .Where(p => rolePermissionsList.Any(rp => rp.RoleId == r.Id && rp.PermissionId == p.Id))
                    .Select(p => new PermissionDto(p.Id, p.Module, p.Action, p.Description))
                    .ToList()))
            .ToList();

        var userRoles = users
            .Select(u => new UserRoleDto(
                u.Id,
                u.Username,
                u.Email,
                u.DisplayName,
                u.IsActive,
                userRolesList.Where(ur => ur.UserId == u.Id).Select(ur => ur.RoleId.ToString()).ToList()))
            .ToList();

        return Ok(new SecurityMatrixReportDto(
            companyId,
            company.Name,
            roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsActive, r.CreatedOn, r.ModifiedOn)).ToList(),
            users.Select(u => new UserDto(u.Id, u.Username, u.Email, u.DisplayName, u.PhoneNumber, u.IsActive, u.LastLoginAt, u.CreatedOn, u.ModifiedOn, Array.Empty<UserRoleAssignmentDto>())).ToList(),
            permissions.Select(p => new PermissionDto(p.Id, p.Module, p.Action, p.Description)).ToList(),
            rolePermissions,
            userRoles,
            DateTimeOffset.UtcNow));
    }

    [HttpGet("audit-trail")]
    public async Task<ActionResult<AuditTrailReportDto>> GetAuditTrailReport(
        [FromQuery] Guid companyId,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, cancellationToken);

        if (company == null)
            return NotFound();

        var query = _context.AuditLogs.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(al => al.PerformedOn >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(al => al.PerformedOn <= toDate.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(al => al.EntityType == entityType);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(al => al.Action == action);

        var totalCount = await query.CountAsync(cancellationToken);

        var auditLogs = await query
            .OrderByDescending(al => al.PerformedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new AuditTrailReportDto(
            companyId,
            company.Name,
            totalCount,
            page,
            pageSize,
            auditLogs.Select(al => new AuditLogDto(al.Id, al.Action, al.EntityType, al.EntityId, al.PerformedBy, al.PerformedOn, al.IpAddress, al.UserAgent, al.CorrelationId, al.OldValues, al.NewValues)).ToList(),
            DateTimeOffset.UtcNow));
    }
}