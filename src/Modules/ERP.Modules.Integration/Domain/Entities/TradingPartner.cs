// <copyright file="TradingPartner.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Integration.Domain.Entities;

public class TradingPartner : AuditableEntity
{
    protected TradingPartner() { }

    public TradingPartner(
        Guid companyId,
        string partnerCode,
        string name,
        string? isaId,
        string? gsSenderId,
        string? gsReceiverId,
        string? protocol,
        string? certificateThumbprint,
        string ediVersion)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        PartnerCode = partnerCode;
        Name = name;
        IsaId = isaId;
        GsSenderId = gsSenderId;
        GsReceiverId = gsReceiverId;
        Protocol = protocol;
        CertificateThumbprint = certificateThumbprint;
        EdiVersion = ediVersion;
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }
    public string PartnerCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? IsaId { get; private set; }
    public string? GsSenderId { get; private set; }
    public string? GsReceiverId { get; private set; }
    public string? Protocol { get; private set; }
    public string? CertificateThumbprint { get; private set; }
    public string EdiVersion { get; private set; } = "5010";
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? CertificateExpiresOn { get; private set; }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateProtocol(string? protocol, string? certificateThumbprint, DateTimeOffset? certExpiresOn)
    {
        Protocol = protocol;
        CertificateThumbprint = certificateThumbprint;
        CertificateExpiresOn = certExpiresOn;
    }
}
