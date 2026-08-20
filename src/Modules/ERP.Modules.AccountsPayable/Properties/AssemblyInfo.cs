// <copyright file="AssemblyInfo.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;

// Allow the payroll test project to construct internal AP domain entities
// (e.g. Voucher) so the expense-reimbursement -> AP voucher wiring can be
// unit-tested without exposing internals more broadly.
[assembly: InternalsVisibleTo("ERP.Modules.Payroll.Tests")]
