// <copyright file="SubcontractController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Domain.Events;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.ProjectAccounting.Api;

#pragma warning disable S6960 // Controller actions should be grouped logically
[ApiController]
[Route("api/v1/project-accounting/subcontracts")]
public class SubcontractController : ControllerBase
{
    private readonly ProjDbContext _context;
    private readonly IProjUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public SubcontractController(ProjDbContext context, IProjUnitOfWork unitOfWork, IDomainEventDispatcher eventDispatcher)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Subcontract>>>> GetAll([FromQuery] Guid projectId, CancellationToken ct)
        => Ok(ApiResponse<List<Subcontract>>.Success(await _context.Subcontracts
            .ApplyCompanyScope(HttpContext, s => s.CompanyId)
            .Where(s => s.ProjectId == projectId)
            .ToListAsync(ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Subcontract>>> GetById(Guid id, CancellationToken ct)
    {
        var s = await _context.Subcontracts
            .Include(x => x.ChangeOrders).Include(x => x.Invoices)
            .Include(x => x.Compliance).Include(x => x.LienWaivers)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return s is null ? NotFound(ApiResponse.Failure(new[] { "Subcontract not found." }, 404)) : Ok(ApiResponse<Subcontract>.Success(s));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateSubcontractRequest r, CancellationToken ct)
    {
        var s = new Subcontract(r.CompanyId, r.ProjectId, r.TaskId, r.VendorId, r.SubcontractNumber, r.ContractAmount, r.RetainagePercentage, r.Scope, r.PayWhenPaid);
        _context.Subcontracts.Add(s);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(s.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdateSubcontractRequest r, CancellationToken ct)
    {
        var s = await _context.Subcontracts.FindAsync(new object[] { id }, ct);
        if (s is null)
            return NotFound(ApiResponse.Failure(new[] { "Subcontract not found." }, 404));
        s.Update(r.ContractAmount, r.RetainagePercentage, r.Scope, r.PayWhenPaid);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success());
    }

    // Change orders
    [HttpPost("{id:guid}/change-orders")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddChangeOrder(Guid id, [FromBody] SubCoRequest r, CancellationToken ct)
    {
        var s = await _context.Subcontracts.FindAsync(new object[] { id }, ct);
        if (s is null)
            return NotFound(ApiResponse.Failure(new[] { "Subcontract not found." }, 404));
        var co = s.AddChangeOrder(r.Description, r.Amount, r.Reason);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(co.Id));
    }

    [HttpPost("change-orders/{coId:guid}/approve")]
    public async Task<ActionResult<ApiResponse>> ApproveChangeOrder(Guid coId, CancellationToken ct)
    {
        var co = await _context.SubcontractChangeOrders.FindAsync(new object[] { coId }, ct);
        if (co is null)
            return NotFound(ApiResponse.Failure(new[] { "Change order not found." }, 404));
        co.Approve();
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success());
    }

    // Invoices (validate against contract, calculate retainage, post to project ledger + GL)
    [HttpPost("{id:guid}/invoices")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddInvoice(Guid id, [FromBody] SubInvoiceRequest r, CancellationToken ct)
    {
        var s = await _context.Subcontracts.FindAsync(new object[] { id }, ct);
        if (s is null)
            return NotFound(ApiResponse.Failure(new[] { "Subcontract not found." }, 404));
        if (s.BilledToDate + r.Amount > s.ContractAmount + s.ChangeOrders.Where(c => c.Status == SubcontractCoStatus.Approved).Sum(c => c.Amount))
            return BadRequest(ApiResponse.Failure(new[] { "Invoice exceeds subcontract contract amount." }));

        var inv = s.AddInvoice(r.InvoiceNumber, r.Amount, r.InvoiceDate, r.RetainageRate, r.Description);
        _context.SubcontractInvoices.Add(inv);

        // Post subcontract cost to the project ledger + dual-post to GL.
        var project = await _context.Projects.FindAsync(new object[] { s.ProjectId }, ct);
        if (project is not null)
        {
            var task = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.ProjectId == s.ProjectId, ct);

            // Retainage portion is held; billable = net of retainage.
            var net = r.Amount - inv.RetainageAmount;
            var txn = new CostTransaction(
                project.CompanyId,
                s.ProjectId,
                task?.Id ?? Guid.Empty,
                CostCategory.Subcontract,
                CostTransactionType.SubcontractInvoice,
                net,
                0,
                $"Sub invoice {r.InvoiceNumber}",
                s.VendorId,
                $"SUBC-{inv.Id:N}");
            _context.CostTransactions.Add(txn);
            await _unitOfWork.SaveChangesAsync(ct);
            await _eventDispatcher.DispatchAsync(
                new ProjectCostPostedEvent(txn.Id, txn.ProjectId, txn.TaskId, txn.Category.ToString(), txn.Amount, txn.CompanyId), ct);
        }
        else
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<Guid>.Success(inv.Id));
    }

    [HttpPost("invoices/{invoiceId:guid}/pay")]
    public async Task<ActionResult<ApiResponse>> MarkInvoicePaid(Guid invoiceId, CancellationToken ct)
    {
        var inv = await _context.SubcontractInvoices.FindAsync(new object[] { invoiceId }, ct);
        if (inv is null)
            return NotFound(ApiResponse.Failure(new[] { "Invoice not found." }, 404));
        inv.MarkPaid();
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success());
    }

    // Compliance
    [HttpPost("{id:guid}/compliance")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddCompliance(Guid id, [FromBody] SubComplianceRequest r, CancellationToken ct)
    {
        var s = await _context.Subcontracts.FindAsync(new object[] { id }, ct);
        if (s is null)
            return NotFound(ApiResponse.Failure(new[] { "Subcontract not found." }, 404));
        var c = s.AddCompliance(r.Type, r.ExpiryDate, r.DocumentReference);
        _context.SubcontractCompliances.Add(c);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(c.Id));
    }

    // Lien waivers
    [HttpPost("{id:guid}/lien-waivers")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddLienWaiver(Guid id, [FromBody] LienWaiverRequest r, CancellationToken ct)
    {
        var s = await _context.Subcontracts.FindAsync(new object[] { id }, ct);
        if (s is null)
            return NotFound(ApiResponse.Failure(new[] { "Subcontract not found." }, 404));
        var w = s.AddLienWaiver(r.WaiverType, r.Amount, r.EffectiveDate, r.IsFinal, r.Description);
        _context.LienWaivers.Add(w);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse<Guid>.Success(w.Id));
    }

    // Retainage release + completion
    [HttpPost("{id:guid}/release-retainage")]
    public async Task<ActionResult<ApiResponse>> ReleaseRetainage(Guid id, [FromBody] RetainageReleaseRequest r, CancellationToken ct)
    {
        var s = await _context.Subcontracts.FindAsync(new object[] { id }, ct);
        if (s is null)
            return NotFound(ApiResponse.Failure(new[] { "Subcontract not found." }, 404));
        s.ReleaseRetainage(r.Amount);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse>> Complete(Guid id, CancellationToken ct)
    {
        var s = await _context.Subcontracts.FindAsync(new object[] { id }, ct);
        if (s is null)
            return NotFound(ApiResponse.Failure(new[] { "Subcontract not found." }, 404));
        s.Close();
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(ApiResponse.Success());
    }
}

// --- Subcontract request DTOs ---
public class CreateSubcontractRequest
{
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid VendorId { get; set; }
    public string SubcontractNumber { get; set; } = string.Empty;
    public decimal ContractAmount { get; set; }
    public decimal RetainagePercentage { get; set; }
    public string? Scope { get; set; }
    public bool PayWhenPaid { get; set; }
}

public class UpdateSubcontractRequest
{
    public decimal? ContractAmount { get; set; }
    public decimal? RetainagePercentage { get; set; }
    public string? Scope { get; set; }
    public bool? PayWhenPaid { get; set; }
}

public class SubCoRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}

public class SubInvoiceRequest
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal RetainageRate { get; set; }
    public string? Description { get; set; }
}

public class SubComplianceRequest
{
    public string Type { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string? DocumentReference { get; set; }
}

public class LienWaiverRequest
{
    public string WaiverType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime EffectiveDate { get; set; }
    public bool IsFinal { get; set; }
    public string? Description { get; set; }
}

public class RetainageReleaseRequest
{
    public decimal Amount { get; set; }
}
