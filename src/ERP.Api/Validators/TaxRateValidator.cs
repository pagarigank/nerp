// <copyright file="TaxRateValidator.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.MalaysianTax.Domain.Entities;
using FluentValidation;

namespace ERP.Api.Validators;

/// <summary>
/// FluentValidation rules for <see cref="TaxRate"/>.
/// Enforces effective-dating constraints and valid rate ranges.
/// </summary>
public class TaxRateValidator : AbstractValidator<TaxRate>
{
    public TaxRateValidator()
    {
        RuleFor(x => x.TaxCodeId)
            .NotEmpty().WithMessage("Tax Code is required");

        RuleFor(x => x.RatePercent)
            .InclusiveBetween(0m, 100m).WithMessage("Rate percent must be between 0 and 100");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective From date is required");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("Effective To must be after Effective From");

        // Prevent rates older than 10 years (reasonable audit boundary)
        RuleFor(x => x.EffectiveFrom)
            .GreaterThan(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithMessage("Effective From date seems unreasonably old");
    }
}
