// <copyright file="ICreditLimitCheckService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Core.Common;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

/// <summary>
/// AR-specific credit-limit check contract. Inherits the shared <see cref="ICreditLimitCheck"/>
/// from ERP.Core so the Order Management module can enforce credit policy without a
/// compile-time dependency on this module.
/// </summary>
public interface ICreditLimitCheckService : ICreditLimitCheck
{
}
