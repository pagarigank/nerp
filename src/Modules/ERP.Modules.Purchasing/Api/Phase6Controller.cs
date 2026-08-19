// <copyright file="Phase6Controller.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
using ERP.Modules.Purchasing.Domain.Entities;
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

    public Phase6Controller(PurchasingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
            l.Id, l.ItemId, l.Description, l.Quantity, l.UnitOfMeasure, l.UnitPrice, l.LineTotal)).ToList());
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
