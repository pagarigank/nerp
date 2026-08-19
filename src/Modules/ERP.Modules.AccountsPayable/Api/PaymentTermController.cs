// <copyright file="PaymentTermController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using Asp.Versioning;
using ERP.Modules.AccountsPayable.Api;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.AccountsPayable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap/payment-terms")]
public class PaymentTermController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentTermController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentTermDto>>> GetAll([FromQuery] bool? activeOnly, CancellationToken cancellationToken)
    {
        Expression<Func<PaymentTerm, bool>> predicate = activeOnly.GetValueOrDefault()
            ? x => x.IsActive
            : x => true;

        var terms = await _unitOfWork.PaymentTerms.FindAsync(predicate, cancellationToken);
        return Ok(terms.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentTermDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var term = await _unitOfWork.PaymentTerms.GetByIdAsync(id, cancellationToken);
        if (term == null)
            return NotFound();

        return Ok(MapToDto(term));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentTermDto>> Create([FromBody] CreatePaymentTermRequest request, CancellationToken cancellationToken)
    {
        var term = new PaymentTerm(request.Name, request.DueDays, request.DiscountDays, request.DiscountPercent);
        await _unitOfWork.PaymentTerms.AddAsync(term, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = term.Id }, MapToDto(term));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PaymentTermDto>> Update(Guid id, [FromBody] UpdatePaymentTermRequest request, CancellationToken cancellationToken)
    {
        var term = await _unitOfWork.PaymentTerms.GetByIdAsync(id, cancellationToken);
        if (term == null)
            return NotFound();

        term.Update(request.Name, request.DueDays, request.DiscountDays, request.DiscountPercent);
        _unitOfWork.PaymentTerms.Update(term);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(MapToDto(term));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var term = await _unitOfWork.PaymentTerms.GetByIdAsync(id, cancellationToken);
        if (term == null)
            return NotFound();

        term.Activate();
        _unitOfWork.PaymentTerms.Update(term);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var term = await _unitOfWork.PaymentTerms.GetByIdAsync(id, cancellationToken);
        if (term == null)
            return NotFound();

        term.Deactivate();
        _unitOfWork.PaymentTerms.Update(term);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static PaymentTermDto MapToDto(PaymentTerm term)
    {
        return new PaymentTermDto(term.Id, term.Name, term.DueDays, term.DiscountDays, term.DiscountPercent, term.IsActive, term.CreatedOn, term.ModifiedOn);
    }
}