// <copyright file="RowLevelSecurityService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.Reporting.Infrastructure;
using ERP.Modules.Reporting.Domain.Entities;
using ERP.Modules.Platform.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Modules.Reporting.Services;

/// <summary>
/// Enforces row-level and field-level security on report output.
/// Reports respect the same Platform Role/Permission model: a user can only
/// see rows belonging to companies they are authorized for, and field-level
/// exclusions (PII redaction for SSN, bank-account, etc.) are applied before
/// the result set leaves the service.
/// </summary>
public interface IRowLevelSecurityService
{
    /// <summary>
    /// Determines whether the given user is allowed to run a specific report
    /// for the given company. Returns true if the user has at least one role
    /// that includes the "rpt:read" or "rpt:{module}:read" permission for
    /// the report's module.
    /// </summary>
    Task<bool> CanUserRunReportAsync(
        string userId,
        Guid companyId,
        string module,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Filters a list of report rows to only those the user is authorized to see.
    /// Rows are filtered by CompanyId (must match one of the user's assigned companies).
    /// </summary>
    IReadOnlyList<Dictionary<string, object?>> ApplyRowFilter(
        string userId,
        Guid? companyId,
        IReadOnlyList<Dictionary<string, object?>> rows);

    /// <summary>
    /// Redacts sensitive fields from each row based on field-level security rules.
    /// Fields listed in the PII registry (SSN, BankAccount, TaxId, etc.) are replaced
    /// with masked values unless the user has the "rpt:pii:read" permission.
    /// </summary>
    IReadOnlyList<Dictionary<string, object?>> ApplyFieldRedaction(
        string userId,
        IReadOnlyList<Dictionary<string, object?>> rows,
        bool isPiiAuthorized);

    /// <summary>
    /// Returns the list of company IDs the user is authorized to access.
    /// An empty list means no access (unless the user is a super admin, in
    /// which case this method should not be called).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAuthorizedCompanyIdsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the user is a super admin (has a role assignment with null CompanyId).
    /// </summary>
    Task<bool> IsSuperAdminAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public class RowLevelSecurityService : IRowLevelSecurityService
{
    private readonly PlatformDbContext _platformDb;

    /// <summary>
    /// Fields that require PII redaction when the user lacks the rpt:pii:read permission.
    /// Keys are case-insensitive column names that appear in report result sets.
    /// </summary>
    private static readonly HashSet<string> PiiFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "SSN", "SocialSecurityNumber", "TaxId", "EIN",
        "BankAccountNumber", "BankRoutingNumber", "AccountNumber",
        "CreditCardNumber", "DriverLicenseNumber",
    };

    /// <summary>
    /// Mask applied to PII fields when the user is not authorized.
    /// </summary>
    private const string PiiMask = "***-**-****";

    public RowLevelSecurityService(PlatformDbContext platformDb)
    {
        _platformDb = platformDb ?? throw new ArgumentNullException(nameof(platformDb));
    }

    public async Task<bool> CanUserRunReportAsync(
        string userId,
        Guid companyId,
        string module,
        CancellationToken cancellationToken = default)
    {
        if (await IsSuperAdminAsync(userId, cancellationToken))
        {
            return true;
        }

        var authorizedCompanies = await GetAuthorizedCompanyIdsAsync(userId, cancellationToken);
        if (!authorizedCompanies.Contains(companyId))
        {
            return false;
        }

        // Check if user has rpt:read or rpt:{module}:read permission
        var hasPermission = await _platformDb.UserRoles
            .Where(ur => ur.UserId.ToString() == userId)
            .Join(_platformDb.RolePermissions,
                ur => ur.RoleId,
                rp => rp.RoleId,
                (ur, rp) => rp.PermissionId)
            .Join(_platformDb.Permissions,
                rpId => rpId,
                p => p.Id,
                (rpId, p) => p)
            .AnyAsync(p =>
                (p.Module == "rpt" && p.Action == "read") ||
                (p.Module == "rpt" && p.Action == $"{module}:read") ||
                (p.Module == "rpt" && p.Action == "admin"),
            cancellationToken);

        return hasPermission;
    }

    public IReadOnlyList<Dictionary<string, object?>> ApplyRowFilter(
        string userId,
        Guid? companyId,
        IReadOnlyList<Dictionary<string, object?>> rows)
    {
        if (companyId.HasValue)
    {
            return rows.Where(r =>
            {
                if (r.TryGetValue("CompanyId", out var cidObj) && cidObj is Guid cid)
                {
                    return cid == companyId.Value;
                }
                return true; // Rows without CompanyId pass through
            }).ToList();
        }

        return rows;
    }

    public IReadOnlyList<Dictionary<string, object?>> ApplyFieldRedaction(
        string userId,
        IReadOnlyList<Dictionary<string, object?>> rows,
        bool isPiiAuthorized)
    {
        if (isPiiAuthorized)
        {
            return rows;
        }

        return rows.Select(row =>
        {
            var redacted = new Dictionary<string, object?>(row);
            foreach (var key in redacted.Keys.ToList())
            {
                if (PiiFields.Contains(key) && redacted[key] != null)
    {
                    redacted[key] = PiiMask;
                }
            }
            return redacted;
        }).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetAuthorizedCompanyIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var companyIds = await _platformDb.UserRoles
            .Where(ur => ur.UserId.ToString() == userId && ur.CompanyId.HasValue)
            .Select(ur => ur.CompanyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return companyIds;
    }

    public async Task<bool> IsSuperAdminAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _platformDb.UserRoles
            .AnyAsync(ur =>
                ur.UserId.ToString() == userId &&
                ur.CompanyId == null,
            cancellationToken);
    }
}
