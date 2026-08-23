// <copyright file="ApServiceContracts.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Core.Common;

/// <summary>
/// Raised when an Accounts Payable voucher batch posts. One event per voucher whose
/// distributions carry project segments. Consumed by Project Accounting
/// (VoucherPostedToProjectHandler) to post the vendor-invoice cost to the project
/// ledger and dual-post to GL. Lives in ERP.Core so Accounts Payable (publisher) and
/// Project Accounting (consumer) can share it without a module cycle, mirroring the
/// LaborPostedToProjectEvent placement.
/// </summary>
public sealed record VoucherPostedEvent : DomainEvent
{
    public VoucherPostedEvent(
        Guid companyId,
        Guid voucherId,
        Guid vendorId,
        string voucherNumber,
        DateTimeOffset postingDate,
        IReadOnlyList<VoucherCostLine> lines)
    {
        CompanyId = companyId;
        VoucherId = voucherId;
        VendorId = vendorId;
        VoucherNumber = voucherNumber;
        PostingDate = postingDate;
        Lines = lines;
    }

    public Guid CompanyId { get; }

    public Guid VoucherId { get; }

    public Guid VendorId { get; }

    public string VoucherNumber { get; }

    public DateTimeOffset PostingDate { get; }

    public IReadOnlyList<VoucherCostLine> Lines { get; }

    public override string EventType => "VoucherPosted";
}

/// <summary>A flattened voucher distribution line for cross-module project costing.</summary>
public sealed record VoucherCostLine(Guid? ProjectId, Guid? TaskId, decimal Amount, string? Category);
