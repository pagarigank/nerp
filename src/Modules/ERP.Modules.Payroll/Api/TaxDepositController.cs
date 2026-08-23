// <copyright file="TaxDepositController.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Payroll.Domain.Entities;
using ERP.Modules.Payroll.Infrastructure;
using ERP.Shared.Kernel.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Payroll.Api;

[ApiController]
[Route("api/v1/payroll")]
public class TaxDepositController : ControllerBase
{
    private readonly PayrollDbContext _context;
    private readonly ApVoucherCreator _voucherCreator;

    public TaxDepositController(PayrollDbContext context, ApVoucherCreator voucherCreator)
    {
        _context = context;
        _voucherCreator = voucherCreator;
    }

    [HttpPost("tax-deposits")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateTaxDeposit(
        [FromBody] CreateTaxDepositRequest request, CancellationToken cancellationToken)
    {
        var sched = new TaxDepositSchedule(request.CompanyId, request.TaxType, request.Agency,
            request.PayrollRunId, request.DepositDate, request.EstimatedAmount, request.Frequency, request.FormType);
        _context.TaxDepositSchedules.Add(sched);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<Guid>.Success(sched.Id));
    }

    [HttpGet("tax-deposits")]
    public async Task<ActionResult<ApiResponse<List<TaxDepositDto>>>> GetTaxDeposits(
        [FromQuery] Guid companyId, CancellationToken cancellationToken)
    {
        var list = await _context.TaxDepositSchedules
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.DepositDate)
            .Select(s => new TaxDepositDto
            {
                Id = s.Id,
                TaxType = s.TaxType,
                Agency = s.Agency,
                DepositDate = s.DepositDate,
                EstimatedAmount = s.EstimatedAmount,
                DepositedAmount = s.DepositedAmount,
                DepositedOn = s.DepositedOn,
                Frequency = s.Frequency,
                FormType = s.FormType,
                Deposited = s.Deposited,
            }).ToListAsync(cancellationToken);
        return Ok(ApiResponse<List<TaxDepositDto>>.Success(list));
    }

    [HttpPost("tax-deposits/{id:guid}/deposit")]
    public async Task<ActionResult<ApiResponse>> MarkDeposited(
        Guid id, [FromBody] MarkDepositedRequest request, CancellationToken cancellationToken)
    {
        var sched = await _context.TaxDepositSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (sched is null) return NotFound(ApiResponse.Failure(new[] { "Tax deposit schedule not found." }, 404));
        sched.MarkDeposited(request.DepositedAmount, request.DepositedOn);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse.Success());
    }

    /// <summary>
    /// Auto-schedules federal (941/EFTPS) and FUTA deposits for a posted run based on
    /// the company deposit schedule. Semi-weekly deposits land the next banking day +3/5;
    /// monthly deposits land on the 15th of the following month. Returns the created schedules.
    /// </summary>
    [HttpPost("tax-deposits/generate/{runId:guid}")]
    public async Task<ActionResult<ApiResponse<List<Guid>>>> GenerateForRun(
        Guid runId, [FromQuery] string frequency, CancellationToken cancellationToken)
    {
        var run = await _context.PayrollRuns
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null) return NotFound(ApiResponse.Failure(new[] { "Payroll run not found." }, 404));
        if (run.Status != PayrollRunStatus.Posted)
            return BadRequest(ApiResponse.Failure(new[] { "Only a posted run can generate tax deposits." }));

        var created = new List<Guid>();
        var fedTax = run.TotalEmployeeTax + run.TotalEmployerTax;
        var futa = Math.Round(run.TotalGross * 0.006m, 2);

        DateTime FedDate() => frequency.Equals("SemiWeekly", StringComparison.OrdinalIgnoreCase)
            ? run.PayDate.AddDays(3)
            : run.PayDate.AddMonths(1).AddDays(15 - run.PayDate.Day);

        var fed = new TaxDepositSchedule(run.CompanyId, "Federal941", "EFTPS", runId, FedDate(), fedTax, frequency, "941");
        _context.TaxDepositSchedules.Add(fed);
        created.Add(fed.Id);

        var futaSched = new TaxDepositSchedule(run.CompanyId, "FUTA", "EFTPS", runId,
            new DateTime(run.PayDate.Year, 1, 31, 0, 0, 0, DateTimeKind.Utc).AddYears(run.PayDate.Month > 3 ? 1 : 0), futa,
            "Quarterly", "940");
        _context.TaxDepositSchedules.Add(futaSched);
        created.Add(futaSched.Id);

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<List<Guid>>.Success(created));
    }

    // --- Payroll liability payment: preview + pay unpaid tax/benefit liabilities via AP vouchers ---
    [HttpGet("liability-payments/pending")]
    public async Task<ActionResult<ApiResponse<List<LiabilityPaymentGroupDto>>>> GetPendingLiabilityPayments(
        [FromQuery] Guid companyId, [FromQuery] DateTime? payThroughDate, CancellationToken cancellationToken)
    {
        var groups = await BuildPendingGroupsAsync(companyId, payThroughDate ?? DateTime.UtcNow, null, cancellationToken);
        return Ok(ApiResponse<List<LiabilityPaymentGroupDto>>.Success(groups));
    }

    [HttpPost("liability-payments")]
    public async Task<ActionResult<ApiResponse<List<LiabilityPaymentResultDto>>>> PayLiabilities(
        [FromBody] PayLiabilitiesRequest request, CancellationToken cancellationToken)
    {
        var through = request.PayThroughDate == default ? DateTime.UtcNow : request.PayThroughDate;
        var groups = await BuildPendingGroupsAsync(request.CompanyId, through, request.Agencies, cancellationToken);
        if (groups.Count == 0)
            return Ok(ApiResponse<List<LiabilityPaymentResultDto>>.Success([]));

        var results = new List<LiabilityPaymentResultDto>();
        foreach (var group in groups)
        {
            var voucherId = await _voucherCreator.CreateLiabilityPaymentVoucherAsync(
                request.CompanyId,
                group.VendorCode,
                ResolveVendorName(group.VendorCode),
                group.Amount,
                through,
                cancellationToken);

            var schedules = await _context.TaxDepositSchedules
                .Where(s => group.TaxDepositIds.Contains(s.Id))
                .ToListAsync(cancellationToken);
            foreach (var schedule in schedules)
                schedule.MarkPaid(voucherId, through);

            if (group.Kind == LiabilityKind.BenefitRemittance && group.RemittedThrough.HasValue)
            {
                var setup = await _context.CompanyPayrollSetups
                    .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, cancellationToken);
                setup?.MarkBenefitRemittedThrough(new DateTimeOffset(DateTime.SpecifyKind(group.RemittedThrough.Value, DateTimeKind.Utc)));
            }

            results.Add(new LiabilityPaymentResultDto
            {
                VendorCode = group.VendorCode,
                AgencyName = group.AgencyName,
                Kind = group.Kind.ToString(),
                Amount = group.Amount,
                DepositCount = group.DepositCount,
                VoucherId = voucherId,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<List<LiabilityPaymentResultDto>>.Success(results));
    }

    private async Task<List<LiabilityPaymentGroupDto>> BuildPendingGroupsAsync(
        Guid companyId, DateTime payThroughDate, List<string>? agencyFilter, CancellationToken cancellationToken)
    {
        var setup = await _context.CompanyPayrollSetups
            .FirstOrDefaultAsync(s => s.CompanyId == companyId, cancellationToken);

        var pendingSchedules = await _context.TaxDepositSchedules
            .Where(s => s.CompanyId == companyId && !s.Deposited && s.DepositDate <= payThroughDate)
            .OrderBy(s => s.DepositDate)
            .ToListAsync(cancellationToken);

        var postedRuns = await _context.PayrollRuns
            .Include(r => r.Lines)
            .Where(r => r.CompanyId == companyId && r.Status == PayrollRunStatus.Posted)
            .ToListAsync(cancellationToken);

        var coveredRunIds = await _context.TaxDepositSchedules
            .Where(s => s.CompanyId == companyId && s.PayrollRunId != null)
            .Select(s => s.PayrollRunId!.Value)
            .ToListAsync(cancellationToken);
        var covered = coveredRunIds.ToHashSet();

        var filter = agencyFilter is { Count: > 0 }
            ? agencyFilter.Select(a => a.Trim().ToUpperInvariant()).ToHashSet()
            : null;

        var groups = new Dictionary<string, LiabilityPaymentGroupDto>();
        foreach (var sched in pendingSchedules)
        {
            var vendorCode = ResolveAgencyVendorCode(sched.TaxType, sched.Agency, setup?.SutaState);
            if (filter is not null && !filter.Contains(vendorCode.ToUpperInvariant()))
                continue;
            if (!groups.TryGetValue(vendorCode, out var group))
            {
                group = new LiabilityPaymentGroupDto
                {
                    VendorCode = vendorCode,
                    AgencyName = string.IsNullOrWhiteSpace(sched.Agency) ? vendorCode : sched.Agency,
                    Kind = LiabilityKind.TaxDeposit,
                };
                groups.Add(vendorCode, group);
            }

            group.Amount += sched.EstimatedAmount;
            group.DepositCount++;
            group.TaxDepositIds.Add(sched.Id);
        }

        // Posted-run taxes with no deposit schedule generated yet are still unpaid liabilities.
        foreach (var run in postedRuns)
        {
            if (covered.Contains(run.Id))
                continue;
            var federalDue = run.TotalEmployeeTax + run.TotalEmployerTax;
            if (federalDue <= 0m)
                continue;
            if (!groups.TryGetValue(FederalVendorCode, out var group))
            {
                group = new LiabilityPaymentGroupDto
                {
                    VendorCode = FederalVendorCode,
                    AgencyName = "EFTPS",
                    Kind = LiabilityKind.TaxDeposit,
                };
                groups.Add(FederalVendorCode, group);
            }

            group.Amount += federalDue;
            group.DepositCount++;
            group.UncoveredPostedRunCount++;
        }

        // Benefit remittances due: employee deductions on posted runs newer than the remittance watermark.
        var watermark = setup?.BenefitRemittancePaidThrough ?? DateTimeOffset.MinValue;
        var benefitRuns = postedRuns
            .Where(r => r.TotalDeductions > 0m
                        && new DateTimeOffset(DateTime.SpecifyKind(r.PayDate, DateTimeKind.Utc)) > watermark)
            .OrderBy(r => r.PayDate)
            .ToList();
        if (benefitRuns.Count > 0)
        {
            var benefitGroup = new LiabilityPaymentGroupDto
            {
                VendorCode = BenefitVendorCode,
                AgencyName = "Benefit Plan Remittance",
                Kind = LiabilityKind.BenefitRemittance,
                Amount = benefitRuns.Sum(r => r.TotalDeductions),
                DepositCount = benefitRuns.Count,
                RemittedThrough = benefitRuns.Max(r => r.PayDate),
            };
            groups.Add(BenefitVendorCode, benefitGroup);
        }

        return groups.Values.OrderBy(g => g.Kind).ThenBy(g => g.VendorCode).ToList();
    }

    public const string FederalVendorCode = "EFTPS-FED";
    public const string BenefitVendorCode = "BENEFIT-REMIT";

    private static string ResolveAgencyVendorCode(string taxType, string agency, string? sutaState)
    {
        if (taxType.Contains("Federal", StringComparison.OrdinalIgnoreCase)
            || taxType.Contains("FUTA", StringComparison.OrdinalIgnoreCase)
            || taxType.Contains("941", StringComparison.OrdinalIgnoreCase)
            || taxType.Contains("940", StringComparison.OrdinalIgnoreCase))
            return FederalVendorCode;

        var normalizedAgency = (agency ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedAgency.Length == 0 || normalizedAgency == "EFTPS")
            return FederalVendorCode;

        foreach (var token in normalizedAgency.Split('-', ' ', '_'))
        {
            if (token.Length == 2 && char.IsAsciiLetter(token[0]) && char.IsAsciiLetter(token[1]))
                return $"DOR-{token}";
        }

        if (!string.IsNullOrWhiteSpace(sutaState))
            return $"DOR-{sutaState.Trim().ToUpperInvariant()}";

        return $"DOR-{(normalizedAgency.Length > 10 ? normalizedAgency[..10] : normalizedAgency)}";
    }

    private static string ResolveVendorName(string vendorCode)
    {
        if (vendorCode == FederalVendorCode)
            return "EFTPS Federal Tax Agency";
        if (vendorCode == BenefitVendorCode)
            return "Benefit Plan Remittance Payee";
        if (vendorCode.StartsWith("DOR-", StringComparison.OrdinalIgnoreCase))
            return $"Department of Revenue ({vendorCode[4..]})";
        return $"Tax Agency {vendorCode}";
    }
}

public class CreateTaxDepositRequest
{
    public Guid CompanyId { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public Guid? PayrollRunId { get; set; }
    public DateTime DepositDate { get; set; }
    public decimal EstimatedAmount { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string? FormType { get; set; }
}

public class MarkDepositedRequest
{
    public decimal DepositedAmount { get; set; }
    public DateTime DepositedOn { get; set; }
}

public class TaxDepositDto
{
    public Guid Id { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public DateTime DepositDate { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal? DepositedAmount { get; set; }
    public DateTime? DepositedOn { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public string? FormType { get; set; }
    public bool Deposited { get; set; }
}

public enum LiabilityKind
{
    TaxDeposit = 0,
    BenefitRemittance = 1,
}

public class LiabilityPaymentGroupDto
{
    public string VendorCode { get; set; } = string.Empty;
    public string AgencyName { get; set; } = string.Empty;
    public LiabilityKind Kind { get; set; }
    public decimal Amount { get; set; }
    public int DepositCount { get; set; }
    public int UncoveredPostedRunCount { get; set; }
    public DateTime? RemittedThrough { get; set; }
    public List<Guid> TaxDepositIds { get; set; } = [];
}

public class PayLiabilitiesRequest
{
    public Guid CompanyId { get; set; }
    public DateTime PayThroughDate { get; set; }
    public List<string>? Agencies { get; set; }
}

public class LiabilityPaymentResultDto
{
    public string VendorCode { get; set; } = string.Empty;
    public string AgencyName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DepositCount { get; set; }
    public Guid VoucherId { get; set; }
}
