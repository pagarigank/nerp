// <copyright file="PaymentController.cs" company="ERP Project">
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
[Route("api/v{version:apiVersion}/ap/payments")]
public class PaymentController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVoucherService _voucherService;

    public PaymentController(IUnitOfWork unitOfWork, IVoucherService voucherService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _voucherService = voucherService ?? throw new ArgumentNullException(nameof(voucherService));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetAll([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var payments = companyId.HasValue
            ? await _unitOfWork.Payments.FindAsync(x => x.CompanyId == companyId.Value, cancellationToken)
            : await _unitOfWork.Payments.GetAllAsync(cancellationToken);

        return Ok(payments.Select(p => MapToDto(p)).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(id, cancellationToken);
        if (payment == null)
            return NotFound();

        var lines = await _unitOfWork.PaymentLines.FindAsync(x => x.PaymentId == id, cancellationToken);
        return Ok(MapToDto(payment, lines.ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = await _voucherService.CreatePaymentAsync(
            request.CompanyId,
            request.VendorId,
            request.PaymentReference,
            request.PaymentDate,
            request.PaymentMethod,
            request.CurrencyCode,
            request.BankAccountId,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, MapToDto(payment, []));
    }

    [HttpPost("{id:guid}/select-vouchers")]
    public async Task<ActionResult<PaymentDto>> SelectVouchers(Guid id, [FromBody] SelectVouchersForPaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = await _voucherService.SelectVouchersForPaymentAsync(id, request.VoucherIds.ToList(), cancellationToken);
        var lines = await _unitOfWork.PaymentLines.FindAsync(x => x.PaymentId == id, cancellationToken);
        return Ok(MapToDto(payment, lines.ToList()));
    }

    [HttpPost("{id:guid}/issue")]
    public async Task<ActionResult<PaymentDto>> Issue(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _voucherService.IssuePaymentAsync(id, cancellationToken);
        var lines = await _unitOfWork.PaymentLines.FindAsync(x => x.PaymentId == id, cancellationToken);
        return Ok(MapToDto(payment, lines.ToList()));
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<PaymentDto>> Void(Guid id, [FromBody] VoidPaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = await _voucherService.VoidPaymentAsync(id, request.Reason, cancellationToken);
        var lines = await _unitOfWork.PaymentLines.FindAsync(x => x.PaymentId == id, cancellationToken);
        return Ok(MapToDto(payment, lines.ToList()));
    }

    private static PaymentDto MapToDto(Payment payment, List<PaymentLine>? lines = null)
    {
        lines ??= [];
        var lineDtos = lines.Select(l => new PaymentLineDto(l.Id, l.PaymentId, l.VoucherId, l.AppliedAmount)).ToList();
        return new PaymentDto(payment.Id, payment.CompanyId, payment.VendorId, payment.PaymentReference, payment.PaymentDate, payment.PaymentMethod, payment.CurrencyCode, payment.BankAccountId, payment.Status, payment.TotalAmount, lineDtos, payment.CreatedOn, payment.ModifiedOn);
    }
}