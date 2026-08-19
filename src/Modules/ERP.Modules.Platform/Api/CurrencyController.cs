// <copyright file="CurrencyController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.Platform.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/platform/currencies")]
public class CurrencyController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public CurrencyController(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CurrencyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var currencies = await _unitOfWork.Currencies.GetAllAsync(cancellationToken);
        return Ok(currencies.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CurrencyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currency = await _unitOfWork.Currencies.GetByIdAsync(id, cancellationToken);
        if (currency == null)
            return NotFound();

        return Ok(MapToDto(currency));
    }

    [HttpPost]
    public async Task<ActionResult<CurrencyDto>> Create([FromBody] CreateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var currency = new Currency(
            request.Code,
            request.Name,
            request.Symbol,
            request.DecimalPlaces);

        await _unitOfWork.Currencies.AddAsync(currency, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "Created",
            nameof(Currency),
            currency.Id,
            "system",
            newValues: new { request.Code, request.Name },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = currency.Id }, MapToDto(currency));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CurrencyDto>> Update(Guid id, [FromBody] UpdateCurrencyRequest request, CancellationToken cancellationToken)
    {
        var currency = await _unitOfWork.Currencies.GetByIdAsync(id, cancellationToken);
        if (currency == null)
            return NotFound();

        currency.Update(request.Name, request.Symbol, request.DecimalPlaces);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(currency));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currency = await _unitOfWork.Currencies.GetByIdAsync(id, cancellationToken);
        if (currency == null)
            return NotFound();

        currency.MarkDeleted("system");
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static CurrencyDto MapToDto(Currency currency)
    {
        return new CurrencyDto(
            currency.Id,
            currency.Code,
            currency.Name,
            currency.Symbol,
            currency.DecimalPlaces,
            currency.IsActive,
            currency.CreatedOn,
            currency.ModifiedOn);
    }
}
