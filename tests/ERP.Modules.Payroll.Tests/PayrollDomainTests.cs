// <copyright file="PayrollDomainTests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System;
using ERP.Modules.Payroll.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.Payroll.Tests;

/// <summary>Domain-rule unit tests for the payroll module (Phase 11, Batch F).</summary>
public class PayrollDomainTests
{
    /// <summary>Reversing a run flips its status to Reversed.</summary>
    [Fact]
    public void PayrollRun_Reverse_MarksRunReversed()
    {
        var run = new PayrollRun(
            Guid.NewGuid(), null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(-6), DateTime.UtcNow);
        run.Status.Should().Be(PayrollRunStatus.Draft);

        run.MarkPosted(Guid.NewGuid(), "GL-BATCH-TEST");
        run.Status.Should().Be(PayrollRunStatus.Posted);

        run.Reverse();

        run.Status.Should().Be(PayrollRunStatus.Reversed);
    }

    /// <summary>Mark1099 toggles the 1099-NEC reporting flag on an off-cycle check.</summary>
    [Fact]
    public void ManualCheck_Mark1099_TogglesFlag()
    {
        var check = new ManualCheck(Guid.NewGuid(), Guid.NewGuid(), 5000m, DateTime.UtcNow, "Bonus");

        check.Is1099.Should().BeFalse();
        check.Mark1099(true);
        check.Is1099.Should().BeTrue();
    }

    /// <summary>The manual-check constructor defaults gross and net to the issued amount.</summary>
    [Fact]
    public void ManualCheck_Constructor_SetsGrossAndNetToAmount()
    {
        var check = new ManualCheck(Guid.NewGuid(), Guid.NewGuid(), 2500m, DateTime.UtcNow, "Advance", "ADV001");

        check.Amount.Should().Be(2500m);
        check.GrossPay.Should().Be(2500m);
        check.NetPay.Should().Be(2500m);
        check.CheckNumber.Should().Be("ADV001");
        check.Status.Should().Be(ManualCheckStatus.Issued);
    }

    /// <summary>Terminating an employee moves its status to Terminated.</summary>
    [Fact]
    public void Employee_Terminate_SetsTerminatedStatus()
    {
        var employee = new Employee(
            Guid.NewGuid(), "EMP001", "Jane", "Doe", EmploymentType.Salary, DateTime.UtcNow.AddYears(-1));

        employee.Status.Should().Be(EmployeeStatus.Active);

        employee.Terminate(DateTime.UtcNow);

        employee.Status.Should().Be(EmployeeStatus.Terminated);
    }

    /// <summary>Progressive bracket lookup returns the correct rate/bracket for a given wage.</summary>
    [Fact]
    public void TaxTable_BracketLookup_RespectsBounds()
    {
        var table = new TaxTable(
            Guid.NewGuid(), "CA State Income Tax", TaxJurisdictionLevel.State, "CA", 2026, FilingStatus.SingleFiler, null);
        table.AddBracket(0.02m, 0m, 1000m, 0m);
        table.AddBracket(0.04m, 1001m, null, 20m);

        var lower = table.Brackets.FirstOrDefault(b => b.LowerBound <= 500m && (b.UpperBound == null || b.UpperBound >= 500m));
        var upper = table.Brackets.FirstOrDefault(b => b.LowerBound <= 5000m && (b.UpperBound == null || b.UpperBound >= 5000m));

        lower.Should().NotBeNull();
        lower!.Rate.Should().Be(0.02m);
        upper.Should().NotBeNull();
        upper!.Rate.Should().Be(0.04m);
        upper!.FixedAmount.Should().Be(20m);
    }
}
