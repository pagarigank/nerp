// <copyright file="PurchaseOrderController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.Purchasing.Domain.Entities;
using ERP.Modules.Purchasing.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Purchasing.Api;

[ApiController]
[Route("api/v1/purchasing/purchase-orders")]
public class PurchaseOrderController : ControllerBase
{
    private readonly ERP.Modules.Purchasing.Infrastructure.IRepository<PurchaseOrder> _poRepository;
    private readonly ERP.Modules.Purchasing.Infrastructure.IUnitOfWork _unitOfWork;
    private readonly PurchasingDbContext _context;
    private readonly IPurchaseOrderService _poService;
    private readonly IProjectCostValidation _projectCostValidation;

    public PurchaseOrderController(
        ERP.Modules.Purchasing.Infrastructure.IRepository<PurchaseOrder> poRepository,
        ERP.Modules.Purchasing.Infrastructure.IUnitOfWork unitOfWork,
        PurchasingDbContext context,
        IPurchaseOrderService poService,
        IProjectCostValidation projectCostValidation)
    {
        _poRepository = poRepository;
        _unitOfWork = unitOfWork;
        _context = context;
        _poService = poService;
        _projectCostValidation = projectCostValidation;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderDto>>>> GetAll(
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? vendorId,
        [FromQuery] PurchaseOrderStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders.AsQueryable();

        if (companyId.HasValue)
            query = ERP.Modules.Platform.Infrastructure.CompanyScope.ApplyCompanyScope(query, HttpContext, po => po.CompanyId, companyId);

        if (vendorId.HasValue)
            query = query.Where(po => po.VendorId == vendorId.Value);

        if (status.HasValue)
            query = query.Where(po => po.Status == status.Value);

        var purchaseOrders = await query.ToListAsync(cancellationToken);

        var dtos = purchaseOrders.Select(po => new PurchaseOrderDto
        {
            Id = po.Id,
            PONumber = po.PONumber,
            CompanyId = po.CompanyId,
            VendorId = po.VendorId,
            OrderDate = po.OrderDate,
            Status = po.Status.ToString(),
            TotalAmount = po.GetTotalAmount(),
            RemainingAmount = po.GetRemainingAmount(),
        }).ToList();

        return Ok(ApiResponse<List<PurchaseOrderDto>>.Success(dtos));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDetailDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (po == null)
        {
            return NotFound(ApiResponse<PurchaseOrderDetailDto>.Failure(["Purchase order not found."]));
        }

        var dto = new PurchaseOrderDetailDto
        {
            Id = po.Id,
            PONumber = po.PONumber,
            CompanyId = po.CompanyId,
            VendorId = po.VendorId,
            OrderDate = po.OrderDate,
            OrderType = po.OrderType.ToString(),
            ShipToName = po.ShipToName,
            ShipToAddress = po.ShipToAddress,
            Status = po.Status.ToString(),
            TotalAmount = po.GetTotalAmount(),
            RemainingAmount = po.GetRemainingAmount(),
            Lines = po.Lines.Select(l => new PurchaseOrderLineDto
            {
                Id = l.Id,
                LineNumber = l.LineNumber,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitOfMeasure = l.UnitOfMeasure,
                UnitPrice = l.UnitPrice,
                QuantityReceived = l.QuantityReceived,
                QuantityInvoiced = l.QuantityInvoiced,
                ExtendedPrice = l.GetExtendedPrice(),
            }).ToList(),
        };

        return Ok(ApiResponse<PurchaseOrderDetailDto>.Success(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Create(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        var po = new PurchaseOrder(
            request.PONumber,
            request.CompanyId,
            request.VendorId,
            request.OrderDate,
            request.OrderType,
            request.ShipToName,
            request.ShipToAddress,
            request.PaymentTermId,
            request.BuyerId,
            request.BuyerNotes,
            request.VendorReference);

        foreach (var lineRequest in request.Lines)
        {
            var line = new PurchaseOrderLine(
                po.Id,
                lineRequest.LineNumber,
                lineRequest.ItemId,
                lineRequest.Description,
                lineRequest.Quantity,
                lineRequest.UnitOfMeasure,
                lineRequest.UnitPrice,
                lineRequest.NeedByDate,
                lineRequest.AccountId,
                lineRequest.ProjectId,
                lineRequest.TaskId,
                lineRequest.RequisitionLineId);

            if (!string.IsNullOrWhiteSpace(lineRequest.TaxCode) || lineRequest.TaxRate != 0)
                line.SetTax(lineRequest.TaxCode, lineRequest.TaxRate);

            po.AddLine(line);
        }

        if (request.BlanketAmountLimit.HasValue)
            po.SetBlanketLimit(request.BlanketAmountLimit.Value);
        if (request.FreightAmount != 0 || request.FreightTaxAmount != 0 || request.TaxExempt)
        {
            po.SetFreight(request.FreightAmount, request.FreightTaxAmount);
            po.SetTaxExempt(request.TaxExempt);
        }

        await _poRepository.AddAsync(po, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new PurchaseOrderDto
        {
            Id = po.Id,
            PONumber = po.PONumber,
            CompanyId = po.CompanyId,
            VendorId = po.VendorId,
            OrderDate = po.OrderDate,
            Status = po.Status.ToString(),
            TotalAmount = po.GetTotalAmount(),
            RemainingAmount = po.GetRemainingAmount(),
        };

        return CreatedAtAction(nameof(GetById), new { id = po.Id }, ApiResponse<PurchaseOrderDto>.Success(dto));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Approve(
        Guid id,
        [FromBody] ApproveRequest request,
        CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (po == null)
        {
            return NotFound(ApiResponse<PurchaseOrderDto>.Failure(["Purchase order not found."]));
        }

        // Validate project budgets for any project-charged lines via the shared cross-module contract.
        var projectLines = po.Lines.Where(l => l.ProjectId.HasValue).ToList();
        foreach (var line in projectLines)
        {
            var proposedAmount = line.GetExtendedPrice();
            var result = await _projectCostValidation.ValidateAsync(
                po.CompanyId, line.ProjectId, line.TaskId, proposedAmount, cancellationToken);

            if (!result.IsValid)
            {
                return BadRequest(ApiResponse<PurchaseOrderDto>.Failure(new[] { result.Message ?? "Project budget exceeded." }));
            }
        }

        po.Approve(request.ApprovedById);
        _poRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new PurchaseOrderDto
        {
            Id = po.Id,
            PONumber = po.PONumber,
            CompanyId = po.CompanyId,
            VendorId = po.VendorId,
            OrderDate = po.OrderDate,
            Status = po.Status.ToString(),
            TotalAmount = po.GetTotalAmount(),
            RemainingAmount = po.GetRemainingAmount(),
        };

        return Ok(ApiResponse<PurchaseOrderDto>.Success(dto));
    }

    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> SubmitForApproval(
        Guid id,
        CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (po == null)
        {
            return NotFound(ApiResponse<PurchaseOrderDto>.Failure(["Purchase order not found."]));
        }

        po.SubmitForApproval();
        _poRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new PurchaseOrderDto
        {
            Id = po.Id,
            PONumber = po.PONumber,
            CompanyId = po.CompanyId,
            VendorId = po.VendorId,
            OrderDate = po.OrderDate,
            Status = po.Status.ToString(),
            TotalAmount = po.GetTotalAmount(),
            RemainingAmount = po.GetRemainingAmount(),
        };

        return Ok(ApiResponse<PurchaseOrderDto>.Success(dto));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Close(
        Guid id,
        [FromBody] CloseRequest? request,
        CancellationToken cancellationToken)
    {
        var po = await _poRepository.GetByIdAsync(id, cancellationToken);

        if (po == null)
        {
            return NotFound(ApiResponse<PurchaseOrderDto>.Failure(["Purchase order not found."]));
        }

        po.Close(request?.Reason);
        _poRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new PurchaseOrderDto
        {
            Id = po.Id,
            PONumber = po.PONumber,
            CompanyId = po.CompanyId,
            VendorId = po.VendorId,
            OrderDate = po.OrderDate,
            Status = po.Status.ToString(),
            TotalAmount = po.GetTotalAmount(),
            RemainingAmount = po.GetRemainingAmount(),
        };

        return Ok(ApiResponse<PurchaseOrderDto>.Success(dto));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Cancel(
        Guid id,
        [FromBody] CancelRequest request,
        CancellationToken cancellationToken)
    {
        var po = await _poRepository.GetByIdAsync(id, cancellationToken);

        if (po == null)
        {
            return NotFound(ApiResponse<PurchaseOrderDto>.Failure(["Purchase order not found."]));
        }

        po.Cancel(request.Reason);
        _poRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new PurchaseOrderDto
        {
            Id = po.Id,
            PONumber = po.PONumber,
            CompanyId = po.CompanyId,
            VendorId = po.VendorId,
            OrderDate = po.OrderDate,
            Status = po.Status.ToString(),
            TotalAmount = po.GetTotalAmount(),
            RemainingAmount = po.GetRemainingAmount(),
        };

        return Ok(ApiResponse<PurchaseOrderDto>.Success(dto));
    }
}

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal? BlanketAmountLimit { get; set; }
    public decimal ReleasedAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal FreightTaxAmount { get; set; }
    public bool TaxExempt { get; set; }
    public DateTime? PrintedDate { get; set; }
    public DateTime? EmailedToVendorDate { get; set; }
}

public class PurchaseOrderDetailDto : PurchaseOrderDto
{
    public string OrderType { get; set; } = string.Empty;
    public string? ShipToName { get; set; }
    public string? ShipToAddress { get; set; }
    public List<PurchaseOrderLineDto> Lines { get; set; } = [];
}

public class PurchaseOrderLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityInvoiced { get; set; }
    public decimal ExtendedPrice { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public string PONumber { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid VendorId { get; set; }
    public DateTime OrderDate { get; set; }
    public PurchaseOrderType OrderType { get; set; }
    public string? ShipToName { get; set; }
    public string? ShipToAddress { get; set; }
    public Guid? PaymentTermId { get; set; }
    public Guid? BuyerId { get; set; }
    public string? BuyerNotes { get; set; }
    public string? VendorReference { get; set; }
    public decimal? BlanketAmountLimit { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal FreightTaxAmount { get; set; }
    public bool TaxExempt { get; set; }
    public List<CreatePurchaseOrderLineRequest> Lines { get; set; } = [];
}

public class CreatePurchaseOrderLineRequest
{
    public int LineNumber { get; set; }
    public string? ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? TaxCode { get; set; }
    public decimal TaxRate { get; set; }
    public DateTime? NeedByDate { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? RequisitionLineId { get; set; }
}

public class ApproveRequest
{
    public Guid ApprovedById { get; set; }
}

public class CloseRequest
{
    public string? Reason { get; set; }
}

public class CancelRequest
{
    public string Reason { get; set; } = string.Empty;
}
