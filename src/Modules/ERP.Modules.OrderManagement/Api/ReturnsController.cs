// <copyright file="ReturnsController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.OrderManagement.Domain.Entities;
using ERP.Modules.OrderManagement.Infrastructure;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.OrderManagement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/om/returns")]
public class ReturnsController : ControllerBase
{
    private readonly OmDbContext _context;

    public ReturnsController(OmDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ReturnSummary>>>> GetAllAsync(
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        var query = _context.Returns.AsNoTracking();

        query = query.ApplyCompanyScope(HttpContext, r => r.CompanyId, companyId);

        var list = await query
            .OrderByDescending(r => r.ReturnDate)
            .Select(r => new ReturnSummary(
                r.Id,
                r.ReturnNumber,
                r.CompanyId,
                r.CustomerId,
                r.ShipmentId,
                r.SalesOrderId,
                r.ReturnDate,
                r.Status,
                r.Lines.Sum(l => (l.Quantity * l.UnitPrice) * ((1m - (l.DiscountPercent / 100m)) * (1m + (l.TaxPercent / 100m)))),
                r.Lines.Sum(l => l.Quantity * l.UnitPrice),
                r.IsApproved))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<ReturnSummary>>.Success(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReturnDetail>>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var returnEntity = await _context.Returns
            .AsNoTracking()
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (returnEntity is null)
            return NotFound(ApiResponse<ReturnDetail>.Failure(new[] { $"Return {id} not found." }));

        var detail = new ReturnDetail(
            returnEntity.Id,
            returnEntity.ReturnNumber,
            returnEntity.CompanyId,
            returnEntity.CustomerId,
            returnEntity.ShipmentId,
            returnEntity.SalesOrderId,
            returnEntity.ReturnDate,
            returnEntity.ReasonCode,
            returnEntity.Note,
            returnEntity.Status,
            returnEntity.IsApproved,
            returnEntity.RejectionReason,
            returnEntity.GetReturnValue(),
            returnEntity.Lines.Select(l => new ReturnLineSummary(
                l.Id,
                l.LineNumber,
                l.ItemId,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.UnitOfMeasure,
                l.WarehouseId,
                l.ShipmentLineId,
                l.SalesOrderLineId,
                l.AccountId,
                l.DiscountPercent,
                l.TaxPercent,
                l.RestockDisposition,
                l.LineTotal)).ToList());

        return Ok(ApiResponse<ReturnDetail>.Success(detail));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsync(
        [FromBody] CreateReturnRequest request,
        CancellationToken cancellationToken)
    {
        var returnEntity = new Return(
            request.ReturnNumber,
            request.CompanyId,
            request.CustomerId,
            request.ShipmentId,
            request.SalesOrderId,
            request.ReturnDate,
            request.ReasonCode,
            request.Note);

        foreach (var line in request.Lines)
        {
            returnEntity.AddLine(new ReturnLine(
                returnEntity.Id,
                line.LineNumber,
                line.ItemId,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.UnitOfMeasure,
                line.WarehouseId,
                line.ShipmentLineId,
                line.SalesOrderLineId,
                line.AccountId,
                line.DiscountPercent,
                line.TaxPercent,
                line.RestockDisposition));
        }

        _context.Returns.Add(returnEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<Guid>.Success(returnEntity.Id));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var returnEntity = await _context.Returns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (returnEntity is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Return {id} not found." }));

        try
        {
            returnEntity.Confirm();

            // Saving dispatches ReturnConfirmedEvent, consumed by Inventory (restock) and AR (credit memo -> GL).
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Confirmed"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }

    // ----- RMA value/approval workflow -----
    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse<string>>> SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken)
    {
        var returnEntity = await _context.Returns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (returnEntity is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Return {id} not found." }));

        try
        {
            returnEntity.SubmitForApproval();
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("PendingApproval"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var returnEntity = await _context.Returns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (returnEntity is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Return {id} not found." }));

        try
        {
            returnEntity.Approve(User.Identity?.Name ?? "system");
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Approved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> RejectAsync(Guid id, [FromBody] RejectReturnRequest request, CancellationToken cancellationToken)
    {
        var returnEntity = await _context.Returns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (returnEntity is null)
            return NotFound(ApiResponse<string>.Failure(new[] { $"Return {id} not found." }));

        try
        {
            returnEntity.Reject(request.Reason);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(ApiResponse<string>.Success("Rejected"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.Failure(new[] { ex.Message }));
        }
    }
}

public record CreateReturnRequest(
    string ReturnNumber,
    Guid CompanyId,
    Guid CustomerId,
    Guid? ShipmentId,
    Guid? SalesOrderId,
    DateTime ReturnDate,
    string? ReasonCode,
    string? Note,
    List<CreateReturnLineRequest> Lines);

public record CreateReturnLineRequest(
    int LineNumber,
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string UnitOfMeasure,
    Guid? WarehouseId,
    Guid? ShipmentLineId,
    Guid? SalesOrderLineId,
    Guid? AccountId,
    decimal DiscountPercent,
    decimal TaxPercent,
    string? RestockDisposition);

public record ReturnSummary(
    Guid Id,
    string ReturnNumber,
    Guid CompanyId,
    Guid CustomerId,
    Guid? ShipmentId,
    Guid? SalesOrderId,
    DateTime ReturnDate,
    ReturnStatus Status,
    decimal TotalAmount,
    decimal ReturnValue,
    bool IsApproved);

public record ReturnLineSummary(
    Guid Id,
    int LineNumber,
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string UnitOfMeasure,
    Guid? WarehouseId,
    Guid? ShipmentLineId,
    Guid? SalesOrderLineId,
    Guid? AccountId,
    decimal DiscountPercent,
    decimal TaxPercent,
    string? RestockDisposition,
    decimal LineTotal);

public record ReturnDetail(
    Guid Id,
    string ReturnNumber,
    Guid CompanyId,
    Guid CustomerId,
    Guid? ShipmentId,
    Guid? SalesOrderId,
    DateTime ReturnDate,
    string? ReasonCode,
    string? Note,
    ReturnStatus Status,
    bool IsApproved,
    string? RejectionReason,
    decimal ReturnValue,
    List<ReturnLineSummary> Lines);

public record RejectReturnRequest(string Reason);
