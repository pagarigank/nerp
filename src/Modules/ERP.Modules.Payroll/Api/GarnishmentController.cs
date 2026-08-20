// <copyright file="GarnishmentController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

[ApiController]
[Route("api/v1/payroll/garnishments")]
public class GarnishmentController : ControllerBase
{
    private readonly PayrollDbContext _context;

    public GarnishmentController(PayrollDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateGarnishmentRequest request, CancellationToken cancellationToken)
    {
        var garnishment = new Garnishment(
            request.CompanyId, request.EmployeeId, request.Type, request.DisposableIncomePercent,
            request.FixedAmount, request.ArrearsWeeks, request.CaseNumber);
        _context.Garnishments.Add(garnishment);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(garnishment.Id));
    }

    [HttpGet("employee/{employeeId:guid}")]
    public async Task<ActionResult<ApiResponse<List<GarnishmentDto>>>> GetForEmployee(
        Guid employeeId, CancellationToken cancellationToken)
    {
        var list = await _context.Garnishments
            .Where(g => g.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
        var ordered = list
            .OrderBy(g => g.Priority)
            .Select(g => new GarnishmentDto
            {
                Id = g.Id,
                EmployeeId = g.EmployeeId,
                Type = g.Type.ToString(),
                Priority = g.Priority,
                DisposableIncomePercent = g.DisposableIncomePercent,
                FixedAmount = g.FixedAmount,
                ArrearsWeeks = g.ArrearsWeeks,
                CaseNumber = g.CaseNumber,
                IsActive = g.IsActive,
            }).ToList();
        return Ok(ApiResponse<List<GarnishmentDto>>.Success(ordered));
    }

    /// <summary>
    /// Computes the CCPA-compliant garnishment deductions for an employee given disposable income.
    /// Applies priority stacking: higher-priority orders are satisfied first, and the aggregate
    /// cannot exceed the disposable-income cap for the highest-priority order type.
    /// </summary>
    [HttpPost("employee/{employeeId:guid}/compute")]
    public async Task<ActionResult<ApiResponse<GarnishmentComputationDto>>> Compute(
        Guid employeeId, [FromBody] ComputeGarnishmentRequest request, CancellationToken cancellationToken)
    {
        var orders = await _context.Garnishments
            .Where(g => g.EmployeeId == employeeId && g.IsActive)
            .ToListAsync(cancellationToken);
        var orderedOrders = orders.OrderBy(g => g.Priority).ToList();

        var remaining = request.DisposableIncome;
        var results = new List<GarnishmentLineResultDto>();
        decimal totalWithheld = 0m;

        foreach (var order in orderedOrders)
        {
            if (remaining <= 0m)
            {
                results.Add(new GarnishmentLineResultDto
                {
                    Id = order.Id,
                    Type = order.Type.ToString(),
                    Priority = order.Priority,
                    Requested = order.ComputeAllowedAmount(request.DisposableIncome),
                    Withheld = 0m,
                    Reason = "Disposable income exhausted by higher-priority orders",
                });
                continue;
            }

            // The aggregate cap is driven by the highest-priority type present.
            var capFraction = order.CcpaCapFraction();
            var aggregateCap = request.DisposableIncome * capFraction;
            var availableCap = Math.Max(0m, aggregateCap - totalWithheld);
            var orderAllowed = order.ComputeAllowedAmount(remaining);
            var withheld = Math.Min(orderAllowed, availableCap);

            totalWithheld += withheld;
            remaining -= withheld;

            results.Add(new GarnishmentLineResultDto
            {
                Id = order.Id,
                Type = order.Type.ToString(),
                Priority = order.Priority,
                Requested = orderAllowed,
                Withheld = withheld,
                Reason = withheld < orderAllowed ? "Limited by CCPA aggregate disposable-income cap" : "Fully withheld",
            });
        }

        return Ok(ApiResponse<GarnishmentComputationDto>.Success(new GarnishmentComputationDto
        {
            EmployeeId = employeeId,
            DisposableIncome = request.DisposableIncome,
            TotalWithheld = totalWithheld,
            NetAfterGarnishment = request.DisposableIncome - totalWithheld,
            Lines = results,
        }));
    }

    [HttpPost("{id:guid}/terminate")]
    public async Task<ActionResult<ApiResponse>> Terminate(Guid id, CancellationToken cancellationToken)
    {
        var garnishment = await _context.Garnishments.FindAsync(new object[] { id }, cancellationToken);
        if (garnishment is null)
            return NotFound(ApiResponse.Failure(new[] { "Garnishment not found." }, 404));
        garnishment.Terminate();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }
}

public class CreateGarnishmentRequest
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public GarnishmentType Type { get; set; }
    public decimal DisposableIncomePercent { get; set; }
    public decimal? FixedAmount { get; set; }
    public int? ArrearsWeeks { get; set; }
    public string? CaseNumber { get; set; }
}

public class ComputeGarnishmentRequest
{
    public decimal DisposableIncome { get; set; }
}

public class GarnishmentDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Priority { get; set; }
    public decimal DisposableIncomePercent { get; set; }
    public decimal? FixedAmount { get; set; }
    public int? ArrearsWeeks { get; set; }
    public string? CaseNumber { get; set; }
    public bool IsActive { get; set; }
}

public class GarnishmentComputationDto
{
    public Guid EmployeeId { get; set; }
    public decimal DisposableIncome { get; set; }
    public decimal TotalWithheld { get; set; }
    public decimal NetAfterGarnishment { get; set; }
    public List<GarnishmentLineResultDto> Lines { get; set; } = [];
}

public class GarnishmentLineResultDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Priority { get; set; }
    public decimal Requested { get; set; }
    public decimal Withheld { get; set; }
    public string Reason { get; set; } = string.Empty;
}
