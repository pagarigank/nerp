// <copyright file="BomStatus.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.BillOfMaterials.Domain.Entities;

public enum BomStatus
{
    Draft = 0,
    Active = 1,
    Obsolete = 2,
}

public enum BomType
{
    Standard = 0,
    Phantom = 1,
    Alternate = 2,
}

public enum BuildOrderStatus
{
    Draft = 0,
    Planned = 1,
    Released = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
}

public enum BuildTransactionType
{
    Assemble = 0,
    Disassemble = 1,
}
