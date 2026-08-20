// <copyright file="Phase6Controller.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
using ERP.Core.Common;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Domain.Events;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing")]
public class Phase6Controller : ControllerBase
{
    private readonly PurchasingDbContext _context;
    private readonly IPurchaseOrderService _poService;
    private readonly IApprovalWorkflowService _approvalWorkflow;
    private readonly IAuditLogService _auditLogService;
    private readonly ERP.Modules.Purchasing.Infrastructure.IRepository<ReceiptWithoutPO> _receiptRepo;
    private readonly ERP.Modules.Purchasing.Infrastructure.IUnitOfWork _unitOfWork;
    private readonly IProjectCostValidation _projectCostValidation;

    public Phase6Controller(
        PurchasingDbContext context,
        IPurchaseOrderService poService,
        IApprovalWorkflowService approvalWorkflow,
        IAuditLogService auditLogService,
        ERP.Modules.Purchasing.Infrastructure.IRepository<ReceiptWithoutPO> receiptRepo,
        ERP.Modules.Purchasing.Infrastructure.IUnitOfWork unitOfWork,
        IProjectCostValidation projectCostValidation)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _poService = poService ?? throw new ArgumentNullException(nameof(poService));
        _approvalWorkflow = approvalWorkflow ?? throw new ArgumentNullException(nameof(approvalWorkflow));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _receiptRepo = receiptRepo ?? throw new ArgumentNullException(nameof(receiptRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _projectCostValidation = projectCostValidation ?? throw new ArgumentNullException(nameof(projectCostValidation));
    }

    // ===== PO blanket / standing release (draw-down) =====
    [HttpPost("purchase-orders/{id:guid}/release")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> ReleasePurchaseOrderAsync(
        Guid id,
        [FromBody] ReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && !p.DeletedOn.HasValue, cancellationToken);
        if (po == null)
            return NotFound(ApiResponse<PurchaseOrderDto>.Failure(new[] { "Purchase order not found." }));

        try
        {
            po.Release(request.Amount);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PurchaseOrderDto>.Failure(new[] { ex.Message }));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<PurchaseOrderDto>.Success(MapPurchaseOrder(po)));
    }

    // ===== PO print / email =====
    [HttpPost("purchase-orders/{id:guid}/print")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderPrintDto>>> PrintPurchaseOrderAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id && !p.DeletedOn.HasValue, cancellationToken);
        if (po == null)
            return NotFound(ApiResponse<PurchaseOrderPrintDto>.Failure(new[] { "Purchase order not found." }));

        po.MarkPrinted();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<PurchaseOrderPrintDto>.Success(new PurchaseOrderPrintDto(
            po.Id,
            po.PONumber,
            po.VendorId,
            po.OrderDate,
            po.Status.ToString(),
            po.Lines.Select(l => new PurchaseOrderPrintLineDto(
                l.LineNumber,
                l.Description,
                l.Quantity,
                l.UnitOfMeasure,
                l.UnitPrice,
                l.TaxCode,
                l.TaxRate,
                l.TaxAmount,
                l.GetExtendedPriceWithTax())).ToList(),
            po.FreightAmount,
            po.FreightTaxAmount,
            po.GetTaxTotal(),
            po.GetTotalAmountWithTax(),
            po.TaxExempt,
            po.PrintedDate)));
    }

    [HttpPost("purchase-orders/{id:guid}/email-vendor")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> EmailPurchaseOrderAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && !p.DeletedOn.HasValue, cancellationToken);
        if (po == null)
            return NotFound(ApiResponse<PurchaseOrderDto>.Failure(new[] { "Purchase order not found." }));

        if (po.Status != PurchaseOrderStatus.Approved && po.Status != PurchaseOrderStatus.Draft)
            return BadRequest(ApiResponse<PurchaseOrderDto>.Failure(new[] { "Only draft or approved POs can be emailed to a vendor." }));

        po.MarkEmailedToVendor();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<PurchaseOrderDto>.Success(MapPurchaseOrder(po)));
    }

    // ===== PO approval queue =====
    [HttpGet("purchase-orders/approval-queue")]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderDto>>>> GetApprovalQueueAsync(
        CancellationToken cancellationToken)
    {
        var pos = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.PendingApproval && !p.DeletedOn.HasValue)
            .OrderBy(p => p.OrderDate)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<PurchaseOrderDto>>.Success(pos.Select(MapPurchaseOrder).ToList()));
    }

    // ===== RFQ / vendor quote workflow =====
    [HttpGet("vendor-quotes")]
    public async Task<ActionResult<ApiResponse<List<VendorQuoteDto>>>> GetVendorQuotesAsync(
        [FromQuery] Guid? vendorId,
        CancellationToken cancellationToken)
    {
        var query = _context.VendorQuotes
            .Include(q => q.Lines)
            .Where(q => !q.DeletedOn.HasValue);
        if (vendorId.HasValue)
            query = query.Where(q => q.VendorId == vendorId.Value);

        var quotes = await query.OrderByDescending(q => q.CreatedOn).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<VendorQuoteDto>>.Success(quotes.Select(MapVendorQuote).ToList()));
    }

    [HttpGet("vendor-quotes/{id:guid}")]
    public async Task<ActionResult<ApiResponse<VendorQuoteDto>>> GetVendorQuoteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var quote = await _context.VendorQuotes
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id && !q.DeletedOn.HasValue, cancellationToken);
        if (quote == null)
            return NotFound(ApiResponse<VendorQuoteDto>.Failure(new[] { "Vendor quote not found." }));

        return Ok(ApiResponse<VendorQuoteDto>.Success(MapVendorQuote(quote)));
    }

    [HttpPost("vendor-quotes")]
    public async Task<ActionResult<ApiResponse<VendorQuoteDto>>> CreateVendorQuoteAsync(
        [FromBody] CreateVendorQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quote = new VendorQuote(
            request.RfxNumber,
            request.CompanyId,
            request.VendorId,
            request.RequestedById,
            request.ValidUntil,
            request.Notes);

        foreach (var line in request.Lines)
            quote.AddLine(line.ItemId, line.Description, line.Quantity, line.UnitOfMeasure, line.UnitPrice);

        _context.VendorQuotes.Add(quote);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<VendorQuoteDto>.Success(MapVendorQuote(quote)));
    }

    [HttpPost("vendor-quotes/{id:guid}/receive")]
    public async Task<ActionResult<ApiResponse<VendorQuoteDto>>> ReceiveVendorQuoteAsync(
        Guid id,
        [FromBody] ReceiveVendorQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quote = await _context.VendorQuotes
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id && !q.DeletedOn.HasValue, cancellationToken);
        if (quote == null)
            return NotFound(ApiResponse<VendorQuoteDto>.Failure(new[] { "Vendor quote not found." }));

        List<VendorQuoteLine>? lines = null;
        if (request.Lines != null && request.Lines.Count > 0)
        {
            lines = request.Lines.Select(l => new VendorQuoteLine(
                quote.Id, l.ItemId, l.Description, l.Quantity, l.UnitOfMeasure, l.UnitPrice)).ToList();
        }

        try
        {
            quote.ReceiveQuote(request.QuoteNumber, request.QuoteDate, request.Freight, lines);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VendorQuoteDto>.Failure(new[] { ex.Message }));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<VendorQuoteDto>.Success(MapVendorQuote(quote)));
    }

    [HttpPost("vendor-quotes/{id:guid}/award")]
    public async Task<ActionResult<ApiResponse<VendorQuoteDto>>> AwardVendorQuoteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var quote = await _context.VendorQuotes.FirstOrDefaultAsync(q => q.Id == id && !q.DeletedOn.HasValue, cancellationToken);
        if (quote == null)
            return NotFound(ApiResponse<VendorQuoteDto>.Failure(new[] { "Vendor quote not found." }));

        try
        {
            quote.Award();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VendorQuoteDto>.Failure(new[] { ex.Message }));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<VendorQuoteDto>.Success(MapVendorQuote(quote)));
    }

    [HttpPost("vendor-quotes/{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<VendorQuoteDto>>> RejectVendorQuoteAsync(
        Guid id,
        [FromBody] RejectVendorQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quote = await _context.VendorQuotes.FirstOrDefaultAsync(q => q.Id == id && !q.DeletedOn.HasValue, cancellationToken);
        if (quote == null)
            return NotFound(ApiResponse<VendorQuoteDto>.Failure(new[] { "Vendor quote not found." }));

        try
        {
            quote.Reject(request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VendorQuoteDto>.Failure(new[] { ex.Message }));
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<VendorQuoteDto>.Success(MapVendorQuote(quote)));
    }

    // ===== PO approval with committed-cost / project budget check (spec §5.10) =====
    [HttpPost("purchase-orders/{id:guid}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse<ApprovalRequestDto>>> SubmitPurchaseOrderForApprovalAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id && !p.DeletedOn.HasValue, cancellationToken);

        if (po == null)
            return NotFound(ApiResponse<ApprovalRequestDto>.Failure(new[] { "Purchase order not found." }));

        // Validate project budgets for any project-charged lines via the shared cross-module contract.
        var projectLines = po.Lines.Where(l => l.ProjectId.HasValue).ToList();
        foreach (var line in projectLines)
        {
            var proposedAmount = line.GetExtendedPrice();
            var result = await _projectCostValidation.ValidateAsync(
                po.CompanyId, line.ProjectId, line.TaskId, proposedAmount, cancellationToken);

            if (!result.IsValid)
            {
                return BadRequest(ApiResponse<ApprovalRequestDto>.Failure(new[] { result.Message ?? "Project budget exceeded." }));
            }
        }

        try
        {
            po.SubmitForApproval();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ApprovalRequestDto>.Failure(new[] { ex.Message }));
        }

        _context.PurchaseOrders.Update(po);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "PO Submitted for Approval",
            nameof(PurchaseOrder),
            po.Id,
            User.Identity?.Name ?? "system",
            newValues: new { PONumber = po.PONumber, Status = po.Status.ToString(), Total = po.GetTotalAmount() },
            cancellationToken: cancellationToken);

        // Route through the Platform approval workflow engine.
        var workflow = await _approvalWorkflow.GetWorkflowAsync(
            "Purchasing", "PurchaseOrder", po.GetTotalAmount(), po.CompanyId, cancellationToken);

        if (workflow is not null)
        {
            var request = await _approvalWorkflow.SubmitForApprovalAsync(
                workflow.Id,
                "Purchasing",
                "PurchaseOrder",
                po.Id,
                po.PONumber,
                po.GetTotalAmount(),
                User.Identity?.Name ?? "system",
                $"PO submitted for approval. Total: {po.GetTotalAmount():C}",
                cancellationToken);

            return Ok(ApiResponse<ApprovalRequestDto>.Success(new ApprovalRequestDto(
                request.Id,
                request.Status.ToString(),
                request.CurrentStep)));
        }

        // If no workflow configured, auto-approve (small dollar POs).
        po.Approve(Guid.Parse(User.Identity?.Name ?? Guid.NewGuid().ToString()));
        _context.PurchaseOrders.Update(po);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<ApprovalRequestDto>.Success(new ApprovalRequestDto(
            po.Id, po.Status.ToString(), 0)));
    }

    // ===== Receipt-without-PO workflow (spec §6: Receipt-without-PO workflow) =====
    [HttpGet("receipts-without-po")]
    public async Task<ActionResult<ApiResponse<List<ReceiptWithoutPoDto>>>> GetReceiptsWithoutPoAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] ReceiptWithoutPOStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.ReceiptsWithoutPO
            .Include(r => r.Lines)
            .AsQueryable();

        if (companyId.HasValue)
            query = ERP.Modules.Platform.Infrastructure.CompanyScope.ApplyCompanyScope(query, HttpContext, r => r.CompanyId, companyId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var receipts = await query.OrderByDescending(r => r.ReceivedDate).ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<ReceiptWithoutPoDto>>.Success(
            receipts.Select(MapReceiptWithoutPO).ToList()));
    }

    [HttpGet("receipts-without-po/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReceiptWithoutPoDetailDto>>> GetReceiptWithoutPoAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var receipt = await _context.ReceiptsWithoutPO
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id && !r.DeletedOn.HasValue, cancellationToken);

        if (receipt == null)
            return NotFound(ApiResponse<ReceiptWithoutPoDetailDto>.Failure(new[] { "Receipt not found." }));

        return Ok(ApiResponse<ReceiptWithoutPoDetailDto>.Success(MapReceiptWithoutPODetail(receipt)));
    }

    [HttpPost("receipts-without-po")]
    public async Task<ActionResult<ApiResponse<ReceiptWithoutPoDto>>> CreateReceiptWithoutPoAsync(
        [FromBody] CreateReceiptWithoutPoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReceiptNumber))
            return BadRequest(ApiResponse<ReceiptWithoutPoDto>.Failure(new[] { "Receipt number is required." }));

        var exists = await _context.ReceiptsWithoutPO
            .AnyAsync(r => r.CompanyId == request.CompanyId && r.ReceiptNumber == request.ReceiptNumber, cancellationToken);

        if (exists)
            return BadRequest(ApiResponse<ReceiptWithoutPoDto>.Failure(new[] { "Receipt number already exists for this company." }));

        var receipt = new ReceiptWithoutPO(
            request.ReceiptNumber,
            request.CompanyId,
            request.VendorId,
            request.ReceivedDate,
            request.ReceivedBy,
            request.PackingSlipNumber,
            request.Notes);

        foreach (var line in request.Lines)
        {
            receipt.AddLine(new ReceiptWithoutPOLine(
                receipt.Id,
                line.LineNumber,
                line.ItemId,
                line.Description,
                line.QuantityReceived,
                line.UnitOfMeasure,
                line.UnitPrice,
                line.AccountId,
                line.ProjectId,
                line.TaskId));
        }

        await _receiptRepo.AddAsync(receipt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ReceiptWithoutPO Created",
            nameof(ReceiptWithoutPO),
            receipt.Id,
            User.Identity?.Name ?? "system",
            newValues: new { request.ReceiptNumber, request.CompanyId, request.VendorId, LineCount = request.Lines.Count },
            cancellationToken: cancellationToken);

        return CreatedAtAction(
            nameof(GetReceiptWithoutPoAsync),
            new { id = receipt.Id },
            ApiResponse<ReceiptWithoutPoDto>.Success(MapReceiptWithoutPO(receipt)));
    }

    [HttpPost("receipts-without-po/{id:guid}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse<ApprovalRequestDto>>> SubmitReceiptWithoutPoForApprovalAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var receipt = await _context.ReceiptsWithoutPO
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id && !r.DeletedOn.HasValue, cancellationToken);

        if (receipt == null)
            return NotFound(ApiResponse<ApprovalRequestDto>.Failure(new[] { "Receipt not found." }));

        var workflow = await _approvalWorkflow.GetWorkflowAsync(
            "Purchasing", "ReceiptWithoutPO", receipt.GetTotalAmount(), receipt.CompanyId, cancellationToken);

        if (workflow is null)
            return BadRequest(ApiResponse<ApprovalRequestDto>.Failure(new[] { "No active approval workflow configured for ReceiptWithoutPO." }));

        var request = await _approvalWorkflow.SubmitForApprovalAsync(
            workflow.Id,
            "Purchasing",
            "ReceiptWithoutPO",
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.GetTotalAmount(),
            User.Identity?.Name ?? "system",
            $"Receipt-without-PO submitted for approval. Total: {receipt.GetTotalAmount():C}",
            cancellationToken);

        receipt.MarkPendingApproval(request.Id);
        _context.ReceiptsWithoutPO.Update(receipt);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ReceiptWithoutPO Submitted for Approval",
            nameof(ReceiptWithoutPO),
            receipt.Id,
            User.Identity?.Name ?? "system",
            newValues: new { ApprovalRequestId = request.Id },
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<ApprovalRequestDto>.Success(new ApprovalRequestDto(
            request.Id, request.Status.ToString(), request.CurrentStep)));
    }

    [HttpPost("receipts-without-po/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ReceiptWithoutPoDto>>> ApproveReceiptWithoutPoAsync(
        Guid id, [FromBody] ApproveRequest request, CancellationToken cancellationToken)
    {
        var receipt = await _context.ReceiptsWithoutPO
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id && !r.DeletedOn.HasValue, cancellationToken);

        if (receipt == null)
            return NotFound(ApiResponse<ReceiptWithoutPoDto>.Failure(new[] { "Receipt not found." }));

        if (receipt.Status != ReceiptWithoutPOStatus.PendingApproval)
            return BadRequest(ApiResponse<ReceiptWithoutPoDto>.Failure(new[] { $"Receipt must be in PendingApproval status to approve. Current: {receipt.Status}" }));

        if (request.ApprovedById == Guid.Empty)
            return BadRequest(ApiResponse<ReceiptWithoutPoDto>.Failure(new[] { "ApprovedById is required." }));

        if (receipt.ApprovalRequestId.HasValue)
        {
            var approvalRequest = await _approvalWorkflow.GetRequestByIdAsync(receipt.ApprovalRequestId.Value, cancellationToken);
            if (approvalRequest?.Status != ApprovalStatus.Pending && approvalRequest?.Status != ApprovalStatus.PartiallyApproved)
                return BadRequest(ApiResponse<ReceiptWithoutPoDto>.Failure(new[] { $"Approval request is in {approvalRequest?.Status} status." }));
        }

        receipt.Approve(request.ApprovedById);
        _context.ReceiptsWithoutPO.Update(receipt);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ReceiptWithoutPO Approved",
            nameof(ReceiptWithoutPO),
            receipt.Id,
            request.ApprovedById.ToString(),
            newValues: new { receipt.Status },
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<ReceiptWithoutPoDto>.Success(MapReceiptWithoutPO(receipt)));
    }

    [HttpPost("receipts-without-po/{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<ReceiptWithoutPoDto>>> PostReceiptWithoutPoAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var receipt = await _context.ReceiptsWithoutPO
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id && !r.DeletedOn.HasValue, cancellationToken);

        if (receipt == null)
            return NotFound(ApiResponse<ReceiptWithoutPoDto>.Failure(new[] { "Receipt not found." }));

        try
        {
            receipt.Post();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ReceiptWithoutPoDto>.Failure(new[] { ex.Message }));
        }

        _context.ReceiptsWithoutPO.Update(receipt);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            "ReceiptWithoutPO Posted",
            nameof(ReceiptWithoutPO),
            receipt.Id,
            User.Identity?.Name ?? "system",
            newValues: new { Status = receipt.Status, receipt.PostedDate },
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<ReceiptWithoutPoDto>.Success(MapReceiptWithoutPO(receipt)));
    }

    // ===== Over-receipt exception approval (spec §6: Over-receipt exception approval workflow) =====
    [HttpGet("over-receipt-approvals")]
    public async Task<ActionResult<ApiResponse<List<OverReceiptApprovalDto>>>> GetOverReceiptApprovalsAsync(
        [FromQuery] Guid? companyId,
        [FromQuery] OverReceiptApprovalStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.OverReceiptApprovals.AsQueryable();

        if (companyId.HasValue)
            query = ERP.Modules.Platform.Infrastructure.CompanyScope.ApplyCompanyScope(query, HttpContext, a => a.CompanyId, companyId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var approvals = await query.OrderByDescending(a => a.CreatedOn).ToListAsync(cancellationToken);

        return Ok(ApiResponse<List<OverReceiptApprovalDto>>.Success(approvals.Select(MapOverReceiptApproval).ToList()));
    }

    [HttpPost("over-receipt-approvals/{id:guid}/resolve")]
    public async Task<ActionResult<ApiResponse<OverReceiptApprovalDto>>> ResolveOverReceiptApprovalAsync(
        Guid id,
        [FromBody] ResolveOverReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var approval = await _context.OverReceiptApprovals
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (approval == null)
            return NotFound(ApiResponse<OverReceiptApprovalDto>.Failure(new[] { "Over-receipt approval not found." }));

        var newStatus = request.Approved
            ? OverReceiptApprovalStatus.Approved
            : OverReceiptApprovalStatus.Rejected;

        try
        {
            approval.Resolve(newStatus);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OverReceiptApprovalDto>.Failure(new[] { ex.Message }));
        }

        _context.OverReceiptApprovals.Update(approval);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            $"OverReceipt {newStatus}",
            nameof(OverReceiptApproval),
            approval.Id,
            User.Identity?.Name ?? "system",
            newValues: new { approval.Status, request.Reason },
            cancellationToken: cancellationToken);

        return Ok(ApiResponse<OverReceiptApprovalDto>.Success(MapOverReceiptApproval(approval)));
    }

    // ===== Mappers =====
    private static PurchaseOrderDto MapPurchaseOrder(PurchaseOrder p) => new()
    {
        Id = p.Id,
        PONumber = p.PONumber,
        CompanyId = p.CompanyId,
        VendorId = p.VendorId,
        OrderDate = p.OrderDate,
        Status = p.Status.ToString(),
        TotalAmount = p.GetTotalAmount(),
        RemainingAmount = p.GetRemainingAmount(),
        BlanketAmountLimit = p.BlanketAmountLimit,
        ReleasedAmount = p.ReleasedAmount,
        FreightAmount = p.FreightAmount,
        FreightTaxAmount = p.FreightTaxAmount,
        TaxExempt = p.TaxExempt,
        PrintedDate = p.PrintedDate,
        EmailedToVendorDate = p.EmailedToVendorDate,
    };

    private static VendorQuoteDto MapVendorQuote(VendorQuote q) => new(
        q.Id,
        q.RfxNumber,
        q.CompanyId,
        q.VendorId,
        q.RequestedById,
        q.Status.ToString(),
        q.ValidUntil,
        q.Notes,
        q.QuoteNumber,
        q.QuoteDate,
        q.QuoteFreight,
        q.QuoteTotal,
        q.Lines.Select(l => new VendorQuoteLineDto(
            l.Id, l.ItemId, l.Description, l.Quantity, l.UnitOfMeasure, l.UnitPrice, l.LineTotal))
            .ToList());

    // Mapper methods for receipt-without-PO and over-receipt approval
    private static ReceiptWithoutPoDto MapReceiptWithoutPO(ReceiptWithoutPO r) => new()
    {
        Id = r.Id,
        ReceiptNumber = r.ReceiptNumber,
        CompanyId = r.CompanyId,
        VendorId = r.VendorId,
        ReceivedDate = r.ReceivedDate,
        Status = r.Status.ToString(),
        IsReversed = r.IsReversed,
        TotalAmount = r.GetTotalAmount(),
    };

    private static ReceiptWithoutPoDetailDto MapReceiptWithoutPODetail(ReceiptWithoutPO r) => new()
    {
        Id = r.Id,
        ReceiptNumber = r.ReceiptNumber,
        CompanyId = r.CompanyId,
        VendorId = r.VendorId,
        ReceivedDate = r.ReceivedDate,
        Status = r.Status.ToString(),
        IsReversed = r.IsReversed,
        TotalAmount = r.GetTotalAmount(),
        ReceivedBy = r.ReceivedBy,
        PackingSlipNumber = r.PackingSlipNumber,
        Notes = r.Notes,
        ApprovalRequestId = r.ApprovalRequestId,
        Lines = r.Lines.Select(l => new ReceiptWithoutPoLineDto
        {
            Id = l.Id,
            LineNumber = l.LineNumber,
            ItemId = l.ItemId,
            Description = l.Description,
            QuantityReceived = l.QuantityReceived,
            UnitOfMeasure = l.UnitOfMeasure,
            UnitPrice = l.UnitPrice,
            AccountId = l.AccountId,
            ProjectId = l.ProjectId,
            TaskId = l.TaskId,
            ExtendedAmount = l.ExtendedAmount,
        }).ToList(),
    };

    private static OverReceiptApprovalDto MapOverReceiptApproval(OverReceiptApproval a) => new()
    {
        Id = a.Id,
        CompanyId = a.CompanyId,
        ReceiptId = a.ReceiptId,
        ReceiptNumber = a.ReceiptNumber,
        PurchaseOrderId = a.PurchaseOrderId,
        PurchaseOrderLineId = a.PurchaseOrderLineId,
        OrderedQuantity = a.OrderedQuantity,
        ReceivedQuantity = a.ReceivedQuantity,
        OverReceiptTolerance = a.OverReceiptTolerance,
        Status = a.Status.ToString(),
        ApprovalRequestId = a.ApprovalRequestId,
    };
}

public record ReleaseRequest(decimal Amount);

public record PurchaseOrderPrintDto(
    Guid Id,
    string PONumber,
    Guid VendorId,
    DateTime OrderDate,
    string Status,
    List<PurchaseOrderPrintLineDto> Lines,
    decimal FreightAmount,
    decimal FreightTaxAmount,
    decimal TaxTotal,
    decimal TotalWithTax,
    bool TaxExempt,
    DateTime? PrintedDate);

public record PurchaseOrderPrintLineDto(
    int LineNumber,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    string? TaxCode,
    decimal TaxRate,
    decimal TaxAmount,
    decimal ExtendedWithTax);

public record CreateVendorQuoteRequest(
    string RfxNumber,
    Guid CompanyId,
    Guid VendorId,
    Guid? RequestedById,
    DateTime? ValidUntil,
    string? Notes,
    List<CreateVendorQuoteLineRequest> Lines);

public record CreateVendorQuoteLineRequest(
    string? ItemId,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice);

public record ReceiveVendorQuoteRequest(
    string QuoteNumber,
    DateTime QuoteDate,
    decimal Freight,
    List<CreateVendorQuoteLineRequest>? Lines);

public record RejectVendorQuoteRequest(string? Reason);

public record VendorQuoteDto(
    Guid Id,
    string RfxNumber,
    Guid CompanyId,
    Guid VendorId,
    Guid? RequestedById,
    string Status,
    DateTime? ValidUntil,
    string? Notes,
    string? QuoteNumber,
    DateTime? QuoteDate,
    decimal QuoteFreight,
    decimal QuoteTotal,
    List<VendorQuoteLineDto> Lines);

public record VendorQuoteLineDto(
    Guid Id,
    string? ItemId,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal LineTotal);

// ===== Receipt-without-PO DTOs and mappers =====
public class ReceiptWithoutPoDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsReversed { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ReceiptWithoutPoDetailDto : ReceiptWithoutPoDto
{
    public string? ReceivedBy { get; set; }
    public string? PackingSlipNumber { get; set; }
    public string? Notes { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public List<ReceiptWithoutPoLineDto> Lines { get; set; } = [];
}

public class ReceiptWithoutPoLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal QuantityReceived { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public decimal ExtendedAmount { get; set; }
}

public class CreateReceiptWithoutPoRequest
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? ReceivedBy { get; set; }
    public string? PackingSlipNumber { get; set; }
    public string? Notes { get; set; }
    public List<CreateReceiptWithoutPoLineRequest> Lines { get; set; } = [];
}

public class CreateReceiptWithoutPoLineRequest
{
    public int LineNumber { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal QuantityReceived { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
}

public record ApprovalRequestDto(Guid Id, string Status, int CurrentStep);

public class OverReceiptApprovalDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal OverReceiptTolerance { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ApprovalRequestId { get; set; }
}

public class ResolveOverReceiptRequest
{
    public bool Approved { get; set; }
    public string? Reason { get; set; }
}
