// <copyright file="ReceiptController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Domain.Events;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/receipts")]
public class ReceiptController : ControllerBase
{
    private readonly ERP.Modules.Purchasing.Infrastructure.IRepository<Receipt> _receiptRepository;
    private readonly ERP.Modules.Purchasing.Infrastructure.IRepository<PurchaseOrder> _poRepository;
    private readonly ERP.Modules.Purchasing.Infrastructure.IUnitOfWork _unitOfWork;
    private readonly PurchasingDbContext _context;

    public ReceiptController(
        ERP.Modules.Purchasing.Infrastructure.IRepository<Receipt> receiptRepository,
        ERP.Modules.Purchasing.Infrastructure.IRepository<PurchaseOrder> poRepository,
        ERP.Modules.Purchasing.Infrastructure.IUnitOfWork unitOfWork,
        PurchasingDbContext context)
    {
        _receiptRepository = receiptRepository;
        _poRepository = poRepository;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ReceiptDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] ReceiptStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.Receipts.AsQueryable();

        if (companyId.HasValue)
            query = ERP.Modules.Platform.Infrastructure.CompanyScope.ApplyCompanyScope(query, HttpContext, r => r.CompanyId, companyId);

        if (purchaseOrderId.HasValue)
            query = query.Where(r => r.PurchaseOrderId == purchaseOrderId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var receipts = await query.ToListAsync(cancellationToken);

        var dtos = receipts.Select(r => new ReceiptDto
        {
            Id = r.Id,
            ReceiptNumber = r.ReceiptNumber,
            CompanyId = r.CompanyId,
            PurchaseOrderId = r.PurchaseOrderId,
            VendorId = r.VendorId,
            ReceivedDate = r.ReceivedDate,
            Status = r.Status.ToString(),
            IsReversed = r.IsReversed,
        }).ToList();

        return Ok(ApiResponse<List<ReceiptDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReceiptDetailDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var receipt = await _context.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (receipt == null)
        {
            return NotFound(ApiResponse<ReceiptDetailDto>.Failure(["Receipt not found."]));
        }

        var dto = new ReceiptDetailDto
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            CompanyId = receipt.CompanyId,
            PurchaseOrderId = receipt.PurchaseOrderId,
            VendorId = receipt.VendorId,
            ReceivedDate = receipt.ReceivedDate,
            ReceivedBy = receipt.ReceivedBy,
            PackingSlipNumber = receipt.PackingSlipNumber,
            Notes = receipt.Notes,
            Status = receipt.Status.ToString(),
            IsReversed = receipt.IsReversed,
            Lines = receipt.Lines.Select(l => new ReceiptLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                ItemId = l.ItemId,
                Description = l.Description,
                QuantityReceived = l.QuantityReceived,
                UnitOfMeasure = l.UnitOfMeasure,
                LotNumber = l.LotNumber,
                SerialNumber = l.SerialNumber,
            }).ToList(),
        };

        return Ok(ApiResponse<ReceiptDetailDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReceiptDto>>> Create(
        [FromBody] CreateReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = new Receipt(
            request.ReceiptNumber,
            request.CompanyId,
            request.PurchaseOrderId,
            request.VendorId,
            request.ReceivedDate,
            request.ReceivedBy,
            request.PackingSlipNumber,
            request.Notes);

        foreach (var lineRequest in request.Lines)
        {
            var line = new ReceiptLine(
                receipt.Id,
                lineRequest.LineNumber,
                lineRequest.PurchaseOrderLineId,
                lineRequest.ItemId,
                lineRequest.Description,
                lineRequest.QuantityReceived,
                lineRequest.UnitOfMeasure,
                lineRequest.LotNumber,
                lineRequest.SerialNumber,
                lineRequest.QualityInspectionRequired,
                lineRequest.WarehouseId,
                lineRequest.BinLocationId);

            receipt.AddLine(line);
        }

        await _receiptRepository.AddAsync(receipt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ReceiptDto
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            CompanyId = receipt.CompanyId,
            PurchaseOrderId = receipt.PurchaseOrderId,
            VendorId = receipt.VendorId,
            ReceivedDate = receipt.ReceivedDate,
            Status = receipt.Status.ToString(),
            IsReversed = receipt.IsReversed,
        };

        return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, ApiResponse<ReceiptDto>.Success(dto));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<ReceiptDto>>> Post(
        Guid id,
        CancellationToken cancellationToken)
    {
        var receipt = await _context.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (receipt == null)
        {
            return NotFound(ApiResponse<ReceiptDto>.Failure(["Receipt not found."]));
        }

        receipt.Post();

        if (receipt.PurchaseOrderId.HasValue)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == receipt.PurchaseOrderId.Value, cancellationToken);

            if (po != null)
            {
                foreach (var receiptLine in receipt.Lines)
                {
                    if (receiptLine.PurchaseOrderLineId.HasValue)
                    {
                        var poLine = po.Lines.FirstOrDefault(l => l.Id == receiptLine.PurchaseOrderLineId.Value);
                        if (poLine != null)
                        {
                            try
                            {
                                poLine.UpdateQuantityReceived(receiptLine.QuantityReceived);
                            }
                            catch (InvalidOperationException)
                            {
                                // Over-receipt exceeded tolerance - record for approval.
                                var orderedQty = poLine.Quantity;
                                var receivedQty = poLine.QuantityReceived + receiptLine.QuantityReceived;
                                var tolerance = 0.05m;

                                var overReceiptApproval = new OverReceiptApproval(
                                    receipt.CompanyId,
                                    receipt.Id,
                                    receipt.ReceiptNumber,
                                    po.Id,
                                    poLine.Id,
                                    orderedQty,
                                    receivedQty,
                                    tolerance);

                                _context.OverReceiptApprovals.Add(overReceiptApproval);
                            }
                        }
                    }
                }
            }
        }

        _receiptRepository.Update(receipt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ReceiptDto
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            CompanyId = receipt.CompanyId,
            PurchaseOrderId = receipt.PurchaseOrderId,
            VendorId = receipt.VendorId,
            ReceivedDate = receipt.ReceivedDate,
            Status = receipt.Status.ToString(),
            IsReversed = receipt.IsReversed,
        };

        return Ok(ApiResponse<ReceiptDto>.Success(dto));
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<ApiResponse<ReceiptDto>>> Reverse(
        Guid id,
        [FromBody] ReverseReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var receipt = await _context.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (receipt == null)
        {
            return NotFound(ApiResponse<ReceiptDto>.Failure(["Receipt not found."]));
        }

        receipt.Reverse(request.Reason);

        if (receipt.PurchaseOrderId.HasValue)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == receipt.PurchaseOrderId.Value, cancellationToken);

            if (po != null)
            {
                foreach (var receiptLine in receipt.Lines)
                {
                    if (receiptLine.PurchaseOrderLineId.HasValue)
                    {
                        var poLine = po.Lines.FirstOrDefault(l => l.Id == receiptLine.PurchaseOrderLineId.Value);
                        if (poLine != null)
                        {
                            poLine.UpdateQuantityReceived(-receiptLine.QuantityReceived);
                        }
                    }
                }
            }
        }

        _receiptRepository.Update(receipt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ReceiptDto
        {
            Id = receipt.Id,
            ReceiptNumber = receipt.ReceiptNumber,
            CompanyId = receipt.CompanyId,
            PurchaseOrderId = receipt.PurchaseOrderId,
            VendorId = receipt.VendorId,
            ReceivedDate = receipt.ReceivedDate,
            Status = receipt.Status.ToString(),
            IsReversed = receipt.IsReversed,
        };

        return Ok(ApiResponse<ReceiptDto>.Success(dto));
    }
}

public class ReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsReversed { get; set; }
}

public class ReceiptDetailDto : ReceiptDto
{
    public string? ReceivedBy { get; set; }
    public string? PackingSlipNumber { get; set; }
    public string? Notes { get; set; }
    public List<ReceiptLineDto> Lines { get; set; } = [];
}

public class ReceiptLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal QuantityReceived { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public class CreateReceiptRequest
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? ReceivedBy { get; set; }
    public string? PackingSlipNumber { get; set; }
    public string? Notes { get; set; }
    public List<CreateReceiptLineRequest> Lines { get; set; } = [];
}

public class CreateReceiptLineRequest
{
    public int LineNumber { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal QuantityReceived { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public bool QualityInspectionRequired { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BinLocationId { get; set; }
}

public class ReverseReceiptRequest
{
    public string Reason { get; set; } = string.Empty;
}
