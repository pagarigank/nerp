// <copyright file="ExpenseController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;
using ERP.Core.Domain.Events;
using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Modules.Platform.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using ERP.Modules.ProjectAccounting.Domain.Entities;
using ERP.Modules.ProjectAccounting.Domain.Events;
using ERP.Modules.ProjectAccounting.Infrastructure;
using ERP.Shared.Kernel.Api;
using ERP.Shared.Kernel.Posting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

[ApiController]
[Route("api/v1/payroll/expenses")]
public class ExpenseController : ControllerBase
{
    private readonly PayrollDbContext _context;
    private readonly PlatformDbContext _platformContext;
    private readonly ProjDbContext _projContext;
    private readonly IPostingEventPublisher _postingPublisher;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ICurrentUserService _currentUser;
    private readonly ApVoucherCreator _apVoucherCreator;

    public ExpenseController(
        PayrollDbContext context,
        PlatformDbContext platformContext,
        ProjDbContext projContext,
        IPostingEventPublisher postingPublisher,
        IDomainEventDispatcher eventDispatcher,
        ICurrentUserService currentUser,
        ApVoucherCreator apVoucherCreator)
    {
        _context = context;
        _platformContext = platformContext;
        _projContext = projContext;
        _postingPublisher = postingPublisher;
        _eventDispatcher = eventDispatcher;
        _currentUser = currentUser;
        _apVoucherCreator = apVoucherCreator;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateReport(
        [FromBody] CreateExpenseReportRequest request, CancellationToken cancellationToken)
    {
        var report = new ExpenseReport(request.CompanyId, request.EmployeeId, request.ReportDate, request.Description);
        _context.ExpenseReports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(report.Id));
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<ApiResponse<Guid>>> AddLine(
        Guid id, [FromBody] AddExpenseLineRequest request, CancellationToken cancellationToken)
    {
        var report = await _context.ExpenseReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
            return NotFound(ApiResponse.Failure(new[] { "Expense report not found." }, 404));

        if (request.ProjectId.HasValue)
        {
            var project = await _projContext.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId.Value, cancellationToken);
            if (project is null || project.Status != ProjectStatus.Active)
                return BadRequest(ApiResponse.Failure(new[] { "Expense line project must be an active project." }));
        }

        var line = report.AddLine(
            request.Type, request.Amount, request.ExpenseDate, request.Description,
            request.ProjectId, request.TaskId, request.GlAccountNumber, request.ClientBillable,
            request.MileageMiles, request.MileageRate, request.PerDiemDays, request.PerDiemRate);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(line.Id));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse>> Submit(Guid id, CancellationToken cancellationToken)
    {
        var report = await _context.ExpenseReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
            return NotFound(ApiResponse.Failure(new[] { "Expense report not found." }, 404));
        report.Submit();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    /// <summary>Approval with threshold routing (mirrors Phase 1 approval workflow): amounts over the
    /// threshold require manager approval; here we still approve but record the routed requirement.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse>> Approve(
        Guid id, [FromBody] ApproveExpenseRequest request, CancellationToken cancellationToken)
    {
        var report = await _context.ExpenseReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
            return NotFound(ApiResponse.Failure(new[] { "Expense report not found." }, 404));

        const decimal managerThreshold = 500m;
        if (report.TotalAmount > managerThreshold && !request.ManagerApproved)
            return BadRequest(ApiResponse.Failure(new[] { $"Expenses over {managerThreshold:C} require manager approval (managerApproved=true)." }));

        report.Approve(request.ApprovedById);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse>> Reject(
        Guid id, [FromBody] RejectExpenseRequest request, CancellationToken cancellationToken)
    {
        var report = await _context.ExpenseReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
            return NotFound(ApiResponse.Failure(new[] { "Expense report not found." }, 404));
        report.Reject(request.Reason);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    /// <summary>Reimburse: posts AP liability via GL (Dr Expense / Cr AP Liabilities) and, for billable
    /// project lines, raises a project cost (mirrors the timesheet labor dual-post).</summary>
    [HttpPost("{id:guid}/reimburse")]
    public async Task<ActionResult<ApiResponse>> Reimburse(Guid id, CancellationToken cancellationToken)
    {
        var report = await _context.ExpenseReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (report is null)
            return NotFound(ApiResponse.Failure(new[] { "Expense report not found." }, 404));
        if (report.Status != ExpenseReportStatus.Approved)
            return BadRequest(ApiResponse.Failure(new[] { "Only an approved report can be reimbursed." }));
        if (report.TotalAmount <= 0m)
            return BadRequest(ApiResponse.Failure(new[] { "Report has no reimbursable amount (add lines with a non-zero amount)." }));

        var apLiabilityId = await ResolveAccountAsync(report.CompanyId, "2200", cancellationToken);
        var expenseAcctId = await ResolveAccountAsync(report.CompanyId, "6000", cancellationToken);
        var segments = ERP.Shared.Kernel.Posting.AccountKey.Create();

        var lines = new List<PostingLine>
        {
            new PostingLine { AccountId = expenseAcctId, Segments = segments, Debit = report.TotalAmount, Credit = 0m, Currency = "USD" },
            new PostingLine { AccountId = apLiabilityId, Segments = segments, Debit = 0m, Credit = report.TotalAmount, Currency = "USD" },
        };

        var period = await ResolveFiscalPeriodAsync(report.CompanyId, report.ReportDate, cancellationToken);
        var postedBy = _currentUser.UserId ?? "system";
        var batchNumber = $"EXPENSE-{report.Id:N}";

        var postingEvent = CanonicalPostingEvent.Create(
            "PAY", batchNumber, report.CompanyId, period?.Id ?? report.CompanyId,
            report.CompanyId.ToString(), (period?.Id ?? report.CompanyId).ToString(),
            new DateTimeOffset(report.ReportDate == default ? DateTime.UtcNow : DateTime.SpecifyKind(report.ReportDate, DateTimeKind.Utc)), lines,
            PostingMetadata.Create(postedBy, Guid.NewGuid()));
        await _postingPublisher.PublishAsync(postingEvent, cancellationToken);

        // Billable project lines post a cost to Project Accounting.
        foreach (var line in report.Lines.Where(l => l.ClientBillable && l.ProjectId.HasValue))
        {
            var ct = new CostTransaction(
                report.CompanyId, line.ProjectId.GetValueOrDefault(), line.TaskId.GetValueOrDefault(),
                CostCategory.Other, CostTransactionType.ManualAdjustment, line.Amount,
                0m, line.Description ?? line.Type.ToString(), report.Id, "ExpenseReport",
                true, null, report.EmployeeId);
            _projContext.CostTransactions.Add(ct);
            await _projContext.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(new ProjectCostPostedEvent(
                ct.Id, ct.ProjectId, ct.TaskId, ct.Category.ToString(), ct.Amount, ct.CompanyId), cancellationToken);
        }

        report.MarkReimbursed();
        await _context.SaveChangesAsync(cancellationToken);

        // Phase 11 cross-module wiring (#1101): create an AP voucher with the employee as
        // the vendor payee so the reimbursement is paid through the normal AP payment run.
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == report.EmployeeId, cancellationToken);
        if (employee is null)
            return BadRequest(ApiResponse.Failure(new[] { "Employee not found for reimbursement." }));

        var voucherId = await _apVoucherCreator.CreateReimbursementVoucherAsync(report, employee, cancellationToken);

        return Ok(ApiResponse<string>.Success(voucherId.ToString()));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ExpenseReportDto>>>> List(
        [FromQuery] Guid? employeeId, CancellationToken cancellationToken)
    {
        var query = _context.ExpenseReports.AsQueryable();
        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        var list = await query
            .OrderByDescending(r => r.ReportDate)
            .Select(r => new ExpenseReportDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                ReportDate = r.ReportDate,
                Status = r.Status.ToString(),
                TotalAmount = r.TotalAmount,
                LineCount = r.Lines.Count,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<ExpenseReportDto>>.Success(list));
    }

    private async Task<Guid> ResolveAccountAsync(Guid companyId, string accountNumber, CancellationToken cancellationToken)
    {
        var acct = await _platformContext.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.CompanyId == companyId, cancellationToken);
        acct ??= await _platformContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
        if (acct is null)
            throw new InvalidOperationException($"GL account {accountNumber} not found for company {companyId}.");
        return acct.Id;
    }

    private async Task<FiscalPeriod?> ResolveFiscalPeriodAsync(Guid companyId, DateTime transactionDate, CancellationToken cancellationToken)
    {
        var date = transactionDate == default ? DateTimeOffset.UtcNow : new DateTimeOffset(DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc));
        return await _platformContext.FiscalPeriods
            .Where(p => p.CompanyId == companyId && p.StartDate <= date && p.EndDate >= date)
            .OrderBy(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class CreateExpenseReportRequest
{
    public Guid CompanyId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime ReportDate { get; set; }
    public string? Description { get; set; }
}

public class AddExpenseLineRequest
{
    public ExpenseType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? TaskId { get; set; }
    public string? GlAccountNumber { get; set; }
    public bool ClientBillable { get; set; }
    public decimal? MileageMiles { get; set; }
    public decimal? MileageRate { get; set; }
    public decimal? PerDiemDays { get; set; }
    public decimal? PerDiemRate { get; set; }
}

public class ApproveExpenseRequest
{
    public Guid ApprovedById { get; set; }
    public bool ManagerApproved { get; set; }
}

public class RejectExpenseRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class ExpenseReportDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime ReportDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int LineCount { get; set; }
}
