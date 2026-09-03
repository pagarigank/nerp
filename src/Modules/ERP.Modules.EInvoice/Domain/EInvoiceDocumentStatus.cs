// <copyright file="EInvoiceDocumentStatus.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.EInvoice.Domain;

/// <summary>
/// e-Invoice document processing status.
/// </summary>
public enum EInvoiceDocumentStatus
{
    Queued = 0,
    Signed = 1,
    Submitted = 2,
    Valid = 3,
    Invalid = 4,
    Cancelled = 5,
    Rejected = 6
}