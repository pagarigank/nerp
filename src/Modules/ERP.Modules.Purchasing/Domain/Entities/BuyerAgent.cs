// <copyright file="BuyerAgent.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class BuyerAgent : AuditableEntity
{
    protected BuyerAgent() { }

    public BuyerAgent(
        string buyerCode,
        string name,
        Guid userId,
        string? email,
        string? phone,
        decimal approvalLimit,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(buyerCode))
            throw new ArgumentException("Buyer code is required.", nameof(buyerCode));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Buyer name is required.", nameof(name));

        if (approvalLimit < 0)
            throw new ArgumentException("Approval limit cannot be negative.", nameof(approvalLimit));

        BuyerCode = buyerCode;
        Name = name;
        UserId = userId;
        Email = email;
        Phone = phone;
        ApprovalLimit = approvalLimit;
        IsActive = isActive;
    }

    public string BuyerCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid UserId { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public decimal ApprovalLimit { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateApprovalLimit(decimal newLimit)
    {
        if (newLimit < 0)
            throw new ArgumentException("Approval limit cannot be negative.", nameof(newLimit));

        ApprovalLimit = newLimit;
    }

    public void UpdateContactInfo(string? email, string? phone)
    {
        Email = email;
        Phone = phone;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public bool CanApprove(decimal amount)
    {
        return IsActive && amount <= ApprovalLimit;
    }
}
