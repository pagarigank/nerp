// <copyright file="VoucherBatchController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using Asp.Versioning;
using ERP.Modules.AccountsPayable.Api;
using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Modules.AccountsPayable.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ap/voucher-batches")]
public class VoucherBatchController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVoucherService _voucherService;

    public VoucherBatchController(IUnitOfWork unitOfWork, IVoucherService voucherService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _voucherService = voucherService ?? throw new ArgumentNullException(nameof(voucherService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VoucherBatchDto>>> GetAll([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var batches = companyId.HasValue
            ? await _unitOfWork.VoucherBatches.FindAsync(x => x.CompanyId == companyId.Value, cancellationToken)
            : await _unitOfWork.VoucherBatches.GetAllAsync(cancellationToken);

        return Ok(batches.Select(b => MapBatchToDto(b)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VoucherBatchDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _unitOfWork.VoucherBatches.GetByIdAsync(id, cancellationToken);
        if (batch == null)
            return NotFound();

        var vouchers = await _unitOfWork.Vouchers.FindAsync(x => x.VoucherBatchId == id, cancellationToken);
        return Ok(MapBatchToDto(batch, vouchers.ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<VoucherBatchDto>> Create([FromBody] CreateVoucherBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await _voucherService.CreateVoucherBatchAsync(
            request.CompanyId,
            request.BatchNumber,
            request.Description,
            request.PostingDate,
            request.FiscalPeriodId,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, MapBatchToDto(batch, []));
    }

    [HttpPost("{id:guid}/vouchers")]
    public async Task<ActionResult<VoucherDto>> AddVoucher(Guid id, [FromBody] AddVoucherToBatchRequest request, CancellationToken cancellationToken)
    {
        var voucher = await _voucherService.AddVoucherToBatchAsync(
            id,
            request.VendorId,
            request.VoucherType,
            request.InvoiceNumber,
            request.InvoiceDate,
            request.DueDate,
            request.TotalAmount,
            request.DiscountAmount,
            request.Description,
            request.PaymentTermId,
            request.PurchaseOrderId,
            request.ReceiptLineId,
            request.Form1099Amount,
            request.BackupWithholdingAmount,
            request.Distributions.Select(d => new ERP.Modules.AccountsPayable.Infrastructure.VoucherDistributionDto(d.AccountId, d.Debit ?? 0, d.Credit ?? 0, d.ProjectId, d.TaskId)).ToList(),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = voucher.Id }, MapVoucherToDto(voucher));
    }

    [HttpPost("{id:guid}/release")]
    public async Task<ActionResult<VoucherBatchDto>> Release(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _voucherService.ReleaseBatchAsync(id, cancellationToken);
        var vouchers = await _unitOfWork.Vouchers.FindAsync(x => x.VoucherBatchId == id, cancellationToken);
        return Ok(MapBatchToDto(batch, vouchers.ToList()));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<VoucherBatchDto>> Post(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _voucherService.PostBatchAsync(id, cancellationToken);
        var vouchers = await _unitOfWork.Vouchers.FindAsync(x => x.VoucherBatchId == id, cancellationToken);
        return Ok(MapBatchToDto(batch, vouchers.ToList()));
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<VoucherBatchDto>> Reverse(Guid id, [FromBody] ReverseBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await _voucherService.ReverseBatchAsync(id, request.Reason, cancellationToken);
        var vouchers = await _unitOfWork.Vouchers.FindAsync(x => x.VoucherBatchId == batch.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, MapBatchToDto(batch, vouchers.ToList()));
    }

    [HttpGet("next-number")]
    public async Task<ActionResult<string>> GetNextBatchNumber([FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var count = await _unitOfWork.VoucherBatches.CountAsync(x => x.CompanyId == companyId, cancellationToken);
        return Ok($"AP-{count + 1:D4}");
    }

    private static VoucherBatchDto MapBatchToDto(VoucherBatch batch, List<Voucher>? vouchers = null)
    {
        vouchers ??= [];
        var voucherDtos = vouchers.Select(v => MapVoucherToDto(v)).ToList();
        return new VoucherBatchDto(batch.Id, batch.CompanyId, batch.BatchNumber, batch.Description, batch.PostingDate, batch.FiscalPeriodId, batch.Status, voucherDtos, batch.CreatedOn, batch.ModifiedOn);
    }

    private static VoucherDto MapVoucherToDto(Voucher voucher)
    {
        var distributions = voucher.Distributions
            .Select(d => new VoucherDistributionDto(d.Id, d.AccountId, d.Debit, d.Credit, d.ProjectId, d.TaskId))
            .ToList();

        return new VoucherDto(voucher.Id, voucher.VoucherBatchId, voucher.VendorId, voucher.VoucherType, voucher.InvoiceNumber, voucher.InvoiceDate, voucher.DueDate, voucher.TotalAmount, voucher.DiscountAmount, voucher.Description, voucher.PaymentTermId, voucher.PurchaseOrderId, voucher.ReceiptLineId, voucher.Form1099Amount, voucher.BackupWithholdingAmount, voucher.Is1099Reportable, voucher.SelectedForPayment, distributions);
    }
}