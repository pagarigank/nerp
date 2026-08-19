// <copyright file="BillingSchedule.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.ProjectAccounting.Domain.Entities;

public class BillingSchedule : AuditableEntity
{
    protected BillingSchedule() { }

    public BillingSchedule(
        Guid projectId,
        string description,
        BillingMethod billingMethod,
        decimal amount,
        decimal? percentCompleteTrigger,
        DateTime? scheduledDate,
        int sequenceNumber)
        : base(Guid.NewGuid())
    {
        ProjectId = projectId;
        Description = description;
        BillingMethod = billingMethod;
        Amount = amount;
        PercentCompleteTrigger = percentCompleteTrigger;
        ScheduledDate = scheduledDate;
        SequenceNumber = sequenceNumber;
        IsBilled = false;
        BilledDate = null;
        InvoiceId = null;
    }

    public Guid ProjectId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public BillingMethod BillingMethod { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? PercentCompleteTrigger { get; private set; }
    public DateTime? ScheduledDate { get; private set; }
    public int SequenceNumber { get; private set; }
    public bool IsBilled { get; private set; }
    public DateTime? BilledDate { get; private set; }
    public Guid? InvoiceId { get; private set; }

    public void MarkBilled(Guid invoiceId)
    {
        IsBilled = true;
        BilledDate = DateTime.UtcNow;
        InvoiceId = invoiceId;
    }

    public void Update(
        string? description,
        decimal? amount,
        decimal? percentCompleteTrigger,
        DateTime? scheduledDate)
    {
        if (description is not null)
        {
            Description = description;
        }

        if (amount.HasValue)
        {
            Amount = amount.Value;
        }

        if (percentCompleteTrigger.HasValue)
        {
            PercentCompleteTrigger = percentCompleteTrigger;
        }

        if (scheduledDate.HasValue)
        {
            ScheduledDate = scheduledDate;
        }
    }
}
