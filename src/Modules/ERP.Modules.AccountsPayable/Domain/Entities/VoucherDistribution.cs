// <copyright file="VoucherDistribution.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.AccountsPayable.Domain.Entities;

public class VoucherDistribution : Entity
{
    protected VoucherDistribution() { }

    internal VoucherDistribution(
        Guid voucherId,
        Guid accountId,
        decimal debit,
        decimal credit,
        Guid? projectId,
        Guid? taskId)
        : base(Guid.NewGuid())
    {
        VoucherId = voucherId;
        AccountId = accountId;
        Debit = debit;
        Credit = credit;
        ProjectId = projectId;
        TaskId = taskId;
    }

    public Guid VoucherId { get; private set; }

    public Guid AccountId { get; private set; }

    public decimal Debit { get; private set; }

    public decimal Credit { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? TaskId { get; private set; }

    public Voucher? Voucher { get; internal set; }
}
