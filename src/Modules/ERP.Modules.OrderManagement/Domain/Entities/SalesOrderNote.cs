// <copyright file="SalesOrderNote.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.OrderManagement.Domain.Entities;

/// <summary>
/// Order-level note / attachment (Phase 8 gap 589). Customer-facing notes, internal
/// memos and attachment references attached to a sales order, surfaced on the order
/// detail and acknowledgment document.
/// </summary>
public class SalesOrderNote : AuditableEntity
{
    protected SalesOrderNote() { }

    public SalesOrderNote(
        Guid companyId,
        Guid salesOrderId,
        string text,
        bool isCustomerFacing = false,
        string noteType = "General",
        string? attachmentLink = null,
        string? createdBy = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        SalesOrderId = salesOrderId;
        Text = text;
        IsCustomerFacing = isCustomerFacing;
        NoteType = noteType;
        AttachmentLink = attachmentLink;
        CreatedBy = createdBy ?? string.Empty;
    }

    public Guid CompanyId { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsCustomerFacing { get; private set; }
    public string NoteType { get; private set; } = "General";
    public string? AttachmentLink { get; private set; }
}

/// <summary>
/// Append-only change-history record for a sales order (Phase 8 gap 589). Every
/// documented change (field, old value, new value, reason code, actor) is captured so
/// the order audit trail survives line edits and status transitions.
/// </summary>
public class SalesOrderChangeHistory : AuditableEntity
{
    protected SalesOrderChangeHistory() { }

    public SalesOrderChangeHistory(
        Guid companyId,
        Guid salesOrderId,
        string changedBy,
        string changeType,
        string? fieldName = null,
        string? oldValue = null,
        string? newValue = null,
        string? reasonCode = null)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        SalesOrderId = salesOrderId;
        ChangedBy = changedBy;
        ChangeType = changeType;
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
        ReasonCode = reasonCode;
    }

    public Guid CompanyId { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public string ChangeType { get; private set; } = string.Empty;
    public string? FieldName { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? ReasonCode { get; private set; }
    public DateTime ChangeDate { get; private set; } = DateTime.UtcNow;
}
