// <copyright file="ImportJob.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Collections.Generic;
using ERP.Core.Domain.Common;

namespace ERP.Modules.Integration.Domain.Entities;

public class ImportJob : AuditableEntity
{
    private readonly List<ImportJobLine> _lines = [];

    protected ImportJob() { }

    public ImportJob(
        Guid companyId,
        string jobName,
        string fileFormat,
        string destination,
        string? fileName,
        Guid? ediDocumentMappingId)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        JobName = jobName;
        FileFormat = fileFormat;
        Destination = destination;
        FileName = fileName;
        EdiDocumentMappingId = ediDocumentMappingId;
        Status = "Draft";
        TotalRows = 0;
        ValidatedRows = 0;
        ErrorRows = 0;
        CommittedRows = 0;
    }

    public Guid CompanyId { get; private set; }
    public string JobName { get; private set; } = string.Empty;
    public string FileFormat { get; private set; } = "csv";
    public string Destination { get; private set; } = string.Empty;
    public string? FileName { get; private set; }
    public Guid? EdiDocumentMappingId { get; private set; }
    public string Status { get; private set; } = "Draft";
    public int TotalRows { get; private set; }
    public int ValidatedRows { get; private set; }
    public int ErrorRows { get; private set; }
    public int CommittedRows { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset? ValidatedOn { get; private set; }
    public DateTimeOffset? CommittedOn { get; private set; }

    public IReadOnlyCollection<ImportJobLine> Lines => _lines.AsReadOnly();

    public void AddLine(ImportJobLine line) => _lines.Add(line);

    public void MarkValidating()
    {
        Status = "Validating";
        TotalRows = _lines.Count;
    }

    public void MarkValidated(int validCount, int errorCount)
    {
        Status = "Validated";
        ValidatedRows = validCount;
        ErrorRows = errorCount;
        ValidatedOn = DateTimeOffset.UtcNow;
    }

    public void MarkCommitting() => Status = "Committing";

    public void MarkCommitted(int committedCount)
    {
        Status = "Committed";
        CommittedRows = committedCount;
        CommittedOn = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        ErrorMessage = error?.Length > 4000 ? error[..4000] : error;
    }

    public void MarkDraft() => Status = "Draft";
}

public class ImportJobLine : AuditableEntity
{
    protected ImportJobLine() { }

    public ImportJobLine(
        Guid importJobId,
        int rowNumber,
        string rowData)
    {
        Id = Guid.NewGuid();
        ImportJobId = importJobId;
        RowNumber = rowNumber;
        RowData = rowData;
        ValidationStatus = "Pending";
    }

    public Guid ImportJobId { get; private set; }
    public int RowNumber { get; private set; }
    public string RowData { get; private set; } = "{}";
    public string ValidationStatus { get; private set; } = "Pending";
    public string? Errors { get; private set; }
    public bool Imported { get; private set; }

    public void MarkValid()
    {
        ValidationStatus = "Valid";
        Errors = null;
    }

    public void MarkInvalid(string errors)
    {
        ValidationStatus = "Invalid";
        Errors = errors?.Length > 4000 ? errors[..4000] : errors;
    }

    public void MarkImported() => Imported = true;
}
