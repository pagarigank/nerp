// <copyright file="ReconciliationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsPayable.Domain.Entities;
using ERP.Modules.AccountsPayable.Infrastructure;
using ERP.Modules.AccountsReceivable.Infrastructure;
using ERP.Modules.CashManagement.Domain.Entities;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Shared.Kernel.Posting;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.CashManagement.Infrastructure;

public interface IReconciliationService
{
    Task<ReconciliationSession> CreateSessionAsync(
        Guid statementId,
        string sessionNumber,
        string createdBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutoMatchResult>> RunAutoMatchAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<BankStatementLine> MarkLineMatchedAsync(
        Guid sessionId,
        Guid statementLineId,
        Guid transactionId,
        BankMatchSource source,
        string clearedBy,
        CancellationToken cancellationToken = default);

    Task<BankStatementLine> MarkLineClearedAsync(
        Guid sessionId,
        Guid statementLineId,
        string clearedBy,
        CancellationToken cancellationToken = default);

    Task<BankStatementLine> MarkLineUnmatchedAsync(
        Guid sessionId,
        Guid statementLineId,
        string clearedBy,
        CancellationToken cancellationToken = default);

    Task<ReconciliationSession> LockSessionAsync(
        Guid sessionId,
        Guid varianceGlAccountId,
        decimal tolerance,
        string lockedBy,
        CancellationToken cancellationToken = default);
}

public class ReconciliationService : IReconciliationService
{
    public const decimal DefaultVarianceTolerance = 10.0m;

    private readonly CashDbContext _context;
    private readonly ApDbContext _apContext;
    private readonly ArDbContext _arContext;
    private readonly PlatformDbContext _platformContext;
    private readonly IAutoMatchService _autoMatchService;
    private readonly IPostingEventPublisher _postingPublisher;

    public ReconciliationService(
        CashDbContext context,
        ApDbContext apContext,
        ArDbContext arContext,
        PlatformDbContext platformContext,
        IAutoMatchService autoMatchService,
        IPostingEventPublisher postingPublisher)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _apContext = apContext ?? throw new ArgumentNullException(nameof(apContext));
        _arContext = arContext ?? throw new ArgumentNullException(nameof(arContext));
        _platformContext = platformContext ?? throw new ArgumentNullException(nameof(platformContext));
        _autoMatchService = autoMatchService ?? throw new ArgumentNullException(nameof(autoMatchService));
        _postingPublisher = postingPublisher ?? throw new ArgumentNullException(nameof(postingPublisher));
    }

    public async Task<ReconciliationSession> CreateSessionAsync(
        Guid statementId,
        string sessionNumber,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var statement = await _context.BankStatements
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == statementId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank statement {statementId} not found.");

        if (statement.Status == BankStatementStatus.Locked)
            throw new InvalidOperationException("A locked statement cannot be reconciled.");

        if (string.IsNullOrWhiteSpace(sessionNumber))
            throw new ArgumentException("Session number is required.", nameof(sessionNumber));

        var session = new ReconciliationSession(
            statement.CompanyId,
            statement.BankAccountId,
            statement.Id,
            sessionNumber,
            statement.StatementDate,
            statement.BeginningBalance,
            statement.EndingBalance);

        session.CreatedBy = createdBy;
        _context.ReconciliationSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<IReadOnlyList<AutoMatchResult>> RunAutoMatchAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var statement = await _context.BankStatements
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == session.BankStatementId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank statement {session.BankStatementId} not found.");

        var candidates = await LoadCandidatesAsync(session.CompanyId, session.BankAccountId, cancellationToken);

        var openLines = statement.Lines
            .Where(l => l.Status == BankStatementLineStatus.Unreconciled)
            .ToList();

        var results = _autoMatchService.MatchAll(openLines, candidates);

        foreach (var result in results.Where(r => r.Confidence >= MatchConfidence.Probable && r.Candidate != null))
        {
            var line = statement.Lines.First(l => l.Id == result.StatementLineId);
            line.MarkMatched(result.Candidate!.Id, result.Candidate.Source);
        }

        if (results.Any(r => r.Confidence >= MatchConfidence.Probable))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return results;
    }

    public async Task<BankStatementLine> MarkLineMatchedAsync(
        Guid sessionId,
        Guid statementLineId,
        Guid transactionId,
        BankMatchSource source,
        string clearedBy,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var line = await GetLineAsync(session, statementLineId, cancellationToken);

        line.MarkMatched(transactionId, source);
        await _context.SaveChangesAsync(cancellationToken);
        return line;
    }

    public async Task<BankStatementLine> MarkLineClearedAsync(
        Guid sessionId,
        Guid statementLineId,
        string clearedBy,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var line = await GetLineAsync(session, statementLineId, cancellationToken);

        line.MarkCleared();
        await _context.SaveChangesAsync(cancellationToken);
        return line;
    }

    public async Task<BankStatementLine> MarkLineUnmatchedAsync(
        Guid sessionId,
        Guid statementLineId,
        string clearedBy,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var line = await GetLineAsync(session, statementLineId, cancellationToken);

        line.MarkUnmatched();
        await _context.SaveChangesAsync(cancellationToken);
        return line;
    }

    public async Task<ReconciliationSession> LockSessionAsync(
        Guid sessionId,
        Guid varianceGlAccountId,
        decimal tolerance,
        string lockedBy,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        var statement = await _context.BankStatements
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == session.BankStatementId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank statement {session.BankStatementId} not found.");

        var bookEndingBalance = session.BeginningBalance
            + statement.Lines.Where(l => l.Status is BankStatementLineStatus.Matched or BankStatementLineStatus.Cleared or BankStatementLineStatus.Locked)
                .Sum(l => l.Amount);

        var variance = Math.Round(session.EndingBalance - bookEndingBalance, 2);
        session.RecordVariance(variance);

        if (Math.Abs(variance) > tolerance)
        {
            throw new InvalidOperationException(
                $"Unreconciled variance {variance:C} exceeds tolerance {tolerance:C}. " +
                "Resolve the variance before locking the reconciliation.");
        }

        Guid? glBatchId = null;
        if (variance != 0)
        {
            glBatchId = await PostVarianceToGlAsync(session, varianceGlAccountId, variance, lockedBy, cancellationToken);
            session.AttachGlJournal(glBatchId.Value);
        }

        statement.MarkReconciled();
        statement.MarkLocked();

        foreach (var line in statement.Lines.Where(l => l.Status is BankStatementLineStatus.Matched or BankStatementLineStatus.Cleared))
        {
            line.Lock();
        }

        var bankAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == session.BankAccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {session.BankAccountId} not found.");

        bankAccount.AdjustBalance(variance);
        bankAccount.MarkModified(lockedBy);

        // Mark matched AP payments and deposits as cleared in their source modules.
        await MarkSourceTransactionsClearedAsync(statement, cancellationToken);

        session.Lock(lockedBy);

        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    private async Task<Guid> PostVarianceToGlAsync(
        ReconciliationSession session,
        Guid varianceGlAccountId,
        decimal variance,
        string postedBy,
        CancellationToken cancellationToken)
    {
        if (variance == 0)
            return Guid.Empty;

        var bankAccount = await _context.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == session.BankAccountId, cancellationToken)
            ?? throw new InvalidOperationException($"Bank account {session.BankAccountId} not found.");

        if (!bankAccount.GlAccountId.HasValue || bankAccount.GlAccountId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Bank account is not mapped to a GL cash account; variance-to-GL posting requires a GL account mapping.");
        }

        var fiscalPeriod = await _platformContext.FiscalPeriods
            .Where(p => p.CompanyId == session.CompanyId
                && p.StartDate <= session.StatementDate
                && p.EndDate >= session.StatementDate)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await _platformContext.FiscalPeriods
                .Where(p => p.CompanyId == session.CompanyId && p.Status == PeriodStatus.Open)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No open fiscal period found for the company.");

        var cashGlAccountId = bankAccount.GlAccountId.Value;
        var offsetGlAccountId = varianceGlAccountId;

        // Debit/credit orientation follows the original GL write:
        //   variance > 0 (bank > book): debit cash, credit variance offset
        //   variance < 0 (bank < book): debit variance offset, credit cash
        var lines = new List<PostingLine>();
        if (variance > 0)
        {
            lines.Add(new PostingLine
            {
                Account = cashGlAccountId.ToString(),
                AccountId = cashGlAccountId,
                Segments = AccountKey.Create(),
                Debit = variance,
                Credit = 0,
                Currency = "USD"
            });
            lines.Add(new PostingLine
            {
                Account = offsetGlAccountId.ToString(),
                AccountId = offsetGlAccountId,
                Segments = AccountKey.Create(),
                Debit = 0,
                Credit = variance,
                Currency = "USD"
            });
        }
        else
        {
            var absolute = Math.Abs(variance);
            lines.Add(new PostingLine
            {
                Account = offsetGlAccountId.ToString(),
                AccountId = offsetGlAccountId,
                Segments = AccountKey.Create(),
                Debit = absolute,
                Credit = 0,
                Currency = "USD"
            });
            lines.Add(new PostingLine
            {
                Account = cashGlAccountId.ToString(),
                AccountId = cashGlAccountId,
                Segments = AccountKey.Create(),
                Debit = 0,
                Credit = absolute,
                Currency = "USD"
            });
        }

        var postingEvent = CanonicalPostingEvent.Create(
            "CASH",
            $"CASH-{session.SessionNumber}",
            session.CompanyId,
            fiscalPeriod.Id,
            session.CompanyId.ToString(),
            fiscalPeriod.Id.ToString(),
            session.StatementDate,
            lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid(), vendorId: null, projectId: null));

        return await _postingPublisher.PublishAsync(postingEvent, cancellationToken);
    }

    private async Task MarkSourceTransactionsClearedAsync(BankStatement statement, CancellationToken cancellationToken)
    {
        var paymentsToClear = statement.Lines
            .Where(l => l.MatchedSource == BankMatchSource.ApPayment && l.MatchedTransactionId.HasValue)
            .Select(l => l.MatchedTransactionId!.Value)
            .Distinct()
            .ToList();

        foreach (var paymentId in paymentsToClear)
        {
            var payment = await _apContext.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.Status == PaymentStatus.Issued, cancellationToken);
            payment?.Clear();
        }

        var depositsToClear = statement.Lines
            .Where(l => l.MatchedSource == BankMatchSource.Deposit && l.MatchedTransactionId.HasValue)
            .Select(l => l.MatchedTransactionId!.Value)
            .Distinct()
            .ToList();

        foreach (var depositId in depositsToClear)
        {
            var deposit = await _context.Deposits
                .FirstOrDefaultAsync(d => d.Id == depositId && d.Status == DepositStatus.Confirmed, cancellationToken);
            deposit?.Clear();
        }

        if (paymentsToClear.Count > 0 || depositsToClear.Count > 0)
        {
            await _apContext.SaveChangesAsync(cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<AutoMatchCandidate>> LoadCandidatesAsync(
        Guid companyId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var candidates = new List<AutoMatchCandidate>();

        var payments = await _apContext.Payments
            .Where(p => p.CompanyId == companyId
                && p.Status == PaymentStatus.Issued
                && p.BankAccountId == bankAccountId)
            .ToListAsync(cancellationToken);

        foreach (var payment in payments)
        {
            candidates.Add(new AutoMatchCandidate(
                payment.Id,
                BankMatchSource.ApPayment,
                payment.PaymentReference,
                -payment.TotalAmount,
                payment.PaymentDate,
                payment.PaymentMethod == PaymentMethod.Check ? payment.PaymentReference : null,
                $"AP Payment {payment.PaymentReference}"));
        }

        var receipts = await _arContext.CashReceipts
            .Where(r => r.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        foreach (var receipt in receipts)
        {
            candidates.Add(new AutoMatchCandidate(
                receipt.Id,
                BankMatchSource.ArCashReceipt,
                receipt.ReceiptReference,
                receipt.TotalAmount,
                receipt.ReceiptDate,
                receipt.ReferenceNumber,
                $"AR Receipt {receipt.ReceiptReference}"));
        }

        var deposits = await _context.Deposits
            .Where(d => d.CompanyId == companyId
                && d.BankAccountId == bankAccountId
                && d.Status == DepositStatus.Confirmed)
            .Include(d => d.Lines)
            .ToListAsync(cancellationToken);

        foreach (var deposit in deposits)
        {
            candidates.Add(new AutoMatchCandidate(
                deposit.Id,
                BankMatchSource.Deposit,
                deposit.DepositNumber,
                deposit.TotalAmount,
                deposit.DepositDate,
                null,
                $"Deposit {deposit.DepositNumber}"));
        }

        return candidates;
    }

    private async Task<ReconciliationSession> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _context.ReconciliationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Reconciliation session {sessionId} not found.");

        if (session.Status == ReconciliationStatus.Locked)
            throw new InvalidOperationException("Reconciliation session is locked and cannot be modified.");

        return session;
    }

    private async Task<BankStatementLine> GetLineAsync(ReconciliationSession session, Guid lineId, CancellationToken cancellationToken)
    {
        var line = await _context.BankStatementLines
            .FirstOrDefaultAsync(l => l.Id == lineId && l.BankStatementId == session.BankStatementId, cancellationToken)
            ?? throw new InvalidOperationException($"Statement line {lineId} not found in session {session.SessionNumber}.");

        return line;
    }
}
