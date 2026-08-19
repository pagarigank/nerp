// <copyright file="BudgetController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.GeneralLedger.Domain.Entities;
using ERP.Modules.GeneralLedger.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.GeneralLedger.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gl/budgets")]
public class BudgetController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public BudgetController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> GetAll([FromQuery] Guid? companyId, [FromQuery] Guid? fiscalYearId, CancellationToken cancellationToken)
    {
        var budgets = await _unitOfWork.Budgets.FindAsync(
            x => (!companyId.HasValue || x.CompanyId == companyId.Value)
                && (!fiscalYearId.HasValue || x.FiscalYearId == fiscalYearId.Value),
            cancellationToken);

        return Ok(budgets.Select(MapToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var budget = await _unitOfWork.Budgets.GetByIdAsync(id, cancellationToken);
        if (budget == null)
            return NotFound();

        return Ok(MapToDto(budget));
    }

    [HttpPost]
    public async Task<ActionResult<BudgetDto>> Create([FromBody] CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = new Budget(
            request.CompanyId,
            request.FiscalYearId,
            request.Name,
            request.Description,
            request.BudgetType);

        await _unitOfWork.Budgets.AddAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = budget.Id }, MapToDto(budget));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<BudgetDto>> AddLine(Guid id, [FromBody] AddBudgetLineRequest request, CancellationToken cancellationToken)
    {
        var budget = await _unitOfWork.Budgets.GetByIdAsync(id, cancellationToken);
        if (budget == null)
            return NotFound();

        budget.AddLine(request.AccountId, request.PeriodNumber, request.Amount, request.ProjectId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(budget));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var budget = await _unitOfWork.Budgets.GetByIdAsync(id, cancellationToken);
        if (budget == null)
            return NotFound();

        budget.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static BudgetDto MapToDto(Budget budget)
    {
        var lines = budget.Lines
            .Select(l => new BudgetLineDto(
                l.Id,
                l.AccountId,
                l.PeriodNumber,
                l.Amount,
                l.ProjectId))
            .ToList();

        return new BudgetDto(
            budget.Id,
            budget.CompanyId,
            budget.FiscalYearId,
            budget.Name,
            budget.Description,
            budget.BudgetType,
            budget.IsActive,
            budget.Lines.Sum(l => l.Amount),
            lines);
    }
}
