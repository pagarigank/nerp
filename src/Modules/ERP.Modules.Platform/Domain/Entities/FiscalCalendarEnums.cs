// <copyright file="FiscalCalendarEnums.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Domain.Entities;

/// <summary>
/// Fiscal calendar pattern. The default <see cref="Standard"/> is a 12-month
/// calendar year. <see cref="Period13"/> uses a 13-period (4-4-5, 4-4-5, 4-4-4-1)
/// pattern common in retail, and <see cref="FourFourFive"/> is the strict 4-4-5
/// quarterly distribution (3 × 4 weeks, 4 weeks, 5 weeks).
/// </summary>
public enum FiscalCalendarType
{
    Standard = 0,
    Period13 = 1,
    FourFourFive = 2
}

/// <summary>
/// Whether the fiscal year ends on a fixed calendar date (December 31, or a
/// configured month-end) or on a floating/business rule (e.g. last Saturday of
/// the month, used by retailers and some manufacturers).
/// </summary>
public enum FiscalYearEndType
{
    Calendar = 0,
    Floating = 1
}
