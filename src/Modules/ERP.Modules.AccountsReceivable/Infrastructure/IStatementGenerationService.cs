// <copyright file="IStatementGenerationService.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using ERP.Modules.AccountsReceivable.Domain.Entities;

namespace ERP.Modules.AccountsReceivable.Infrastructure;

public interface IStatementGenerationService
{
    Task<IReadOnlyList<Statement>> GenerateStatementsAsync(Guid companyId, DateTimeOffset asOfDate, CancellationToken cancellationToken = default);
}
