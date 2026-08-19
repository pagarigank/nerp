// <copyright file="IUnitOfWork.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    IRepository<Domain.Entities.Company> Companies { get; }
    IRepository<Domain.Entities.FiscalYear> FiscalYears { get; }
    IRepository<Domain.Entities.FiscalPeriod> FiscalPeriods { get; }
    IRepository<Domain.Entities.SegmentType> SegmentTypes { get; }
    IRepository<Domain.Entities.SegmentValue> SegmentValues { get; }
    IRepository<Domain.Entities.Account> Accounts { get; }
    IRepository<Domain.Entities.Currency> Currencies { get; }
    IRepository<Domain.Entities.ExchangeRate> ExchangeRates { get; }
    IRepository<Domain.Entities.NumberSequence> NumberSequences { get; }
    IRepository<Domain.Entities.Role> Roles { get; }
    IRepository<Domain.Entities.Permission> Permissions { get; }
    IRepository<Domain.Entities.RolePermission> RolePermissions { get; }
    IRepository<Domain.Entities.User> Users { get; }
    IRepository<Domain.Entities.UserRole> UserRoles { get; }
    IRepository<Domain.Entities.AuditLog> AuditLogs { get; }
    IRepository<Domain.Entities.ApprovalWorkflow> ApprovalWorkflows { get; }
    IRepository<Domain.Entities.ApprovalStep> ApprovalSteps { get; }
    IRepository<Domain.Entities.ApprovalRequest> ApprovalRequests { get; }
    IRepository<Domain.Entities.ApprovalAction> ApprovalActions { get; }
    IRepository<Domain.Entities.SoDRule> SoDRules { get; }
    IRepository<Domain.Entities.SoDConflict> SoDConflicts { get; }
    IRepository<Domain.Entities.ValidatedCombination> ValidatedCombinations { get; }
    IRepository<Domain.Entities.ApiKey> ApiKeys { get; }
    IRepository<Domain.Entities.ApprovalDelegation> ApprovalDelegations { get; }
    IRepository<Domain.Entities.ApprovalEscalationPolicy> ApprovalEscalationPolicies { get; }
    IRepository<Domain.Entities.HolidayCalendar> HolidayCalendars { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
