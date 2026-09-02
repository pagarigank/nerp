// <copyright file="EdiTransactionLog.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Integration.Domain.Entities;

public class EdiTransactionLog : AuditableEntity
{
    protected EdiTransactionLog() { }

    public EdiTransactionLog(
        Guid companyId,
        Guid? tradingPartnerId,
        string partnerCode,
        string documentType,
        string direction,
        string? isaControlNumber,
        string? gsControlNumber,
        string? stControlNumber,
        string? rawContent)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        TradingPartnerId = tradingPartnerId;
        PartnerCode = partnerCode;
        DocumentType = documentType;
        Direction = direction;
        IsaControlNumber = isaControlNumber;
        GsControlNumber = gsControlNumber;
        StControlNumber = stControlNumber;
        RawContent = rawContent;
        Status = "Received";
        ProcessedOn = DateTimeOffset.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public Guid? TradingPartnerId { get; private set; }
    public string PartnerCode { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty;
    public string Direction { get; private set; } = string.Empty;
    public string? IsaControlNumber { get; private set; }
    public string? GsControlNumber { get; private set; }
    public string? StControlNumber { get; private set; }
    public string Status { get; private set; } = "Received";
    public string? ErrorMessage { get; private set; }
    public string? RawContent { get; private set; }
    public DateTimeOffset ProcessedOn { get; private set; }
    public DateTimeOffset? AcknowledgedOn { get; private set; }
    public Guid? ReferenceId { get; private set; }

    public void MarkProcessed(Guid? referenceId)
    {
        Status = "Processed";
        ReferenceId = referenceId;
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        ErrorMessage = error?.Length > 4000 ? error[..4000] : error;
    }

    public void MarkAcknowledged()
    {
        Status = "Acknowledged";
        AcknowledgedOn = DateTimeOffset.UtcNow;
    }
}

public class EdiAck : AuditableEntity
{
    protected EdiAck() { }

    public EdiAck(
        string ackType,
        string originalIsaControlNumber,
        string originalGsControlNumber,
        string? originalStControlNumber,
        string? rawContent)
    {
        Id = Guid.NewGuid();
        AckType = ackType;
        OriginalIsaControlNumber = originalIsaControlNumber;
        OriginalGsControlNumber = originalGsControlNumber;
        OriginalStControlNumber = originalStControlNumber;
        RawContent = rawContent;
        Status = "Received";
        ProcessedOn = DateTimeOffset.UtcNow;
    }

    public string AckType { get; private set; } = "997";
    public string OriginalIsaControlNumber { get; private set; } = string.Empty;
    public string OriginalGsControlNumber { get; private set; } = string.Empty;
    public string? OriginalStControlNumber { get; private set; }
    public string Status { get; private set; } = "Received";
    public string? ErrorMessage { get; private set; }
    public string? RawContent { get; private set; }
    public DateTimeOffset ProcessedOn { get; private set; }
    public DateTimeOffset? AcknowledgedOn { get; private set; }

    public void MarkAccepted()
    {
        Status = "Accepted";
        AcknowledgedOn = DateTimeOffset.UtcNow;
    }

    public void MarkRejected(string error)
    {
        Status = "Rejected";
        ErrorMessage = error?.Length > 2000 ? error[..2000] : error;
        AcknowledgedOn = DateTimeOffset.UtcNow;
    }
}
