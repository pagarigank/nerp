// <copyright file="BankFeeService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Infrastructure;

public interface IBankFeeService
{
    Task<BankFee> RecordAsync(
        Guid companyId,
        Guid bankAccountId,
        string feeNumber,
        BankFeeType feeType,
        decimal amount,
        DateTimeOffset feeDate,
        string? description,
        Guid expenseGlAccountId,
        string postedBy,
        CancellationToken cancellationToken = default);
}

public class BankFeeService : IBankFeeService
{
    private readonly CashDbContext _context;
    private readonly PlatformDbContext _platformContext;
    private readonly IPostingEventPublisher _postingPublisher;

    public BankFeeService(
        CashDbContext context,
        PlatformDbContext platformContext,
        IPostingEventPublisher postingPublisher)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _postingPublisher = postingPublisher ?? throw new ArgumentNullException(nameof(postingPublisher));
    }

    public async Task<BankFee> RecordAsync(
        Guid companyId,
        Guid bankAccountId,
        string feeNumber,
        BankFeeType feeType,
        decimal amount,
        DateTimeOffset feeDate,
        string? description,
        Guid expenseGlAccountId,
        string postedBy,
        CancellationToken cancellationToken = default)
    {
        var bankAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == bankAccountId && a.CompanyId == companyId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {bankAccountId} not found.");

        if (!bankAccount.GlAccountId.HasValue || bankAccount.GlAccountId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Bank account is not mapped to a GL cash account; bank fee posting requires a GL account mapping.");
        }

        var fee = new BankFee(companyId, bankAccountId, feeNumber, feeType, amount, feeDate, description);
        fee.CreatedBy = postedBy;

        var fiscalPeriod = await _platformContext.FiscalPeriods
            .Where(p => p.CompanyId == companyId
                && p.StartDate <= feeDate
                && p.EndDate >= feeDate)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await _platformContext.FiscalPeriods
                .Where(p => p.CompanyId == companyId && p.Status == PeriodStatus.Open)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No open fiscal period found for the company.");

        var lines = new List<PostingLine>
        {
            new PostingLine
            {
                Account = expenseGlAccountId.ToString(),
                AccountId = expenseGlAccountId,
                Segments = AccountKey.Create(),
                Debit = amount,
                Credit = 0,
                Currency = "USD"
            },
            new PostingLine
            {
                Account = bankAccount.GlAccountId.Value.ToString(),
                AccountId = bankAccount.GlAccountId.Value,
                Segments = AccountKey.Create(),
                Debit = 0,
                Credit = amount,
                Currency = "USD"
            }
        };

        var postingEvent = CanonicalPostingEvent.Create(
            "CASH",
            $"FEE-{feeNumber}",
            companyId,
            fiscalPeriod.Id,
            companyId.ToString(),
            fiscalPeriod.Id.ToString(),
            feeDate,
            lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid(), vendorId: null, projectId: null));

        var glBatchId = await _postingPublisher.PublishAsync(postingEvent, cancellationToken);

        fee.AttachGlJournal(glBatchId);
        fee.Post();
        bankAccount.AdjustBalance(-amount);
        bankAccount.MarkModified(postedBy);

        _context.BankFees.Add(fee);
        await _context.SaveChangesAsync(cancellationToken);

        return fee;
    }
}
