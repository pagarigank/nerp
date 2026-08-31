// <copyright file="UserAccessRequest.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Platform.Domain.Entities;

/// <summary>
/// Status of a self-service access request submitted via the registration page.
/// </summary>
public enum AccessRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}

/// <summary>
/// A request for system access submitted by a prospective user through the
/// public "Request Access" (registration) page. Only a company administrator
/// (or the super admin) may approve/reject a request, after which a real
/// <see cref="User"/> account is provisioned.
/// </summary>
public class UserAccessRequest : AuditableAggregateRoot
{
    protected UserAccessRequest() { }

    public UserAccessRequest(
        string fullName,
        string email,
        string username,
        string passwordHash,
        Guid companyId,
        string requestedRole,
        string? phoneNumber = null,
        string? reason = null) : base(Guid.NewGuid())
    {
        FullName = fullName.Trim();
        Email = email.Trim();
        Username = username.Trim();
        PasswordHash = passwordHash;
        CompanyId = companyId;
        RequestedRole = requestedRole;
        PhoneNumber = phoneNumber?.Trim();
        Reason = reason?.Trim();
        Status = AccessRequestStatus.Pending;
    }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Username { get; private set; } = string.Empty;

    /// <summary>PBKDF2 hash of the requested password (format: pbkdf2:iterations:salt:hash).</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    /// <summary>The role the requester asked for (e.g. "Accountant"). The approver may override.</summary>
    public string RequestedRole { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? Reason { get; private set; }

    public AccessRequestStatus Status { get; private set; }

    public Guid? ReviewedBy { get; private set; }

    public DateTimeOffset? ReviewedOn { get; private set; }

    public string? ReviewNotes { get; private set; }

    public void Approve(Guid reviewerId, string? notes = null)
    {
        Status = AccessRequestStatus.Approved;
        ReviewedBy = reviewerId;
        ReviewedOn = DateTimeOffset.UtcNow;
        ReviewNotes = notes?.Trim();
    }

    public void Reject(Guid reviewerId, string? notes = null)
    {
        Status = AccessRequestStatus.Rejected;
        ReviewedBy = reviewerId;
        ReviewedOn = DateTimeOffset.UtcNow;
        ReviewNotes = notes?.Trim();
    }
}
