// <copyright file="EmployeeTaxProfile.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Domain.Common;

namespace ERP.Modules.Payroll.Domain.Entities;

/// <summary>
/// Per-employee tax profile: federal/state allowances, additional withholding,
/// and assigned tax jurisdictions. Consumed by the withholding engine alongside
/// the W-4 record and the applicable <see cref="TaxTable"/> rows.
/// </summary>
public class EmployeeTaxProfile : AuditableEntity
{
    protected EmployeeTaxProfile() { }

    public EmployeeTaxProfile(
        Guid companyId,
        Guid employeeId,
        string? residentState,
        string? workState,
        decimal additionalFederalWithholding,
        decimal additionalStateWithholding,
        bool exemptFederal,
        bool exemptState)
        : base(Guid.NewGuid())
    {
        CompanyId = companyId;
        EmployeeId = employeeId;
        ResidentState = residentState;
        WorkState = workState;
        AdditionalFederalWithholding = additionalFederalWithholding;
        AdditionalStateWithholding = additionalStateWithholding;
        ExemptFederal = exemptFederal;
        ExemptState = exemptState;
    }

    public Guid CompanyId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string? ResidentState { get; private set; }
    public string? WorkState { get; private set; }
    public decimal AdditionalFederalWithholding { get; private set; }
    public decimal AdditionalStateWithholding { get; private set; }
    public bool ExemptFederal { get; private set; }
    public bool ExemptState { get; private set; }

    public void Update(
        string? residentState,
        string? workState,
        decimal additionalFederalWithholding,
        decimal additionalStateWithholding,
        bool exemptFederal,
        bool exemptState)
    {
        ResidentState = residentState;
        WorkState = workState;
        AdditionalFederalWithholding = additionalFederalWithholding;
        AdditionalStateWithholding = additionalStateWithholding;
        ExemptFederal = exemptFederal;
        ExemptState = exemptState;
    }
}
