// <copyright file="UnitOfWork.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Platform.Domain.Entities;

namespace ERP.Modules.Platform.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly PlatformDbContext _context;

    private IRepository<Company>? _companies;
    private IRepository<FiscalYear>? _fiscalYears;
    private IRepository<FiscalPeriod>? _fiscalPeriods;
    private IRepository<SegmentType>? _segmentTypes;
    private IRepository<SegmentValue>? _segmentValues;
    private IRepository<Account>? _accounts;
    private IRepository<Currency>? _currencies;
    private IRepository<ExchangeRate>? _exchangeRates;
    private IRepository<NumberSequence>? _numberSequences;
    private IRepository<Role>? _roles;
    private IRepository<Permission>? _permissions;
    private IRepository<RolePermission>? _rolePermissions;
    private IRepository<User>? _users;
    private IRepository<UserRole>? _userRoles;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<ApprovalWorkflow>? _approvalWorkflows;
    private IRepository<ApprovalStep>? _approvalSteps;
    private IRepository<ApprovalRequest>? _approvalRequests;
    private IRepository<ApprovalAction>? _approvalActions;
    private IRepository<SoDRule>? _soDRules;
    private IRepository<SoDConflict>? _soDConflicts;
    private IRepository<ValidatedCombination>? _validatedCombinations;
    private IRepository<ApiKey>? _apiKeys;
    private IRepository<ApprovalDelegation>? _approvalDelegations;
    private IRepository<ApprovalEscalationPolicy>? _approvalEscalationPolicies;
    private IRepository<HolidayCalendar>? _holidayCalendars;

    public UnitOfWork(PlatformDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IRepository<Company> Companies => _companies ??= new Repository<Company>(_context);
    public IRepository<FiscalYear> FiscalYears => _fiscalYears ??= new Repository<FiscalYear>(_context);
    public IRepository<FiscalPeriod> FiscalPeriods => _fiscalPeriods ??= new Repository<FiscalPeriod>(_context);
    public IRepository<SegmentType> SegmentTypes => _segmentTypes ??= new Repository<SegmentType>(_context);
    public IRepository<SegmentValue> SegmentValues => _segmentValues ??= new Repository<SegmentValue>(_context);
    public IRepository<Account> Accounts => _accounts ??= new Repository<Account>(_context);
    public IRepository<Currency> Currencies => _currencies ??= new Repository<Currency>(_context);
    public IRepository<ExchangeRate> ExchangeRates => _exchangeRates ??= new Repository<ExchangeRate>(_context);
    public IRepository<NumberSequence> NumberSequences => _numberSequences ??= new Repository<NumberSequence>(_context);
    public IRepository<Role> Roles => _roles ??= new Repository<Role>(_context);
    public IRepository<Permission> Permissions => _permissions ??= new Repository<Permission>(_context);
    public IRepository<RolePermission> RolePermissions => _rolePermissions ??= new Repository<RolePermission>(_context);
    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<UserRole> UserRoles => _userRoles ??= new Repository<UserRole>(_context);
    public IRepository<AuditLog> AuditLogs => _auditLogs ??= new Repository<AuditLog>(_context);
    public IRepository<ApprovalWorkflow> ApprovalWorkflows => _approvalWorkflows ??= new Repository<ApprovalWorkflow>(_context);
    public IRepository<ApprovalStep> ApprovalSteps => _approvalSteps ??= new Repository<ApprovalStep>(_context);
    public IRepository<ApprovalRequest> ApprovalRequests => _approvalRequests ??= new Repository<ApprovalRequest>(_context);
    public IRepository<ApprovalAction> ApprovalActions => _approvalActions ??= new Repository<ApprovalAction>(_context);
    public IRepository<SoDRule> SoDRules => _soDRules ??= new Repository<SoDRule>(_context);
    public IRepository<SoDConflict> SoDConflicts => _soDConflicts ??= new Repository<SoDConflict>(_context);
    public IRepository<ValidatedCombination> ValidatedCombinations => _validatedCombinations ??= new Repository<ValidatedCombination>(_context);
    public IRepository<ApiKey> ApiKeys => _apiKeys ??= new Repository<ApiKey>(_context);
    public IRepository<ApprovalDelegation> ApprovalDelegations => _approvalDelegations ??= new Repository<ApprovalDelegation>(_context);
    public IRepository<ApprovalEscalationPolicy> ApprovalEscalationPolicies => _approvalEscalationPolicies ??= new Repository<ApprovalEscalationPolicy>(_context);
    public IRepository<HolidayCalendar> HolidayCalendars => _holidayCalendars ??= new Repository<HolidayCalendar>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
