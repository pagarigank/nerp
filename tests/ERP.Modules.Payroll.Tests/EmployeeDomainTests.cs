using ERP.Modules.Payroll.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ERP.Modules.Payroll.Tests;

public class EmployeeDomainTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Fact]
    public void Employee_CreatesWithValidProperties()
    {
        var emp = new Employee(CompanyId, "EMP-001", "John", "Doe",
            EmploymentType.Salary, new DateTime(2024, 1, 15));

        emp.EmployeeCode.Should().Be("EMP-001");
        emp.FirstName.Should().Be("John");
        emp.LastName.Should().Be("Doe");
        emp.FullName.Should().Be("John Doe");
        emp.EmploymentType.Should().Be(EmploymentType.Salary);
        emp.Status.Should().Be(EmployeeStatus.Active);
        emp.HireDate.Should().Be(new DateTime(2024, 1, 15));
    }

    [Fact]
    public void Employee_ThrowsOnEmptyCode()
    {
        var act = () => new Employee(CompanyId, "", "John", "Doe",
            EmploymentType.Hourly, DateTime.UtcNow);
        act.Should().Throw<ArgumentException>().WithMessage("*Employee code is required*");
    }

    [Fact]
    public void Employee_FullNameCombinesFirstAndLast()
    {
        var emp = new Employee(CompanyId, "EMP-001", "Jane", "Smith",
            EmploymentType.Hourly, DateTime.UtcNow);
        emp.FullName.Should().Be("Jane Smith");
    }

    [Fact]
    public void Employee_TerminateSetsStatusAndDate()
    {
        var emp = new Employee(CompanyId, "EMP-001", "John", "Doe",
            EmploymentType.Salary, DateTime.UtcNow.AddYears(-2));

        emp.Terminate(DateTime.UtcNow);

        emp.Status.Should().Be(EmployeeStatus.Terminated);
        emp.TerminationDate.Should().NotBeNull();
    }

    // ==================== PayrollRun Tests ====================

    [Fact]
    public void PayrollRun_CreatesWithValidProperties()
    {
        var run = new PayrollRun(CompanyId, null,
            new DateTime(2024, 1, 1), new DateTime(2024, 1, 15),
            new DateTime(2024, 1, 20));

        run.PeriodStart.Should().Be(new DateTime(2024, 1, 1));
        run.PeriodEnd.Should().Be(new DateTime(2024, 1, 15));
        run.PayDate.Should().Be(new DateTime(2024, 1, 20));
        run.Status.Should().Be(PayrollRunStatus.Draft);
    }

    [Fact]
    public void PayrollRun_TotalGrossSumsLines()
    {
        var run = new PayrollRun(CompanyId, null,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(14), DateTime.UtcNow.AddDays(21));

        run.AddLine(Guid.NewGuid(), 80m, 0m, 25m, 0m, 2000m, 300m, 150m, 200m, 1550m);
        run.AddLine(Guid.NewGuid(), 40m, 10m, 25m, 37.5m, 1375m, 200m, 100m, 150m, 925m);

        run.TotalGross.Should().Be(3375m);
        run.TotalEmployeeTax.Should().Be(500m);
        run.TotalDeductions.Should().Be(250m);
        run.TotalNet.Should().Be(2475m);
    }

    [Fact]
    public void PayrollRun_MarkPostedSetsStatusAndDate()
    {
        var run = new PayrollRun(CompanyId, null,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(14), DateTime.UtcNow.AddDays(21));

        run.MarkPosted(Guid.NewGuid(), "GL-2024-001");

        run.Status.Should().Be(PayrollRunStatus.Posted);
        run.PostedOn.Should().NotBeNull();
    }

    [Fact]
    public void PayrollRun_VoidReversesStatus()
    {
        var run = new PayrollRun(CompanyId, null,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(14), DateTime.UtcNow.AddDays(21));

        run.Void();

        run.Status.Should().Be(PayrollRunStatus.Void);
    }

    // ==================== Timesheet Tests ====================

    [Fact]
    public void Timesheet_CreatesWithValidProperties()
    {
        var ts = new Timesheet(CompanyId, Guid.NewGuid(), new DateTime(2024, 1, 7));

        ts.WeekEnding.Should().Be(new DateTime(2024, 1, 7));
        ts.Status.Should().Be(TimesheetStatus.Draft);
    }

    [Fact]
    public void Timesheet_SubmitChangesStatus()
    {
        var ts = new Timesheet(CompanyId, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));

        ts.Submit(Guid.NewGuid());

        ts.Status.Should().Be(TimesheetStatus.Submitted);
    }

    [Fact]
    public void Timesheet_ApproveSetsApprovalInfo()
    {
        var ts = new Timesheet(CompanyId, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        ts.Submit(Guid.NewGuid());
        var approverId = Guid.NewGuid();

        ts.Approve(approverId);

        ts.Status.Should().Be(TimesheetStatus.Approved);
        ts.ApprovedById.Should().Be(approverId);
        ts.ApprovedOn.Should().NotBeNull();
    }

    [Fact]
    public void Timesheet_RejectSetsRejectionReason()
    {
        var ts = new Timesheet(CompanyId, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));
        ts.Submit(Guid.NewGuid());

        ts.Reject("Missing hours for Friday");

        ts.Status.Should().Be(TimesheetStatus.Rejected);
        ts.RejectionReason.Should().Be("Missing hours for Friday");
    }

    [Fact]
    public void Timesheet_AddLineIncreasesTotalHours()
    {
        var ts = new Timesheet(CompanyId, Guid.NewGuid(), DateTime.UtcNow.AddDays(7));

        ts.AddLine(null, null, Guid.NewGuid(), new DateTime(2024, 1, 1), 8m, 25m, "Regular day");
        ts.AddLine(null, null, Guid.NewGuid(), new DateTime(2024, 1, 2), 10m, 37.5m, "Overtime");

        ts.TotalHours.Should().Be(18m);
    }
}
