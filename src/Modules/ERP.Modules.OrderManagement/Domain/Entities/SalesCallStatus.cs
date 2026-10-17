// <copyright file="SalesCallStatus.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.OrderManagement.Domain.Entities;

public enum SalesCallStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
}
