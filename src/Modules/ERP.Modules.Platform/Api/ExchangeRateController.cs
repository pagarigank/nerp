// <copyright file="ExchangeRateController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/exchange-rates")]
#pragma warning disable S6960
public class ExchangeRateController : ControllerBase
#pragma warning restore S6960
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly PlatformDbContext _context;

    public ExchangeRateController(IUnitOfWork unitOfWork, IAuditLogService auditLogService, PlatformDbContext context)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExchangeRateDto>>> GetAll([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var rates = await _context.ExchangeRates
            .AsNoTracking()
            .ApplyCompanyScope(HttpContext, r => r.CompanyId, companyId)
            .ToListAsync(cancellationToken);
        return Ok(rates.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExchangeRateDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rate = await _unitOfWork.ExchangeRates.GetByIdAsync(id, cancellationToken);
        if (rate == null)
            return NotFound();

        return Ok(MapToDto(rate));
    }

    [HttpPost]
    public async Task<ActionResult<ExchangeRateDto>> Create([FromBody] CreateExchangeRateRequest request, CancellationToken cancellationToken)
    {
        var rate = new ExchangeRate(
            request.CompanyId,
            request.FromCurrency,
            request.ToCurrency,
            request.Rate,
            request.EffectiveDate);

        await _unitOfWork.ExchangeRates.AddAsync(rate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(ExchangeRate),
            rate.Id,
            "system",
            newValues: new { request.FromCurrency, request.ToCurrency, request.Rate },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = rate.Id }, MapToDto(rate));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExchangeRateDto>> Update(Guid id, [FromBody] UpdateExchangeRateRequest request, CancellationToken cancellationToken)
    {
        var rate = await _unitOfWork.ExchangeRates.GetByIdAsync(id, cancellationToken);
        if (rate == null)
            return NotFound();

        rate.Update(request.Rate, request.EffectiveDate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(rate));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var rate = await _unitOfWork.ExchangeRates.GetByIdAsync(id, cancellationToken);
        if (rate == null)
            return NotFound();

        rate.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static ExchangeRateDto MapToDto(ExchangeRate rate)
    {
        return new ExchangeRateDto(
            rate.Id,
            rate.CompanyId,
            rate.FromCurrency,
            rate.ToCurrency,
            rate.Rate,
            rate.EffectiveDate,
            rate.CreatedOn,
            rate.ModifiedOn);
    }
}
