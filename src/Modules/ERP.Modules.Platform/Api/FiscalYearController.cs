// <copyright file="FiscalYearController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Globalization;
using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/fiscal-years")]
public class FiscalYearController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public FiscalYearController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FiscalYearDto>>> GetAll([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var years = await _unitOfWork.FiscalYears.FindAsync(x => x.CompanyId == companyId, cancellationToken);
        return Ok(years.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FiscalYearDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var year = await _unitOfWork.FiscalYears.GetByIdAsync(id, cancellationToken);
        if (year == null)
            return NotFound();

        return Ok(MapToDto(year));
    }

    [HttpPost]
    public async Task<ActionResult<FiscalYearDto>> Create([FromBody] CreateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        var year = new FiscalYear(
            request.CompanyId,
            request.Year,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.CalendarType,
            request.YearEndType);

        await _unitOfWork.FiscalYears.AddAsync(year, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(FiscalYear),
            year.Id,
            "system",
            newValues: new { request.Year, request.Description },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = year.Id }, MapToDto(year));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FiscalYearDto>> Update(Guid id, [FromBody] UpdateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        var year = await _unitOfWork.FiscalYears.GetByIdAsync(id, cancellationToken);
        if (year == null)
            return NotFound();

        year.Update(request.Description, request.StartDate, request.EndDate, request.CalendarType, request.YearEndType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(year));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        var year = await _unitOfWork.FiscalYears.GetByIdAsync(id, cancellationToken);
        if (year == null)
            return NotFound();

        year.Close();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Closed",
            nameof(FiscalYear),
            year.Id,
            "system",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken cancellationToken)
    {
        var year = await _unitOfWork.FiscalYears.GetByIdAsync(id, cancellationToken);
        if (year == null)
            return NotFound();

        year.Reopen();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Reopened",
            nameof(FiscalYear),
            year.Id,
            "system",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    private static FiscalYearDto MapToDto(FiscalYear year)
    {
        return new FiscalYearDto(
            year.Id,
            year.CompanyId,
            year.Year,
            year.Description,
            year.StartDate,
            year.EndDate,
            year.IsClosed,
            year.CalendarType,
            year.YearEndType,
            year.CreatedOn,
            year.ModifiedOn);
    }

    /// <summary>
    /// Generate fiscal periods for a fiscal year according to its calendar type.
    /// <list type="bullet">
    ///   <item><description><see cref="FiscalCalendarType.Standard"/>: 12 monthly periods.</description></item>
    ///   <item><description><see cref="FiscalCalendarType.Period13"/>: 4-4-5 quarters → 13 periods (the 13th is a 4-week period).</description></item>
    ///   <item><description><see cref="FiscalCalendarType.FourFourFive"/>: strict 4-4-5 (4,4,5 weeks per quarter).</description></item>
    /// </list>
    /// Idempotent: skips period numbers that already exist for the year. Periods are
    /// bounded by the fiscal year start/end dates.
    /// </summary>
    [HttpPost("{id:guid}/generate-periods")]
    public async Task<ActionResult<IReadOnlyList<FiscalPeriodDto>>> GeneratePeriods(Guid id, CancellationToken cancellationToken)
    {
        var year = await _unitOfWork.FiscalYears.GetByIdAsync(id, cancellationToken);
        if (year == null)
            return NotFound();

        var existing = await _unitOfWork.FiscalPeriods.FindAsync(p => p.FiscalYearId == id, cancellationToken);
        var existingNumbers = new HashSet<int>(existing.Select(p => p.PeriodNumber));

        var plan = BuildPeriodPlan(year);
        var created = new List<FiscalPeriodDto>();

        foreach (var p in plan)
        {
            if (existingNumbers.Contains(p.PeriodNumber))
                continue;
            var period = new FiscalPeriod(year.Id, year.CompanyId, p.PeriodNumber, p.Description, p.Start, p.End);
            await _unitOfWork.FiscalPeriods.AddAsync(period, cancellationToken);
            created.Add(new FiscalPeriodDto(period.Id, period.FiscalYearId, period.CompanyId, period.PeriodNumber, period.Description, period.StartDate, period.EndDate, period.Status, period.CreatedOn, period.ModifiedOn));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "GeneratedPeriods",
            nameof(FiscalYear),
            year.Id,
            "system",
            newValues: new { year.CalendarType, Count = created.Count },
            cancellationToken: cancellationToken);

        var all = await _unitOfWork.FiscalPeriods.FindAsync(p => p.FiscalYearId == id, cancellationToken);
        return Ok(all.OrderBy(p => p.PeriodNumber).Select(p =>
            new FiscalPeriodDto(p.Id, p.FiscalYearId, p.CompanyId, p.PeriodNumber, p.Description, p.StartDate, p.EndDate, p.Status, p.CreatedOn, p.ModifiedOn)).ToList());
    }

    private static List<(int PeriodNumber, string Description, DateTimeOffset Start, DateTimeOffset End)> BuildPeriodPlan(FiscalYear year)
    {
        var start = year.StartDate.Date;
        var end = year.EndDate.Date;
        var plan = new List<(int, string, DateTimeOffset, DateTimeOffset)>();

        if (year.CalendarType == FiscalCalendarType.Standard)
        {
            for (var i = 0; i < 12; i++)
            {
                var ps = start.AddMonths(i);
                var pe = ps.AddMonths(1).AddDays(-1);
                if (pe > end)
                    pe = end;
                plan.Add((i + 1, ps.ToString("MMMM yyyy", CultureInfo.InvariantCulture), ps, pe));
            }

            return plan;
        }

        // 4-4-5 / 13-period: weeks are 7 days; quarter pattern [4,4,5] weeks.
        var week = 0;
        var pattern = new[] { 4, 4, 5 };
        for (var q = 0; q < 4; q++)
        {
            foreach (var weeks in pattern)
            {
                var ps = start.AddDays(week * 7);
                var pe = ps.AddDays((weeks * 7) - 1);
                if (pe > end)
                    pe = end;
                week += weeks;
                plan.Add((plan.Count + 1, $"P{plan.Count + 1} (Q{q + 1})", ps, pe));
                if (pe >= end)
                    return plan;
            }
        }

        return plan;
    }
}
