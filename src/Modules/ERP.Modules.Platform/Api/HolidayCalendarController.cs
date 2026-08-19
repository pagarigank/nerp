// <copyright file="HolidayCalendarController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/platform/holiday-calendar")]
public class HolidayCalendarController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public HolidayCalendarController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HolidayCalendarDto>>> GetAll([FromQuery] Guid companyId, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var entries = await _unitOfWork.HolidayCalendars.FindAsync(h => h.CompanyId == companyId, cancellationToken);
        if (year.HasValue)
            entries = entries.Where(h => h.Date.Year == year.Value).ToList();
        return Ok(entries.OrderBy(h => h.Date).Select(MapToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<HolidayCalendarDto>> Create([FromBody] CreateHolidayCalendarRequest request, CancellationToken cancellationToken)
    {
        var entry = new HolidayCalendar(request.CompanyId, request.Date, request.Description, request.IsWorkingDay);
        await _unitOfWork.HolidayCalendars.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = entry.Id }, MapToDto(entry));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HolidayCalendarDto>> Update(Guid id, [FromBody] UpdateHolidayCalendarRequest request, CancellationToken cancellationToken)
    {
        var entry = await _unitOfWork.HolidayCalendars.GetByIdAsync(id, cancellationToken);
        if (entry == null)
            return NotFound();
        entry.Update(request.Description, request.IsWorkingDay);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _unitOfWork.HolidayCalendars.GetByIdAsync(id, cancellationToken);
        if (entry == null)
            return NotFound();
        entry.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Advance a date by <paramref name="businessDays"/> working days, skipping holidays (and
    /// explicit working-day overrides). Used by payroll pay-date, PO delivery and AR dunning date math.</summary>
    [HttpGet("advance")]
    public async Task<ActionResult<DateOnly>> Advance([FromQuery] Guid companyId, [FromQuery] string from, [FromQuery] int businessDays, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(from, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
            return BadRequest("Invalid 'from' date.");
        if (businessDays < 0)
            return BadRequest("businessDays must be >= 0.");

        var holidays = (await _unitOfWork.HolidayCalendars.FindAsync(h => h.CompanyId == companyId, cancellationToken))
            .ToDictionary(h => h.Date, h => h.IsWorkingDay);

        var current = start;
        var remaining = businessDays;
        while (remaining > 0)
        {
            current = current.AddDays(1);
            var isHoliday = holidays.TryGetValue(current, out var isWorkingDayOverride);
            var working = isHoliday ? isWorkingDayOverride : IsWeekend(current);
            if (!working)
                remaining--;
        }

        return Ok(current);
    }

    private static bool IsWeekend(DateOnly d) =>
        d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static HolidayCalendarDto MapToDto(HolidayCalendar h) => new(
        h.Id, h.CompanyId, h.Date, h.Description, h.IsWorkingDay, h.CreatedOn, h.ModifiedOn);
}

public record HolidayCalendarDto(
    Guid Id, Guid CompanyId, DateOnly Date, string Description, bool IsWorkingDay, DateTimeOffset CreatedOn, DateTimeOffset? ModifiedOn);

public record CreateHolidayCalendarRequest(Guid CompanyId, DateOnly Date, string Description, bool IsWorkingDay = false);

public record UpdateHolidayCalendarRequest(string Description, bool IsWorkingDay);
