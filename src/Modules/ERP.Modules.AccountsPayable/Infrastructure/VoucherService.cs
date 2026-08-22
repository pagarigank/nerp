// <copyright file="VoucherService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.AccountsPayable.Infrastructure;

public class VoucherService : IVoucherService
{
    private readonly ApDbContext _context;
    private readonly IPostingEventPublisher _postingPublisher;
    private readonly ICurrentUserService _currentUser;
    private readonly IPeriodService _periodService;
    private readonly ISodService _sodService;

    public VoucherService(
        ApDbContext context,
        IPostingEventPublisher postingPublisher,
        ICurrentUserService currentUser,
        IPeriodService periodService,
        ISodService sodService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _postingPublisher = postingPublisher ?? throw new ArgumentNullException(nameof(postingPublisher));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _periodService = periodService ?? throw new ArgumentNullException(nameof(periodService));
        _sodService = sodService ?? throw new ArgumentNullException(nameof(sodService));
    }

    public async Task<VoucherBatch> CreateVoucherBatchAsync(
        Guid companyId,
        string batchNumber,
        string description,
        DateTimeOffset postingDate,
        Guid fiscalPeriodId,
        CancellationToken cancellationToken = default)
    {
        var batch = new VoucherBatch(companyId, batchNumber, description, postingDate, fiscalPeriodId);
        _context.VoucherBatches.Add(batch);
        await _context.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task<Voucher> AddVoucherToBatchAsync(
        Guid batchId,
        Guid vendorId,
        VoucherType voucherType,
        string invoiceNumber,
        DateTimeOffset invoiceDate,
        DateTimeOffset dueDate,
        decimal totalAmount,
        decimal discountAmount,
        string? description,
        Guid? paymentTermId,
        Guid? purchaseOrderId,
        Guid? receiptLineId,
        decimal form1099Amount,
        decimal backupWithholdingAmount,
        IReadOnlyList<VoucherDistributionDto> distributions,
        CancellationToken cancellationToken = default)
    {
        var batch = await _context.VoucherBatches
            .Include(b => b.Vouchers)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException($"Voucher batch {batchId} not found.");

        var voucher = batch.AddVoucher(
            vendorId,
            voucherType,
            invoiceNumber,
            invoiceDate,
            dueDate,
            totalAmount,
            discountAmount,
            description,
            paymentTermId,
            purchaseOrderId,
            receiptLineId);

        foreach (var dist in distributions)
        {
            voucher.AddDistribution(dist.AccountId, dist.Debit, dist.Credit, dist.ProjectId, dist.TaskId);
        }

        if (!voucher.IsBalanced())
        {
            throw new InvalidOperationException("Voucher distributions must balance (debits = credits).");
        }

        if (form1099Amount > 0)
        {
            voucher.Set1099Amount(form1099Amount);
        }

        if (backupWithholdingAmount > 0)
        {
            voucher.SetBackupWithholding(backupWithholdingAmount);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return voucher;
    }

    public async Task<VoucherBatch> ReleaseBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.VoucherBatches
            .Include(b => b.Vouchers)
                .ThenInclude(v => v.Distributions)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException($"Voucher batch {batchId} not found.");

        batch.Release();
        await _context.SaveChangesAsync(cancellationToken);
        return batch;
    }

    public async Task<VoucherBatch> PostBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.VoucherBatches
            .Include(b => b.Vouchers)
                .ThenInclude(v => v.Distributions)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException($"Voucher batch {batchId} not found.");

        var companyId = batch.CompanyId;
        var fiscalPeriodId = batch.FiscalPeriodId;
        var postingDate = batch.PostingDate;
        var postedBy = _currentUser.UserId ?? "system";

        // Period control: a batch may only be posted into an open fiscal period.
        // Posting into a closed period is rejected here so the GL control
        // accounts and financial statements remain trustworthy.
        if (!await _periodService.IsPeriodOpenAsync(companyId, postingDate, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Cannot post voucher batch {batch.BatchNumber}: fiscal period for {postingDate:yyyy-MM-dd} is not open.");
        }

        // Separation of Duties: the user who created the batch must not also post
        // it. The create action is recorded in the audit trail by the save
        // interceptor, so we check the conflicting "Created" action here.
        if (!string.IsNullOrEmpty(_currentUser.UserId)
            && await _sodService.CheckConflictAsync(
                "AccountsPayable", nameof(VoucherBatch), _currentUser.UserId, "Post", 0, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Separation of Duties conflict: user {_currentUser.UserId} created voucher batch {batch.BatchNumber} and may not also post it.");
        }

        batch.Post();
        await _context.SaveChangesAsync(cancellationToken);

        // Dual-write to the General Ledger through the canonical posting contract
        // (architecture.md §5.1). The GL is the system of record for every
        // sub-ledger; AP must never post directly to control accounts.
        var lines = new List<PostingLine>();
        foreach (var voucher in batch.Vouchers)
        {
            foreach (var distribution in voucher.Distributions)
            {
                var segments = AccountKey.Create();
                if (distribution.ProjectId.HasValue)
                    segments = segments.WithSegment("PROJECT", distribution.ProjectId.Value.ToString());
                if (distribution.TaskId.HasValue)
                    segments = segments.WithSegment("TASK", distribution.TaskId.Value.ToString());

                lines.Add(new PostingLine
                {
                    Account = distribution.AccountId.ToString(),
                    AccountId = distribution.AccountId,
                    Segments = segments,
                    Debit = distribution.Debit,
                    Credit = distribution.Credit,
                    Currency = "USD"
                });
            }
        }

        if (lines.Count > 0)
        {
            var postingEvent = CanonicalPostingEvent.Create(
                "AP",
                $"VCH-{batch.BatchNumber}",
                companyId,
                fiscalPeriodId,
                companyId.ToString(),
                fiscalPeriodId.ToString(),
                postingDate,
                lines,
                PostingMetadata.Create(postedBy, Guid.NewGuid(), vendorId: null, projectId: null));

            await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
        }

        return batch;
    }

    public async Task<VoucherBatch> ReverseBatchAsync(Guid batchId, string reason, CancellationToken cancellationToken = default)
    {
        var batch = await _context.VoucherBatches
            .Include(b => b.Vouchers)
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException($"Voucher batch {batchId} not found.");

        var reversal = batch.Reverse(reason);
        _context.VoucherBatches.Add(reversal);
        await _context.SaveChangesAsync(cancellationToken);
        return reversal;
    }

    public async Task<Payment> CreatePaymentAsync(
        Guid companyId,
        Guid vendorId,
        string paymentReference,
        DateTimeOffset paymentDate,
        PaymentMethod paymentMethod,
        string currencyCode,
        Guid? bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var payment = new Payment(companyId, vendorId, paymentReference, paymentDate, paymentMethod, currencyCode, bankAccountId);
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> SelectVouchersForPaymentAsync(
        Guid paymentId,
        IReadOnlyList<Guid> voucherIds,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {paymentId} not found.");

        var heldVendorIds = await _context.Vendors
            .Where(v => v.OnHold)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var vouchers = await _context.Vouchers
            .Where(v => voucherIds.Contains(v.Id) && !heldVendorIds.Contains(v.VendorId))
            .ToListAsync(cancellationToken);

        foreach (var voucher in vouchers)
        {
            var existingLine = payment.Lines.FirstOrDefault(l => l.VoucherId == voucher.Id);
            if (existingLine == null)
            {
                if (!voucher.SelectedForPayment)
                {
                    voucher.MarkSelectedForPayment();
                }

                payment.AddVoucher(voucher, voucher.TotalAmount);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> IssuePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {paymentId} not found.");

        payment.Issue();
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> VoidPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {paymentId} not found.");

        payment.Void(reason);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }
}