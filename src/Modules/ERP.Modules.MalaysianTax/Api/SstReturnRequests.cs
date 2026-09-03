// <copyright file="SstReturnRequests.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.MalaysianTax.Api;

/// <summary>
/// Request body for computing an SST return.
/// </summary>
public class ComputeReturnRequest
{
    /// <summary>Gets or sets the period start date (1st of odd month).</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>Gets or sets the return type: SST-02 (Sales Tax) or SST-02A (Service Tax).</summary>
    public string ReturnType { get; set; } = string.Empty;
}

/// <summary>
/// Request body for filing an SST return.
/// </summary>
public class FileReturnRequest
{
    /// <summary>Gets or sets the LHDN reference number.</summary>
    public string LhdnReference { get; set; } = string.Empty;
}
