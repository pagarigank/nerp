// <copyright file="TaxCodeValidator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;
using FluentValidation;

namespace ERP.Api.Validators;

/// <summary>
/// FluentValidation rules for <see cref="TaxCode"/>.
/// Enforces LHDN-compliant tax type codes and required fields before persistence.
/// </summary>
public class TaxCodeValidator : AbstractValidator<TaxCode>
{
    /// <summary>
    /// Valid LHDN e-Invoice tax type codes per MyInvois specification.
    /// </summary>
    private static readonly string[] ValidTaxTypeCodes = { "01", "02", "03", "04", "E", "ZRL" };

    public TaxCodeValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Tax code is required")
            .MaximumLength(50).WithMessage("Tax code must not exceed 50 characters")
            .Matches("^[A-Za-z0-9\\-]+$").WithMessage("Tax code must contain only letters, numbers, and hyphens");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(250).WithMessage("Description must not exceed 250 characters");

        RuleFor(x => x.LhdntTaxType)
            .NotEmpty().WithMessage("LHDN tax type is required")
            .MaximumLength(5).WithMessage("LHDN tax type must not exceed 5 characters")
            .Must(taxType => ValidTaxTypeCodes.Contains(taxType))
            .WithMessage($"LHDN tax type must be one of: {string.Join(", ", ValidTaxTypeCodes)}");
    }
}
