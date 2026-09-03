// <copyright file="ReportController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using Asp.Versioning;
using ERP.Modules.Platform.Api;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Reporting;
using ERP.Shared.Kernel.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

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

    private static CompanyDocumentHeader MkHeader(string company, string title, string number) =>
        new(company, null, null, null, "USD", null, null, null, title, number, DateTimeOffset.UtcNow);

    // Data-fetching helpers (shared by JSON + PDF endpoints)
    private async Task<CompanySetupReportDto> GetCompanySetupDataAsync(Guid companyId, CancellationToken ct)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var fiscalYears = await _context.FiscalYears
            .Where(fy => fy.CompanyId == companyId && !fy.DeletedOn.HasValue)
            .OrderBy(fy => fy.Year)
            .ToListAsync(ct);

        var segmentTypes = await _context.SegmentTypes
            .Where(st => st.CompanyId == companyId && !st.DeletedOn.HasValue)
            .OrderBy(st => st.DisplayOrder)
            .ToListAsync(ct);

        var currencies = await _context.Currencies
            .Where(c => !c.DeletedOn.HasValue)
            .ToListAsync(ct);

        var numberSequences = await _context.NumberSequences
            .Where(ns => ns.CompanyId == companyId && !ns.DeletedOn.HasValue)
            .ToListAsync(ct);

        var users = await _context.Users
            .Where(u => !u.DeletedOn.HasValue)
            .ToListAsync(ct);

        var roles = await _context.Roles
            .Where(r => !r.DeletedOn.HasValue)
            .ToListAsync(ct);

        return new CompanySetupReportDto(
            new CompanyDto(company.Id, company.Name, company.LegalName, company.BaseCurrency, company.TaxId, company.Address, company.ParentCompanyId, company.IsActive, company.CreatedOn, company.ModifiedOn),
            fiscalYears.Select(fy => new FiscalYearDto(fy.Id, fy.CompanyId, fy.Year, fy.Description, fy.StartDate, fy.EndDate, fy.IsClosed, fy.CalendarType, fy.YearEndType, fy.CreatedOn, fy.ModifiedOn)).ToList(),
            segmentTypes.Select(st => new SegmentTypeDto(st.Id, st.CompanyId, st.Name, st.Code, st.DisplayOrder, st.IsRequired, st.IsActive, st.CreatedOn, st.ModifiedOn)).ToList(),
            currencies.Select(c => new CurrencyDto(c.Id, c.Code, c.Name, c.Symbol, c.DecimalPlaces, c.IsActive, c.CreatedOn, c.ModifiedOn)).ToList(),
            numberSequences.Select(ns => new NumberSequenceDto(ns.Id, ns.CompanyId, ns.Name, ns.Prefix, ns.NextValue, ns.Increment, ns.MinValue, ns.MaxValue, ns.IsActive, ns.CreatedOn, ns.ModifiedOn)).ToList(),
            users.Select(u => new UserDto(u.Id, u.Username, u.Email, u.DisplayName, u.PhoneNumber, u.IsActive, u.LastLoginAt, u.CreatedOn, u.ModifiedOn, Array.Empty<UserRoleAssignmentDto>())).ToList(),
            roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsActive, r.CreatedOn, r.ModifiedOn, Array.Empty<PermissionDto>())).ToList(),
            DateTimeOffset.UtcNow);
    }

    private async Task<ChartOfAccountsReportDto> GetChartOfAccountsDataAsync(Guid companyId, bool activeOnly, CancellationToken ct)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var accounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId && (!activeOnly || a.IsActive) && !a.DeletedOn.HasValue)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync(ct);

        var segmentTypes = await _context.SegmentTypes
            .Where(st => st.CompanyId == companyId && !st.DeletedOn.HasValue)
            .OrderBy(st => st.DisplayOrder)
            .ToListAsync(ct);

        return new ChartOfAccountsReportDto(
            companyId,
            company.Name,
            accounts.Select(a => new AccountDto(a.Id, a.CompanyId, a.AccountNumber, a.Description, a.AccountType, a.NormalBalance, a.IsActive, a.CreatedOn, a.ModifiedOn)).ToList(),
            segmentTypes.Select(st => new SegmentTypeDto(st.Id, st.CompanyId, st.Name, st.Code, st.DisplayOrder, st.IsRequired, st.IsActive, st.CreatedOn, st.ModifiedOn)).ToList(),
            DateTimeOffset.UtcNow);
    }

    private async Task<FiscalCalendarReportDto> GetFiscalCalendarDataAsync(Guid companyId, int? year, CancellationToken ct)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var query = _context.FiscalYears
            .Where(fy => fy.CompanyId == companyId && !fy.DeletedOn.HasValue);

        if (year.HasValue)
            query = query.Where(fy => fy.Year == year.Value);

        var fiscalYears = await query
            .OrderBy(fy => fy.Year)
            .ToListAsync(ct);

        var result = new List<FiscalYearWithPeriodsDto>();

        foreach (var fy in fiscalYears)
        {
            var periods = await _context.FiscalPeriods
                .Where(fp => fp.FiscalYearId == fy.Id && !fp.DeletedOn.HasValue)
                .OrderBy(fp => fp.PeriodNumber)
                .ToListAsync(ct);

            result.Add(new FiscalYearWithPeriodsDto(
                new FiscalYearDto(fy.Id, fy.CompanyId, fy.Year, fy.Description, fy.StartDate, fy.EndDate, fy.IsClosed, fy.CalendarType, fy.YearEndType, fy.CreatedOn, fy.ModifiedOn),
                periods.Select(p => new FiscalPeriodDto(p.Id, p.FiscalYearId, p.CompanyId, p.PeriodNumber, p.Description, p.StartDate, p.EndDate, p.Status, p.CreatedOn, p.ModifiedOn)).ToList()));
        }

        return new FiscalCalendarReportDto(companyId, company.Name, result, DateTimeOffset.UtcNow);
    }

    private async Task<SecurityMatrixReportDto> GetSecurityMatrixDataAsync(Guid companyId, CancellationToken ct)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var roles = await _context.Roles
            .Where(r => !r.DeletedOn.HasValue)
            .ToListAsync(ct);

        var users = await _context.Users
            .Where(u => !u.DeletedOn.HasValue)
            .ToListAsync(ct);

        var permissions = await _context.Permissions
            .ToListAsync(ct);

        var rolePermissionsList = await _context.RolePermissions
            .ToListAsync(ct);

        var userRolesList = await _context.UserRoles
            .ToListAsync(ct);

        var rolePermissions = roles
            .Select(r => new RolePermissionDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsActive,
                permissions
                    .Where(p => rolePermissionsList.Any(rp => rp.RoleId == r.Id && rp.PermissionId == p.Id))
                    .Select(p => new PermissionDto(p.Id, p.Module, p.Page, p.Action, p.Code, p.Description))
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

        return new SecurityMatrixReportDto(
            companyId,
            company.Name,
            roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsActive, r.CreatedOn, r.ModifiedOn, Array.Empty<PermissionDto>())).ToList(),
            users.Select(u => new UserDto(u.Id, u.Username, u.Email, u.DisplayName, u.PhoneNumber, u.IsActive, u.LastLoginAt, u.CreatedOn, u.ModifiedOn, Array.Empty<UserRoleAssignmentDto>())).ToList(),
            permissions.Select(p => new PermissionDto(p.Id, p.Module, p.Page, p.Action, p.Code, p.Description)).ToList(),
            rolePermissions,
            userRoles,
            DateTimeOffset.UtcNow);
    }

    private async Task<AuditTrailReportDto> GetAuditTrailDataAsync(Guid companyId, DateTimeOffset? fromDate, DateTimeOffset? toDate, string? entityType, string? action, int page, int pageSize, CancellationToken ct)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.DeletedOn.HasValue, ct);
        if (company == null)
            return null!;

        var query = _context.AuditLogs.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(al => al.PerformedOn >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(al => al.PerformedOn <= toDate.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(al => al.EntityType == entityType);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(al => al.Action == action);

        var totalCount = await query.CountAsync(ct);

        var auditLogs = await query
            .OrderByDescending(al => al.PerformedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new AuditTrailReportDto(
            companyId,
            company.Name,
            totalCount,
            page,
            pageSize,
            auditLogs.Select(al => new AuditLogDto(al.Id, al.Action, al.EntityType, al.EntityId, al.PerformedBy, al.PerformedOn, al.IpAddress, al.UserAgent, al.CorrelationId, al.OldValues, al.NewValues)).ToList(),
            DateTimeOffset.UtcNow);
    }

    // JSON endpoints
    [HttpGet("company-setup")]
    public async Task<ActionResult<CompanySetupReportDto>> GetCompanySetupReport(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken)
    {
        return Ok(await GetCompanySetupDataAsync(companyId, cancellationToken));
    }

    [HttpGet("chart-of-accounts")]
    public async Task<ActionResult<ChartOfAccountsReportDto>> GetChartOfAccountsReport(
        [FromQuery] Guid companyId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetChartOfAccountsDataAsync(companyId, activeOnly, cancellationToken));
    }

    [HttpGet("fiscal-calendar")]
    public async Task<ActionResult<FiscalCalendarReportDto>> GetFiscalCalendarReport(
        [FromQuery] Guid companyId,
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetFiscalCalendarDataAsync(companyId, year, cancellationToken));
    }

    [HttpGet("security-matrix")]
    public async Task<ActionResult<SecurityMatrixReportDto>> GetSecurityMatrixReport(
        [FromQuery] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await GetSecurityMatrixDataAsync(companyId, cancellationToken));
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
        return Ok(await GetAuditTrailDataAsync(companyId, fromDate, toDate, entityType, action, page, pageSize, cancellationToken));
    }

    // PDF endpoints
    [HttpGet("company-setup/pdf")]
    public async Task<ActionResult> CompanySetupPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var data = await GetCompanySetupDataAsync(companyId, ct);
        if (data == null) return NotFound();
        var header = MkHeader(data.Company.Name, "Company Setup", "Setup");
        var tableRows = new[] {
            new[] { "Name", data.Company.Name },
            new[] { "Legal Name", data.Company.LegalName ?? "—" },
            new[] { "Currency", data.Company.BaseCurrency },
            new[] { "Tax ID", data.Company.TaxId ?? "—" },
            new[] { "Address", data.Company.Address ?? "—" },
            new[] { "Active", data.Company.IsActive ? "Yes" : "No" },
            new[] { "Fiscal Years", data.FiscalYears.Count.ToString(CultureInfo.InvariantCulture) },
            new[] { "Users", data.Users.Count.ToString(CultureInfo.InvariantCulture) },
            new[] { "Roles", data.Roles.Count.ToString(CultureInfo.InvariantCulture) },
        };
        var doc = new ReportDocument(header, "Company Setup", $"Generated {data.GeneratedOn:yyyy-MM-dd}",
            new (string[], string[][])[] { (new[] { "Field", "Value" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "CompanySetup.pdf");
    }

    [HttpGet("chart-of-accounts/pdf")]
    public async Task<ActionResult> ChartOfAccountsPdfAsync([FromQuery] Guid companyId, [FromQuery] bool activeOnly, CancellationToken ct)
    {
        var data = await GetChartOfAccountsDataAsync(companyId, activeOnly, ct);
        if (data == null) return NotFound();
        var header = MkHeader(data.CompanyName, "Chart of Accounts", "COA");
        var tableRows = data.Accounts.Select(a => new[]
        {
            a.AccountNumber, a.Description, a.AccountType.ToString(),
            a.NormalBalance.ToString(), a.IsActive ? "Active" : "Inactive",
        }).ToArray();
        var doc = new ReportDocument(header, "Chart of Accounts", $"{data.CompanyName} — {tableRows.Length} accounts",
            new (string[], string[][])[] { (new[] { "Account #", "Description", "Type", "Normal Balance", "Status" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "ChartOfAccounts.pdf");
    }

    [HttpGet("fiscal-calendar/pdf")]
    public async Task<ActionResult> FiscalCalendarPdfAsync([FromQuery] Guid companyId, [FromQuery] int? year, CancellationToken ct)
    {
        var data = await GetFiscalCalendarDataAsync(companyId, year, ct);
        if (data == null) return NotFound();
        var header = MkHeader(data.CompanyName, "Fiscal Calendar", "Calendar");
        var tableRows = data.FiscalYears.SelectMany(fy => fy.Periods.Select(p => new[]
        {
            $"FY {fy.FiscalYear.Year}", $"P{p.PeriodNumber}", p.Description,
            p.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            p.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            p.Status.ToString(),
        })).ToArray();
        var doc = new ReportDocument(header, "Fiscal Calendar", $"{data.CompanyName}",
            new (string[], string[][])[] { (new[] { "Year", "Period", "Description", "Start", "End", "Status" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "FiscalCalendar.pdf");
    }

    [HttpGet("audit-trail/pdf")]
    public async Task<ActionResult> AuditTrailPdfAsync([FromQuery] Guid companyId, [FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate, CancellationToken ct)
    {
        var data = await GetAuditTrailDataAsync(companyId, fromDate, toDate, null, null, 1, 100, ct);
        if (data == null) return NotFound();
        var header = MkHeader(data.CompanyName, "Audit Trail", "Audit");
        var tableRows = data.Entries.Select(l => new[]
        {
            l.PerformedOn.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            l.Action, l.EntityType,
            l.EntityId.ToString().Substring(0, 8),
            l.PerformedBy ?? "—",
        }).ToArray();
        var doc = new ReportDocument(header, "Audit Trail", $"{data.CompanyName} — {data.TotalCount} entries",
            new (string[], string[][])[] { (new[] { "Date", "Action", "Entity", "Entity ID", "Performed By" }, tableRows) }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "AuditTrail.pdf");
    }

    [HttpGet("security-matrix/pdf")]
    public async Task<ActionResult> SecurityMatrixPdfAsync([FromQuery] Guid companyId, CancellationToken ct)
    {
        var data = await GetSecurityMatrixDataAsync(companyId, ct);
        if (data == null) return NotFound();
        var header = MkHeader(data.CompanyName, "Security Matrix", "Security");
        var roleRows = data.RolePermissions.Select(rp => new[]
        {
            rp.RoleName, rp.IsActive ? "Active" : "Inactive",
            rp.Permissions.Count.ToString(CultureInfo.InvariantCulture),
        }).ToArray();
        var userRows = data.UserRoles.Select(ur => new[]
        {
            ur.UserName, ur.DisplayName ?? "—", ur.IsActive ? "Active" : "Inactive",
            ur.Roles.Count.ToString(CultureInfo.InvariantCulture),
        }).ToArray();
        var doc = new ReportDocument(header, "Security Matrix", $"{data.CompanyName} — {data.Roles.Count} roles, {data.Users.Count} users",
            new (string[], string[][])[]
            {
                (new[] { "Role", "Status", "Permissions" }, roleRows),
                (new[] { "User", "Display Name", "Status", "Roles" }, userRows),
            }, null, null);
        return File(DocumentGenerator.GeneratePdf(doc), "application/pdf", "SecurityMatrix.pdf");
    }
}
