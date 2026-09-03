// <copyright file="EInvoiceDocumentStatus.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.EInvoice.Domain;

/// <summary>
/// e-Invoice document processing status.
/// </summary>
#pragma warning disable CA1720
public enum EInvoiceDocumentStatus
{
    Queued = 0,
    Signed = 1,
    Submitted = 2,
    Validating = 3,
    Valid = 4,
    Invalid = 5,
    Cancelled = 6,
    Rejected = 7
}
#pragma warning restore CA1720