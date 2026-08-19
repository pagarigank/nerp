// <copyright file="IAutoCashApplicationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public interface IAutoCashApplicationService
{
    Task<IReadOnlyList<CashReceiptApplication>> AutoApplyAsync(Guid cashReceiptId, CancellationToken cancellationToken = default);
}
