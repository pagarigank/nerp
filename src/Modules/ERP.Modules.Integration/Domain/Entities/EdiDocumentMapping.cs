// <copyright file="EdiDocumentMapping.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Integration.Domain.Entities;

public class EdiDocumentMapping : AuditableEntity
{
    protected EdiDocumentMapping() { }

    public EdiDocumentMapping(
        Guid companyId,
        Guid? tradingPartnerId,
        string documentType,
        string documentName,
        string direction,
        string version,
        string mappingJson)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        TradingPartnerId = tradingPartnerId;
        DocumentType = documentType;
        DocumentName = documentName;
        Direction = direction;
        Version = version;
        MappingJson = mappingJson;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public Guid? TradingPartnerId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string DocumentName { get; private set; } = string.Empty;
    public string Direction { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public string MappingJson { get; private set; } = "{}";
    public bool IsActive { get; private set; } = true;

    public void UpdateMapping(string mappingJson) => MappingJson = mappingJson;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
