// <copyright file="RequisitionTemplate.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Purchasing.Domain.Entities;

public class RequisitionTemplate : AuditableEntity
{
    private readonly List<RequisitionTemplateLine> _lines = [];

    protected RequisitionTemplate() { }

    public RequisitionTemplate(
        string templateCode,
        string templateName,
        Guid companyId,
        string? description,
        bool isActive = true)
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(templateCode))
            throw new ArgumentException("Template code is required.", nameof(templateCode));

        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required.", nameof(templateName));

        TemplateCode = templateCode;
        TemplateName = templateName;
        CompanyId = companyId;
        Description = description;
        IsActive = isActive;
    }

    public string TemplateCode { get; private set; } = string.Empty;

    public string TemplateName { get; private set; } = string.Empty;

    public Guid CompanyId { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<RequisitionTemplateLine> Lines => _lines.AsReadOnly();

    public void AddLine(RequisitionTemplateLine line)
    {
        _lines.Add(line);
    }

    public void RemoveLine(Guid lineId)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line != null)
        {
            _lines.Remove(line);
        }
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }
}
